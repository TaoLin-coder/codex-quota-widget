using System;
using System.Windows;

namespace CodexQuotaWidget
{
    internal static class PreviewProgram
    {
        [STAThread]
        private static void Main()
        {
            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            DashboardWindow window = new DashboardWindow();
            window.ShowInTaskbar = true;
            window.Topmost = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            app.MainWindow = window;
            window.Loaded += async delegate
            {
                UsageSnapshot snapshot = await new UsageClient().FetchAsync();
                window.UpdateSnapshot(snapshot);
            };
            app.Run(window);
        }
    }
}
