using Microsoft.Win32;

namespace CodexQuotaWidget
{
    internal static class AppSettings
    {
        private const string SettingsPath = @"Software\CodexQuotaWidget";

        public static bool TrayIconHidden
        {
            get
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsPath))
                    {
                        object value = key == null ? null : key.GetValue("HideTrayIcon");
                        return value != null && System.Convert.ToInt32(value) != 0;
                    }
                }
                catch { return false; }
            }
            set
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SettingsPath))
                        key.SetValue("HideTrayIcon", value ? 1 : 0, RegistryValueKind.DWord);
                }
                catch { }
            }
        }
    }
}
