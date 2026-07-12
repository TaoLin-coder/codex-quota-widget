using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace CodexQuotaWidget
{
    internal sealed class DashboardWindow : Window
    {
        private readonly TextBlock planText;
        private readonly TextBlock statusText;
        private readonly TextBlock updatedText;
        private readonly TextBlock creditsText;
        private readonly QuotaCard fiveHourCard;
        private readonly QuotaCard weeklyCard;

        public event EventHandler RefreshRequested;
        public event EventHandler ExitRequested;

        public DashboardWindow()
        {
            Title = "Codex 用量看板";
            Width = 350;
            Height = 332;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;

            Border shell = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 29, 32)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(65, 67, 73)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18),
                Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 5, Opacity = 0.38, Color = Colors.Black }
            };

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition());
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            StackPanel title = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            title.Children.Add(new Ellipse { Width = 9, Height = 9, Fill = new SolidColorBrush(Color.FromRgb(52, 211, 153)), Margin = new Thickness(0, 0, 9, 0) });
            title.Children.Add(new TextBlock { Text = "Codex 用量", FontFamily = new FontFamily("Microsoft YaHei UI"), FontWeight = FontWeights.SemiBold, FontSize = 17, Foreground = Brushes.White });
            planText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(168, 171, 181)),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 3, 8, 3)
            };
            Grid.SetColumn(planText, 1);
            header.Children.Add(title);
            header.Children.Add(planText);

            fiveHourCard = new QuotaCard("5 小时额度");
            weeklyCard = new QuotaCard("一周额度");

            Grid footer = new Grid();
            footer.ColumnDefinitions.Add(new ColumnDefinition());
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            StackPanel info = new StackPanel();
            statusText = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(168, 171, 181)) };
            updatedText = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(119, 122, 132)), Margin = new Thickness(0, 3, 0, 0) };
            creditsText = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(119, 122, 132)), Margin = new Thickness(0, 3, 0, 0) };
            info.Children.Add(statusText);
            info.Children.Add(updatedText);
            info.Children.Add(creditsText);
            Button refresh = new Button
            {
                Content = "刷新",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(53, 55, 61)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(76, 78, 86)),
                Padding = new Thickness(14, 7, 14, 7),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            refresh.Click += delegate { if (RefreshRequested != null) RefreshRequested(this, EventArgs.Empty); };
            Button exit = new Button
            {
                Content = "退出程序",
                Foreground = new SolidColorBrush(Color.FromRgb(239, 160, 160)),
                Background = new SolidColorBrush(Color.FromRgb(53, 55, 61)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(76, 78, 86)),
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(7, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            exit.Click += delegate { if (ExitRequested != null) ExitRequested(this, EventArgs.Empty); };
            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            actions.Children.Add(refresh);
            actions.Children.Add(exit);
            Grid.SetColumn(actions, 1);
            footer.Children.Add(info);
            footer.Children.Add(actions);

            Grid.SetRow(header, 0);
            Grid.SetRow(fiveHourCard, 2);
            Grid.SetRow(weeklyCard, 4);
            Grid.SetRow(footer, 6);
            root.Children.Add(header);
            root.Children.Add(fiveHourCard);
            root.Children.Add(weeklyCard);
            root.Children.Add(footer);
            shell.Child = root;
            Content = shell;

            Deactivated += delegate { Close(); };
            PreviewKeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Close(); };
        }

        public void UpdateSnapshot(UsageSnapshot snapshot)
        {
            planText.Text = String.IsNullOrWhiteSpace(snapshot.PlanType) ? "" : snapshot.PlanType.ToUpperInvariant();
            statusText.Text = snapshot.StatusMessage;
            updatedText.Text = snapshot.UpdatedAt == default(DateTime) ? "" : "更新于 " + snapshot.UpdatedAt.ToString("HH:mm:ss");
            creditsText.Text = snapshot.IsOnline ? "额外 Credits：" + snapshot.CreditsBalance + "    可用重置：" + snapshot.AvailableResetCredits : "";
            fiveHourCard.Update(snapshot.FiveHour, snapshot.IsOnline);
            weeklyCard.Update(snapshot.Weekly, snapshot.IsOnline);
        }

        private sealed class QuotaCard : Border
        {
            private readonly TextBlock percentText;
            private readonly TextBlock resetText;
            private readonly Border fill;
            private readonly Grid bar;

            public QuotaCard(string title)
            {
                Background = new SolidColorBrush(Color.FromRgb(37, 39, 43));
                CornerRadius = new CornerRadius(7);
                Padding = new Thickness(13, 10, 13, 10);
                Grid root = new Grid();
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(9) });
                root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });

                Grid text = new Grid();
                text.ColumnDefinitions.Add(new ColumnDefinition());
                text.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                StackPanel left = new StackPanel { Orientation = Orientation.Horizontal };
                left.Children.Add(new TextBlock { Text = title, Foreground = new SolidColorBrush(Color.FromRgb(218, 220, 225)), FontSize = 13, FontWeight = FontWeights.SemiBold });
                resetText = new TextBlock { Foreground = new SolidColorBrush(Color.FromRgb(139, 142, 151)), FontSize = 11, Margin = new Thickness(9, 2, 0, 0) };
                left.Children.Add(resetText);
                percentText = new TextBlock { Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.SemiBold };
                Grid.SetColumn(percentText, 1);
                text.Children.Add(left);
                text.Children.Add(percentText);

                bar = new Grid { ClipToBounds = true, Background = new SolidColorBrush(Color.FromRgb(60, 62, 68)) };
                fill = new Border { HorizontalAlignment = HorizontalAlignment.Left };
                bar.Children.Add(fill);
                bar.SizeChanged += delegate { ApplyFillWidth(); };

                Grid.SetRow(text, 0);
                Grid.SetRow(bar, 2);
                root.Children.Add(text);
                root.Children.Add(bar);
                Child = root;
            }

            private int remaining;

            public void Update(RateWindow window, bool online)
            {
                remaining = online ? window.RemainingPercent : 0;
                percentText.Text = online ? "剩余 " + remaining + "%" : "--";
                resetText.Text = online ? UsageText.ResetLong(window.ResetAfterSeconds) : "等待数据";
                fill.Background = online ? Theme.AccentFor(remaining) : new SolidColorBrush(Color.FromRgb(100, 103, 112));
                ApplyFillWidth();
            }

            private void ApplyFillWidth()
            {
                fill.Width = Math.Max(0, bar.ActualWidth * remaining / 100.0);
                fill.Height = bar.ActualHeight;
            }
        }
    }
}
