using Microsoft.Win32;

namespace Hercules.UI.Elements.Settings.Pages
{
    public partial class MobilePage
    {
        private static readonly Uri RemoteDesktopSetupUri = new("https://remotedesktop.google.com/headless");

        public MobilePage()
        {
            InitializeComponent();
            Loaded += MobilePage_Loaded;
        }

        private void MobilePage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            bool installed = IsChromeRemoteDesktopInstalled();
            StatusText.Text = installed
                ? "Chrome Remote Desktop is already installed."
                : "Chrome Remote Desktop is not installed. Setup opens only when you click the button.";
            CompletionCard.Visibility = installed
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
        }

        private void OpenRemoteDesktopSetup_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(RemoteDesktopSetupUri.AbsoluteUri)
                {
                    UseShellExecute = true
                });
                StatusText.Text = "Chrome Remote Desktop setup opened in your browser.";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MobilePage::OpenRemoteDesktopSetup", ex);
                StatusText.Text = $"Could not open setup. Visit {RemoteDesktopSetupUri}";
            }
        }

        private static bool IsChromeRemoteDesktopInstalled()
        {
            string[] uninstallKeys =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (string keyPath in uninstallKeys)
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath);
                if (key is null)
                    continue;

                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    using RegistryKey? subKey = key.OpenSubKey(subKeyName);
                    if (subKey?.GetValue("DisplayName") is string displayName &&
                        displayName.Contains("Chrome Remote Desktop Host", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
