using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TokenBaby
{
    internal enum PetMood { Unknown, Low, Mid, High }

    internal sealed class PetWindow : Window
    {
        private readonly Image image;
        private readonly BitmapImage lowSource;
        private readonly BitmapImage midSource;
        private readonly BitmapImage highSource;
        private readonly BitmapImage crySource;
        private readonly BitmapImage glanceSource;
        private readonly BitmapImage laughSource;
        private readonly ScaleTransform scale = new ScaleTransform(1, 1);
        private readonly RotateTransform rotate = new RotateTransform(0);
        private readonly TranslateTransform translate = new TranslateTransform(0, 0);
        private readonly DispatcherTimer idleTimer;
        private readonly DispatcherTimer clickTimer;
        private readonly Random random = new Random();
        private PetMood mood = PetMood.Unknown;
        private PetMood clickMood = PetMood.Unknown;
        private int clickFrame;

        public event Action ActivatedByClick;
        public event Action RightClicked;

        public PetWindow()
        {
            Title = "TokenBaby";
            Width = 112;
            Height = 140;
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
            Left = SystemParameters.WorkArea.Right - Width - 24;
            Top = SystemParameters.WorkArea.Bottom - Height - 24;
            SettingsStore.Load(this);
            Cursor = Cursors.Hand;

            lowSource = LoadImage("pet-low.png");
            midSource = LoadImage("pet-mid.png");
            highSource = LoadImage("pet-high.png");
            crySource = LoadImage("pet-cry.png");
            glanceSource = LoadImage("pet-glance.png");
            laughSource = LoadImage("pet-laugh.png");

            var root = new Grid();
            Content = root;
            image = new Image();
            image.Source = midSource;
            image.Stretch = Stretch.Uniform;
            image.RenderTransformOrigin = new Point(0.5, 0.88);
            var transforms = new TransformGroup();
            transforms.Children.Add(scale);
            transforms.Children.Add(rotate);
            transforms.Children.Add(translate);
            image.RenderTransform = transforms;
            root.Children.Add(image);

            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseRightButtonUp += (s, e) =>
            {
                var handler = RightClicked;
                if (handler != null) handler();
                e.Handled = true;
            };

            idleTimer = new DispatcherTimer();
            idleTimer.Interval = TimeSpan.FromSeconds(5);
            idleTimer.Tick += (s, e) => RunIdleAction();
            idleTimer.Start();
            clickTimer = new DispatcherTimer();
            clickTimer.Tick += (s, e) => AdvanceClickAction();
        }

        private static BitmapImage LoadImage(string name)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", name);
            if (!File.Exists(path)) return null;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            double beforeLeft = Left;
            double beforeTop = Top;
            try { DragMove(); }
            catch (InvalidOperationException) { }
            double moved = Math.Abs(Left - beforeLeft) + Math.Abs(Top - beforeTop);
            if (moved < 5)
            {
                StartClickAction();
                var handler = ActivatedByClick;
                if (handler != null) handler();
            }
            else SettingsStore.Save(this);
            e.Handled = true;
        }

        public void ApplySnapshot(QuotaSnapshot snapshot)
        {
            double? remaining = snapshot.FiveHour == null ? null : snapshot.FiveHour.RemainingPercent;
            if (!remaining.HasValue) mood = PetMood.Unknown;
            else if (remaining.Value <= 20) mood = PetMood.Low;
            else if (remaining.Value <= 70) mood = PetMood.Mid;
            else mood = PetMood.High;
            if (!clickTimer.IsEnabled) SetBasePose();
        }

        private void SetBasePose()
        {
            image.Source = mood == PetMood.Low ? lowSource
                : mood == PetMood.High ? highSource : midSource;
        }

        private void StartClickAction()
        {
            clickTimer.Stop();
            idleTimer.Stop();
            ResetTransforms();
            clickMood = mood == PetMood.Unknown ? PetMood.Mid : mood;
            clickFrame = 0;
            clickTimer.Interval = TimeSpan.FromMilliseconds(clickMood == PetMood.Mid ? 220 : 190);
            AdvanceClickAction();
            clickTimer.Start();
        }

        private void AdvanceClickAction()
        {
            int frame = clickFrame++;
            if (clickMood == PetMood.High)
            {
                if (frame == 0) SetActionFrame(highSource, 1, 0.94, -2, 3);
                else if (frame == 1) SetActionFrame(laughSource, 1.04, 1.06, 3, -4);
                else if (frame == 2) SetActionFrame(laughSource, 0.98, 0.98, -3, 1);
                else if (frame == 3) SetActionFrame(laughSource, 1.04, 1.06, 3, -4);
                else if (frame == 4) SetActionFrame(laughSource, 1, 1, -1, 0);
                else EndClickAction();
            }
            else if (clickMood == PetMood.Low)
            {
                if (frame == 0) SetActionFrame(lowSource, 0.98, 0.96, -2, 3);
                else if (frame == 1) SetActionFrame(crySource, 1, 0.98, 2, 1);
                else if (frame == 2) SetActionFrame(crySource, 0.98, 0.96, -2, 3);
                else if (frame == 3) SetActionFrame(crySource, 1, 0.98, 2, 1);
                else if (frame == 4) SetActionFrame(crySource, 0.99, 0.96, 0, 3);
                else EndClickAction();
            }
            else
            {
                if (frame == 0) SetActionFrame(midSource, 1, 1, -2, 0);
                else if (frame == 1) SetActionFrame(glanceSource, 1.01, 1.01, 1, -1);
                else if (frame == 2) SetActionFrame(glanceSource, 1.01, 1.01, 0, -1);
                else if (frame == 3) SetActionFrame(glanceSource, 1, 1, 2, 0);
                else EndClickAction();
            }
        }

        private void SetActionFrame(BitmapImage pose, double xScale, double yScale,
            double angle, double y)
        {
            if (pose != null) image.Source = pose;
            AnimateTo(scale, ScaleTransform.ScaleXProperty, xScale);
            AnimateTo(scale, ScaleTransform.ScaleYProperty, yScale);
            AnimateTo(rotate, RotateTransform.AngleProperty, angle);
            AnimateTo(translate, TranslateTransform.YProperty, y);
        }

        private static void AnimateTo(Animatable target, DependencyProperty property, double to)
        {
            var animation = new DoubleAnimation
            {
                From = (double)target.GetValue(property),
                To = to,
                Duration = TimeSpan.FromMilliseconds(160),
                FillBehavior = FillBehavior.HoldEnd
            };
            target.BeginAnimation(property, animation);
        }

        private void EndClickAction()
        {
            clickTimer.Stop();
            ResetTransforms();
            SetBasePose();
            idleTimer.Start();
        }

        private void ResetTransforms()
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            rotate.BeginAnimation(RotateTransform.AngleProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);
            scale.ScaleX = scale.ScaleY = 1;
            rotate.Angle = translate.Y = 0;
        }

        private void RunIdleAction()
        {
            if (clickTimer.IsEnabled) return;
            idleTimer.Interval = TimeSpan.FromSeconds(4 + random.Next(5));
            int choice = random.Next(4);
            if (mood == PetMood.Low)
            {
                if (choice % 2 == 0) Animate(rotate, RotateTransform.AngleProperty, -1.5, 1.5, 1100);
                else Animate(scale, ScaleTransform.ScaleYProperty, 1, 1.025, 1000);
                return;
            }
            if (choice == 1 && mood == PetMood.High)
                Animate(translate, TranslateTransform.YProperty, 0, -5, 380);
            else if (choice == 2)
                Animate(rotate, RotateTransform.AngleProperty, -2, 2, 700);
            else
                Animate(scale, ScaleTransform.ScaleYProperty, 1, 1.025, 800);
        }

        private static void Animate(Animatable target, DependencyProperty property,
            double from, double to, int milliseconds)
        {
            var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(milliseconds));
            animation.AutoReverse = true;
            animation.FillBehavior = FillBehavior.Stop;
            target.BeginAnimation(property, animation);
        }
    }
}

