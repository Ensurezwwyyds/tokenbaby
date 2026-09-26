using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace TokenBaby
{
    internal sealed class TokenBabyApp : Application
    {
        private PetWindow pet;
        private QuotaPanel panel;
        private CodexClient client;
        private WinForms.NotifyIcon tray;
        private DispatcherTimer refreshTimer;

        [STAThread]
        private static void Main()
        {
            var app = new TokenBabyApp();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            app.Startup += app.OnStartup;
            app.Exit += app.OnExit;
            app.Run();
        }

        private void OnStartup(object sender, StartupEventArgs args)
        {
            pet = new PetWindow();
            panel = new QuotaPanel();
            client = new CodexClient();

            pet.ActivatedByClick += TogglePanel;
            pet.RightClicked += ShowTrayMenu;
            panel.RefreshClicked += () => client.Refresh(true);
            panel.CloseClicked += () => panel.Hide();

            client.SnapshotReceived += snapshot =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    panel.ApplySnapshot(snapshot);
                    pet.ApplySnapshot(snapshot);
                }));
            client.StatusChanged += status =>
                Dispatcher.BeginInvoke(new Action(() => panel.SetStatus(status)));

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("显示 / 隐藏宠物", null, (s, e) => TogglePet());
            menu.Items.Add("刷新额度", null, (s, e) => client.Refresh(true));
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("退出", null, (s, e) => Shutdown());
            tray = new WinForms.NotifyIcon();
            tray.Icon = MakeIcon();
            tray.Text = "TokenBaby · Codex 桌宠";
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            tray.DoubleClick += (s, e) => TogglePet();

            pet.Show();
#if DEBUG_UI
            panel.PositionNear(pet);
            panel.Show();
#endif
            client.Refresh(true);
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMinutes(1);
            refreshTimer.Tick += (s, e) => client.Refresh(false);
            refreshTimer.Start();
        }

        private void TogglePet()
        {
            if (pet.IsVisible)
            {
                panel.Hide();
                pet.Hide();
            }
            else
            {
                pet.Show();
                pet.Activate();
            }
        }

        private void TogglePanel()
        {
            if (panel.IsVisible)
                panel.Hide();
            else
            {
                panel.PositionNear(pet);
                panel.Show();
                panel.Activate();
            }
        }

        private void ShowTrayMenu()
        {
            tray.ContextMenuStrip.Show(WinForms.Cursor.Position);
        }

        private void OnExit(object sender, ExitEventArgs args)
        {
            if (refreshTimer != null) refreshTimer.Stop();
            if (client != null) client.Dispose();
            if (tray != null)
            {
                tray.Visible = false;
                tray.Dispose();
            }
        }

        private static Icon MakeIcon()
        {
            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                using (var body = new SolidBrush(Color.FromArgb(255, 224, 85)))
                    graphics.FillEllipse(body, 4, 2, 24, 27);
                using (var eye = new SolidBrush(Color.FromArgb(112, 169, 93)))
                using (var pupil = new SolidBrush(Color.FromArgb(35, 37, 28)))
                {
                    graphics.FillEllipse(eye, 10, 10, 5, 6);
                    graphics.FillEllipse(eye, 19, 10, 5, 6);
                    graphics.FillEllipse(pupil, 12, 12, 2, 3);
                    graphics.FillEllipse(pupil, 21, 12, 2, 3);
                }
                using (var pen = new Pen(Color.FromArgb(86, 65, 38), 1.5f))
                    graphics.DrawArc(pen, 13, 16, 8, 6, 10, 160);
                IntPtr handle = bitmap.GetHicon();
                try { return (Icon)Icon.FromHandle(handle).Clone(); }
                finally { DestroyIcon(handle); }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}

