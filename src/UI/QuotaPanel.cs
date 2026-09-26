using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace TokenBaby
{
    internal sealed class QuotaPanel : Window
    {
        private sealed class QuotaSection
        {
            public TextBlock Value;
            public TextBlock Reset;
            public Border Fill;
        }

        private readonly QuotaSection fiveHour;
        private readonly QuotaSection sevenDay;
        private readonly TextBlock status;
        private readonly TextBlock updated;

        public event Action RefreshClicked;
        public event Action CloseClicked;

        public QuotaPanel()
        {
            Title = "Codex 额度";
            Width = 292;
            Height = 288;
            Topmost = true;
            ShowInTaskbar = false;
#if DEBUG_UI
            ShowInTaskbar = true;
            AllowsTransparency = false;
            Background = Brushes.White;
            WindowStyle = WindowStyle.SingleBorderWindow;
#else
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStyle = WindowStyle.None;
#endif
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.Manual;

            var card = new Border();
            card.Margin = new Thickness(6);
            card.CornerRadius = new CornerRadius(19);
            card.Background = new SolidColorBrush(Color.FromRgb(255, 253, 247));
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 225, 208));
            card.BorderThickness = new Thickness(1);
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 3,
                Opacity = 0.18,
                Color = Colors.Black
            };
            Content = card;

            var content = new StackPanel();
            content.Margin = new Thickness(18, 14, 18, 12);
            card.Child = content;

            var heading = new Grid();
            heading.ColumnDefinitions.Add(new ColumnDefinition());
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.Children.Add(heading);
            var title = new TextBlock
            {
                Text = "Codex 额度",
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                Foreground = Ink()
            };
            heading.Children.Add(title);
            var close = MakeButton("×", 30);
            close.FontSize = 19;
            close.Padding = new Thickness(0);
            close.Click += (s, e) =>
            {
                var handler = CloseClicked;
                if (handler != null) handler();
            };
            Grid.SetColumn(close, 1);
            heading.Children.Add(close);

            status = MakeText("正在连接 Codex…", 11, Color.FromRgb(117, 111, 95));
            status.Margin = new Thickness(0, 1, 0, 11);
            content.Children.Add(status);
            fiveHour = AddSection(content, "5 小时");
            sevenDay = AddSection(content, "7 天");

            var footer = new Grid();
            footer.Margin = new Thickness(0, 8, 0, 0);
            footer.ColumnDefinitions.Add(new ColumnDefinition());
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.Children.Add(footer);
            updated = MakeText("尚未刷新", 10, Color.FromRgb(132, 126, 112));
            updated.VerticalAlignment = VerticalAlignment.Center;
            footer.Children.Add(updated);
            var refresh = MakeButton("刷新", 47);
            refresh.Click += (s, e) =>
            {
                var handler = RefreshClicked;
                if (handler != null) handler();
            };
            Grid.SetColumn(refresh, 1);
            footer.Children.Add(refresh);
        }

        private static QuotaSection AddSection(StackPanel content, string label)
        {
            var section = new QuotaSection();
            var row = new Grid();
            row.Margin = new Thickness(0, 0, 0, 3);
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.Children.Add(row);
            row.Children.Add(MakeText(label, 13, Color.FromRgb(68, 62, 48)));
            section.Value = MakeText("—", 14, Color.FromRgb(46, 77, 65));
            section.Value.FontWeight = FontWeights.Bold;
            Grid.SetColumn(section.Value, 1);
            row.Children.Add(section.Value);

            var track = new Border
            {
                Width = 235,
                Height = 9,
                Background = new SolidColorBrush(Color.FromRgb(232, 231, 222)),
                CornerRadius = new CornerRadius(5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 3)
            };
            var barGrid = new Grid { Width = 235, Height = 9, ClipToBounds = true };
            section.Fill = new Border
            {
                Width = 0,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Color.FromRgb(96, 175, 130)),
                CornerRadius = new CornerRadius(5)
            };
            barGrid.Children.Add(section.Fill);
            track.Child = barGrid;
            content.Children.Add(track);
            section.Reset = MakeText("重置时间未知", 10, Color.FromRgb(132, 126, 112));
            section.Reset.Margin = new Thickness(0, 0, 0, 10);
            content.Children.Add(section.Reset);
            return section;
        }

        private static TextBlock MakeText(string text, double size, Color color)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = size,
                Foreground = new SolidColorBrush(color)
            };
        }

        private static Brush Ink()
        {
            return new SolidColorBrush(Color.FromRgb(57, 52, 38));
        }

        private static Button MakeButton(string text, double width)
        {
            return new Button
            {
                Content = text,
                Width = width,
                Height = 27,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                Foreground = Ink(),
                Background = new SolidColorBrush(Color.FromRgb(247, 242, 226)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(223, 215, 193)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
        }

        public void ApplySnapshot(QuotaSnapshot snapshot)
        {
            UpdateSection(fiveHour, snapshot.FiveHour);
            UpdateSection(sevenDay, snapshot.SevenDay);
            updated.Text = "更新于 " + snapshot.FetchedAt.ToString("HH:mm:ss");
        }

        public void SetStatus(string text)
        {
            status.Text = text;
        }

        private static void UpdateSection(QuotaSection section, QuotaWindow window)
        {
            if (window == null || !window.RemainingPercent.HasValue)
            {
                section.Value.Text = "—";
                section.Reset.Text = "重置时间未知";
                section.Fill.Width = 0;
                return;
            }
            double remaining = window.RemainingPercent.Value;
            section.Value.Text = "剩余 " + remaining.ToString("0.#") + "%";
            section.Fill.Width = 235 * remaining / 100;
            section.Fill.Background = new SolidColorBrush(remaining <= 20
                ? Color.FromRgb(211, 126, 83)
                : remaining <= 70 ? Color.FromRgb(214, 179, 82) : Color.FromRgb(96, 175, 130));
            section.Reset.Text = window.ResetsAt.HasValue
                ? "重置于 " + window.ResetsAt.Value.ToString("M月d日 HH:mm")
                : "重置时间未知";
        }

        public void PositionNear(Window pet)
        {
            Rect area = SystemParameters.WorkArea;
            double x = pet.Left - Width - 7;
            if (x < area.Left) x = pet.Left + pet.Width + 7;
            Left = Math.Max(area.Left, Math.Min(x, area.Right - Width));
            Top = Math.Max(area.Top, Math.Min(pet.Top + 12, area.Bottom - Height));
        }
    }
}

