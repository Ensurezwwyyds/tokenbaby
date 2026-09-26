using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TokenBaby
{
    internal sealed class CodexClient : IDisposable
    {
        private readonly object writerLock = new object();
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        private Process process;
        private int nextId = 3;
        private DateTime lastRequest = DateTime.MinValue;
        private bool disposed;

        public event Action<QuotaSnapshot> SnapshotReceived;
        public event Action<string> StatusChanged;

        public void Refresh(bool force)
        {
            if (disposed) return;
            if (process == null || process.HasExited)
            {
                Start();
                return;
            }
            if (!force && DateTime.UtcNow - lastRequest < TimeSpan.FromSeconds(5)) return;
            lastRequest = DateTime.UtcNow;
            Send("{\"method\":\"account/rateLimits/read\",\"id\":" +
                 (++nextId).ToString(CultureInfo.InvariantCulture) +
                 ",\"params\":{\"excludeResetCreditDetails\":true}}");
        }

        private void Start()
        {
            if (process != null) StopProcess();
            string executable = FindCodex();
            if (executable == null)
            {
                ReportStatus("找不到 Codex。请安装 Codex 桌面版或 CLI。");
                return;
            }

            try
            {
                var info = new ProcessStartInfo(executable, "app-server");
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;

                string codexHome = Environment.GetEnvironmentVariable("CODEX_HOME");
                if (String.IsNullOrEmpty(codexHome))
                {
                    string candidate = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
                    if (Directory.Exists(candidate)) info.EnvironmentVariables["CODEX_HOME"] = candidate;
                }

                process = new Process();
                process.StartInfo = info;
                process.Start();
                Process active = process;
                Task.Run(() => ReadOutput(active));
                Task.Run(() => DrainErrors(active));

                Send("{\"method\":\"initialize\",\"id\":1,\"params\":{\"clientInfo\":{\"name\":\"tokenbaby\",\"title\":\"TokenBaby\",\"version\":\"0.1.0\"}}}");
                Send("{\"method\":\"initialized\",\"params\":{}}");
                Send("{\"method\":\"account/read\",\"id\":2,\"params\":{\"refreshToken\":false}}");
                lastRequest = DateTime.UtcNow;
                Send("{\"method\":\"account/rateLimits/read\",\"id\":3,\"params\":{\"excludeResetCreditDetails\":true}}");
                ReportStatus("正在读取 Codex 额度…");
            }
            catch (Exception ex)
            {
                ReportStatus("无法启动 Codex：" + ex.Message);
                StopProcess();
            }
        }

        private void Send(string json)
        {
            try
            {
                lock (writerLock)
                {
                    if (process == null || process.HasExited) return;
                    process.StandardInput.WriteLine(json);
                    process.StandardInput.Flush();
                }
            }
            catch (Exception)
            {
                ReportStatus("Codex 连接中断，稍后重试。");
            }
        }

        private void ReadOutput(Process active)
        {
            try
            {
                string line;
                while ((line = active.StandardOutput.ReadLine()) != null)
                {
                    HandleLine(line);
                }
            }
            catch (Exception) { }
            if (!disposed && Object.ReferenceEquals(process, active))
                ReportStatus("Codex 连接中断，稍后重试。");
        }

        private void DrainErrors(Process active)
        {
            try
            {
                while (active.StandardError.ReadLine() != null) { }
            }
            catch (Exception) { }
        }

        private void HandleLine(string line)
        {
            Dictionary<string, object> message;
            try { message = serializer.DeserializeObject(line) as Dictionary<string, object>; }
            catch (Exception) { return; }
            if (message == null) return;

            object methodValue;
            if (message.TryGetValue("method", out methodValue) &&
                String.Equals(methodValue as string, "account/rateLimits/updated", StringComparison.Ordinal))
            {
                Refresh(false);
                return;
            }

            object idValue;
            if (!message.TryGetValue("id", out idValue) || idValue == null) return;
            int id;
            try { id = Convert.ToInt32(idValue, CultureInfo.InvariantCulture); }
            catch (Exception) { return; }

            var error = QuotaParser.GetMap(message, "error");
            if (error != null)
            {
                if (id >= 2) ReportStatus("额度读取失败：" + QuotaParser.GetString(error, "message", "请检查 Codex 登录状态"));
                return;
            }

            var result = QuotaParser.GetMap(message, "result");
            if (result == null) return;
            if (id == 2)
            {
                var account = QuotaParser.GetMap(result, "account");
                string type = account == null ? null : QuotaParser.GetString(account, "type", null);
                if (type == null) ReportStatus("请先在 Codex 登录 ChatGPT 账号。");
                else if (type == "apiKey") ReportStatus("当前是 API Key 登录；请改用 ChatGPT 账号登录 Codex。");
                return;
            }
            if (id >= 3)
            {
                QuotaSnapshot snapshot = QuotaParser.ParseSnapshot(result);
                if (snapshot == null)
                    ReportStatus("Codex 未返回套餐额度，请检查账号登录方式。");
                else
                {
                    var handler = SnapshotReceived;
                    if (handler != null) handler(snapshot);
                    ReportStatus("已连接");
                }
            }
        }

        private void ReportStatus(string status)
        {
            var handler = StatusChanged;
            if (handler != null) handler(status);
        }

        private static string FindCodex()
        {
            string overridePath = Environment.GetEnvironmentVariable("TOKENBABY_CODEX_PATH");
            if (!String.IsNullOrEmpty(overridePath) && File.Exists(overridePath)) return overridePath;

            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string part in path.Split(Path.PathSeparator))
            {
                if (String.IsNullOrWhiteSpace(part)) continue;
                try
                {
                    string candidate = Path.Combine(part.Trim().Trim('"'), "codex.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch (Exception) { }
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string bin = Path.Combine(localAppData, "OpenAI", "Codex", "bin");
            if (Directory.Exists(bin))
            {
                foreach (string dir in Directory.GetDirectories(bin))
                {
                    string candidate = Path.Combine(dir, "codex.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            }
            return null;
        }

        private void StopProcess()
        {
            if (process == null) return;
            try { if (!process.HasExited) process.Kill(); }
            catch (Exception) { }
            try { process.Dispose(); }
            catch (Exception) { }
            process = null;
        }

        public void Dispose()
        {
            disposed = true;
            StopProcess();
        }
    }
}

