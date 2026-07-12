using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace CodexQuotaWidget
{
    internal static class Program
    {
        private static readonly UsageClient client = new UsageClient();
        private static WidgetWindow widget;
        private static DispatcherTimer refreshTimer;
        private static bool refreshing;
        private static Mutex singleInstance;
        private static Forms.NotifyIcon trayIcon;

        [STAThread]
        private static void Main()
        {
            bool created;
            singleInstance = new Mutex(true, @"Local\CodexQuotaWidget", out created);
            if (!created) return;

            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            widget = new WidgetWindow();
            widget.RefreshRequested += async delegate { await RefreshAsync(); };
            widget.ExitRequested += delegate { app.Shutdown(); };
            widget.TrayIconHiddenChanged += delegate(bool hidden)
            {
                if (trayIcon != null) trayIcon.Visible = !hidden;
            };
            widget.Show();
            CreateTrayIcon(app);

            refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            refreshTimer.Tick += async delegate { await RefreshAsync(); };
            refreshTimer.Start();
            widget.Dispatcher.BeginInvoke(new Action(async delegate { await RefreshAsync(); }), DispatcherPriority.Background);
            app.Exit += delegate
            {
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
                if (singleInstance != null) singleInstance.Dispose();
            };
            app.Run();
        }

        private static void CreateTrayIcon(Application app)
        {
            trayIcon = new Forms.NotifyIcon();
            trayIcon.Text = "Codex 额度看板";
            trayIcon.Icon = System.Drawing.SystemIcons.Information;
            Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
            menu.Items.Add("显示用量看板", null, delegate { widget.Dispatcher.BeginInvoke(new Action(widget.ToggleDashboard)); });
            menu.Items.Add("立即刷新", null, async delegate { await RefreshAsync(); });
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("退出", null, delegate { widget.Dispatcher.BeginInvoke(new Action(app.Shutdown)); });
            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += delegate { widget.Dispatcher.BeginInvoke(new Action(widget.ToggleDashboard)); };
            trayIcon.Visible = !AppSettings.TrayIconHidden;
        }

        private static async Task RefreshAsync()
        {
            if (refreshing) return;
            refreshing = true;
            try
            {
                UsageSnapshot snapshot = await client.FetchAsync();
                widget.UpdateSnapshot(snapshot);
            }
            finally
            {
                refreshing = false;
            }
        }
    }
}
