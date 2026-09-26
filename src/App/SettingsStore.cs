using System;
using System.IO;
using System.Windows;

namespace TokenBaby
{
    internal static class SettingsStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TokenBaby", "position.txt");

        public static void Load(Window window)
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                string[] lines = File.ReadAllLines(FilePath);
                if (lines.Length < 2) return;
                double left, top;
                if (Double.TryParse(lines[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out left) &&
                    Double.TryParse(lines[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out top))
                {
                    Rect work = SystemParameters.WorkArea;
                    window.Left = Math.Max(work.Left, Math.Min(left, work.Right - window.Width));
                    window.Top = Math.Max(work.Top, Math.Min(top, work.Bottom - window.Height));
                }
            }
            catch (Exception) { }
        }

        public static void Save(Window window)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, new[]
                {
                    window.Left.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    window.Top.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
            }
            catch (Exception) { }
        }
    }
}

