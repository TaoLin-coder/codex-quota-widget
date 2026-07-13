using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CodexQuotaWidget
{
    internal sealed class WidgetWindow : Window
    {
        private readonly TextBlock fivePercent;
        private readonly Ellipse fiveAlert;
        private readonly TextBlock weekPercent;
        private readonly Ellipse weekAlert;
        private readonly DispatcherTimer positionTimer;
        private uint taskbarCreatedMessage;
        private IntPtr hwnd;
        private UsageSnapshot snapshot;
        private DashboardWindow dashboard;
        private NativeMethods.RECT taskbarRect;
        private double dpiScale = 1.0;
        private IntPtr taskbarHwnd;

        public event EventHandler RefreshRequested;
        public event EventHandler ExitRequested;
        public event Action<bool> TrayIconHiddenChanged;

        public WidgetWindow()
        {
            Title = "Codex 额度任务栏组件";
            Width = 238;
            Height = 48;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            Focusable = false;

            Brush foreground = Theme.TaskbarText;
            Border hitArea = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                Padding = new Thickness(8, 3, 8, 3),
                Cursor = Cursors.Hand
            };
            Grid rows = new Grid();
            rows.RowDefinitions.Add(new RowDefinition());
            rows.RowDefinitions.Add(new RowDefinition());

            Grid first = CreateRow("5 小时", foreground, out fiveAlert, out fivePercent);
            Grid second = CreateRow("一周", foreground, out weekAlert, out weekPercent);
            Grid.SetRow(second, 1);
            rows.Children.Add(first);
            rows.Children.Add(second);
            hitArea.Child = rows;
            Content = hitArea;

            snapshot = UsageSnapshot.Loading();
            UpdateSnapshot(snapshot);
            MouseLeftButtonUp += delegate { ToggleDashboard(); };
            MouseRightButtonUp += ShowContextMenu;
            SourceInitialized += OnSourceInitialized;
            Closed += delegate { positionTimer.Stop(); };

            positionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            positionTimer.Tick += delegate { PositionBesideTaskbar(); };
        }

        private static Grid CreateRow(string label, Brush foreground, out Ellipse alert, out TextBlock percent)
        {
            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(47) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            row.ColumnDefinitions.Add(new ColumnDefinition());

            TextBlock name = new TextBlock
            {
                Text = label,
                Foreground = foreground,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontWeight = FontWeights.SemiBold,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            alert = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Visibility = Visibility.Collapsed
            };
            percent = new TextBlock
            {
                Foreground = foreground,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(alert, 1);
            Grid.SetColumn(percent, 2);
            row.Children.Add(name);
            row.Children.Add(alert);
            row.Children.Add(percent);
            return row;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            hwnd = new WindowInteropHelper(this).Handle;
            int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, style | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);
            HwndSource source = HwndSource.FromHwnd(hwnd);
            if (source != null) source.AddHook(WndProc);
            taskbarCreatedMessage = NativeMethods.RegisterWindowMessage("TaskbarCreated");
            EmbedIntoTaskbar();
            PositionBesideTaskbar();
            positionTimer.Start();
        }

        private IntPtr WndProc(IntPtr h, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if ((uint)msg == taskbarCreatedMessage)
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    EmbedIntoTaskbar();
                    PositionBesideTaskbar();
                }));
            return IntPtr.Zero;
        }

        private bool EmbedIntoTaskbar()
        {
            IntPtr found = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (found == IntPtr.Zero)
                return false;

            taskbarHwnd = found;
            if (NativeMethods.GetParent(hwnd) != taskbarHwnd)
            {
                int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
                style = (style & ~NativeMethods.WS_POPUP) | NativeMethods.WS_CHILD;
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, style);
                NativeMethods.SetParent(hwnd, taskbarHwnd);
            }
            return NativeMethods.GetParent(hwnd) == taskbarHwnd;
        }

        public void UpdateSnapshot(UsageSnapshot value)
        {
            snapshot = value;
            if (value.IsOnline)
            {
                fivePercent.Text = value.FiveHour.IsAvailable ? value.FiveHour.RemainingPercent + "%" : "--";
                fiveAlert.Visibility = value.FiveHour.IsLow ? Visibility.Visible : Visibility.Collapsed;
                weekPercent.Text = value.Weekly.IsAvailable ? value.Weekly.RemainingPercent + "%" : "--";
                weekAlert.Visibility = value.Weekly.IsLow ? Visibility.Visible : Visibility.Collapsed;
                ToolTip = "5 小时：" + (value.FiveHour.IsAvailable ? UsageText.ResetLong(value.FiveHour.ResetAfterSeconds) : "暂未提供")
                    + "\n一周：" + (value.Weekly.IsAvailable ? UsageText.ResetLong(value.Weekly.ResetAfterSeconds) : "暂未提供")
                    + "\n点击查看详情 · 更新于 " + value.UpdatedAt.ToString("HH:mm:ss");
            }
            else
            {
                fivePercent.Text = "--";
                weekPercent.Text = "--";
                fiveAlert.Visibility = Visibility.Collapsed;
                weekAlert.Visibility = Visibility.Collapsed;
                ToolTip = value.StatusMessage;
            }
            if (dashboard != null && dashboard.IsVisible)
                dashboard.UpdateSnapshot(value);
        }

        private void PositionBesideTaskbar()
        {
            if (hwnd == IntPtr.Zero) return;
            if (taskbarHwnd == IntPtr.Zero || NativeMethods.GetParent(hwnd) != taskbarHwnd)
                if (!EmbedIntoTaskbar()) return;

            NativeMethods.APPBARDATA data = new NativeMethods.APPBARDATA();
            data.cbSize = Marshal.SizeOf(typeof(NativeMethods.APPBARDATA));
            if (NativeMethods.SHAppBarMessage(NativeMethods.ABM_GETTASKBARPOS, ref data) == UIntPtr.Zero)
                return;

            taskbarRect = data.rc;
            uint dpi = 96;
            try { dpi = NativeMethods.GetDpiForWindow(hwnd); } catch { }
            if (dpi == 0) dpi = 96;
            dpiScale = dpi / 96.0;

            double barTop = taskbarRect.Top / dpiScale;
            double barWidth = (taskbarRect.Right - taskbarRect.Left) / dpiScale;
            double barHeight = (taskbarRect.Bottom - taskbarRect.Top) / dpiScale;
            bool horizontal = barWidth >= barHeight;

            if (horizontal)
            {
                Height = barHeight;
                Width = Math.Min(190, Math.Max(130, barWidth * 0.16));
            }
            else
            {
                Width = barWidth;
                Height = 48;
            }

            NativeMethods.RECT client;
            if (!NativeMethods.GetClientRect(taskbarHwnd, out client)) return;
            int physicalX = horizontal ? (int)Math.Round(12 * dpiScale) : 0;
            int physicalY = horizontal ? 0 : (int)Math.Round(12 * dpiScale);
            int physicalWidth = horizontal ? (int)Math.Round(Width * dpiScale) : client.Right - client.Left;
            int physicalHeight = horizontal ? client.Bottom - client.Top : (int)Math.Round(Height * dpiScale);
            NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOP, physicalX, physicalY, physicalWidth, physicalHeight,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOOWNERZORDER);
        }

        public void ToggleDashboard()
        {
            if (dashboard != null && dashboard.IsVisible)
            {
                dashboard.Close();
                dashboard = null;
                return;
            }

            dashboard = new DashboardWindow();
            dashboard.UpdateSnapshot(snapshot);
            dashboard.RefreshRequested += delegate { if (RefreshRequested != null) RefreshRequested(this, EventArgs.Empty); };
            dashboard.ExitRequested += delegate { if (ExitRequested != null) ExitRequested(this, EventArgs.Empty); };
            dashboard.Closed += delegate { dashboard = null; };
            PositionDashboard(dashboard);
            dashboard.Show();
            dashboard.Activate();
        }

        private void PositionDashboard(Window popup)
        {
            double barTop = taskbarRect.Top / dpiScale;
            double barBottom = taskbarRect.Bottom / dpiScale;
            bool taskbarAtTop = barTop <= 1;
            popup.Left = taskbarRect.Left / dpiScale + 12;
            popup.Top = taskbarAtTop ? barBottom + 8 : barTop - popup.Height - 8;
        }

        private void ShowContextMenu(object sender, MouseButtonEventArgs e)
        {
            ContextMenu menu = new ContextMenu();
            MenuItem refresh = new MenuItem { Header = "立即刷新" };
            refresh.Click += delegate { if (RefreshRequested != null) RefreshRequested(this, EventArgs.Empty); };
            MenuItem startup = new MenuItem { Header = "开机自动启动", IsCheckable = true, IsChecked = IsStartupEnabled() };
            startup.Click += delegate { SetStartup(startup.IsChecked); };
            MenuItem hideTray = new MenuItem { Header = "隐藏托盘图标", IsCheckable = true, IsChecked = AppSettings.TrayIconHidden };
            hideTray.Click += delegate
            {
                AppSettings.TrayIconHidden = hideTray.IsChecked;
                if (TrayIconHiddenChanged != null) TrayIconHiddenChanged(hideTray.IsChecked);
            };
            MenuItem exit = new MenuItem { Header = "退出" };
            exit.Click += delegate { if (ExitRequested != null) ExitRequested(this, EventArgs.Empty); };
            menu.Items.Add(refresh);
            menu.Items.Add(startup);
            menu.Items.Add(hideTray);
            menu.Items.Add(new Separator());
            menu.Items.Add(exit);
            menu.IsOpen = true;
            e.Handled = true;
        }

        private static bool IsStartupEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                return key != null && key.GetValue("CodexQuotaWidget") != null;
        }

        private static void SetStartup(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (enabled)
                    key.SetValue("CodexQuotaWidget", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
                else
                    key.DeleteValue("CodexQuotaWidget", false);
            }
        }
    }
}
