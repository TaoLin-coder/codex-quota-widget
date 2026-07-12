using Microsoft.Win32;
using System.Windows.Media;

namespace CodexQuotaWidget
{
    internal static class Theme
    {
        public static bool IsLightTaskbar()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object value = key == null ? null : key.GetValue("SystemUsesLightTheme");
                    return value != null && System.Convert.ToInt32(value) != 0;
                }
            }
            catch { return false; }
        }

        public static Brush TaskbarText
        {
            get { return IsLightTaskbar() ? Brushes.Black : Brushes.White; }
        }

        public static SolidColorBrush AccentFor(int remaining)
        {
            if (remaining <= 20) return new SolidColorBrush(Color.FromRgb(239, 68, 68));
            if (remaining <= 40) return new SolidColorBrush(Color.FromRgb(245, 158, 11));
            return new SolidColorBrush(Color.FromRgb(52, 211, 153));
        }
    }
}
