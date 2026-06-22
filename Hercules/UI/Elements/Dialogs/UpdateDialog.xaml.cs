using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Hercules.UI.Elements.Base;

namespace Hercules.UI.Elements.Dialogs
{
    public partial class UpdateDialog : WpfUiWindow
    {
        private readonly CancellationTokenSource _cts = new();
        private bool _restartPending;

        public UpdateDialog()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await CheckAndDownloadAsync();
        }

        private async Task CheckAndDownloadAsync()
        {
            try
            {
                TitleText.Text = "Checking for updates...";
                StatusText.Text = "Fetching latest release info from GitHub...";
                ProgressBar.IsIndeterminate = true;
                ActionButton.Content = "Cancel";

                string? latestTag = await GithubUpdater.GetLatestVersionTagAsync();
                if (string.IsNullOrEmpty(latestTag))
                {
                    TitleText.Text = "Update check failed";
                    StatusText.Text = "Could not reach GitHub. Check your internet connection.";
                    ActionButton.Content = "Close";
                    ProgressBar.IsIndeterminate = false;
                    return;
                }

                string localVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
                string normalizedTag = latestTag.TrimStart('v', 'V');

                if (!IsNewerVersion(normalizedTag, localVersion))
                {
                    TitleText.Text = "You're up to date!";
                    StatusText.Text = $"Hercules v{localVersion} is the latest version.";
                    ActionButton.Content = "Close";
                    ProgressBar.IsIndeterminate = false;
                    return;
                }

                TitleText.Text = $"Update available: v{normalizedTag}";
                StatusText.Text = "Downloading Hercules...";
                ActionButton.Content = "Cancel";
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 0;

                var progress = new Progress<double>(p =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = p;
                        StatusText.Text = $"Downloading... {p * 100:F0}%";
                    });
                });

                bool success = await GithubUpdater.DownloadAndInstallUpdate(latestTag, progress);

                if (_cts.IsCancellationRequested)
                {
                    TitleText.Text = "Update cancelled";
                    StatusText.Text = "The update was cancelled.";
                    ActionButton.Content = "Close";
                    return;
                }

                if (success)
                {
                    TitleText.Text = "Update ready!";
                    StatusText.Text = "Hercules will now restart to apply the update.";
                    ActionButton.Content = "Restart now";
                    ProgressBar.Value = 1.0;
                    _restartPending = true;
                }
                else
                {
                    TitleText.Text = "Update failed";
                    StatusText.Text = "The update could not be installed. Check the logs for details.";
                    ActionButton.Content = "Close";
                }
            }
            catch (OperationCanceledException)
            {
                TitleText.Text = "Update cancelled";
                StatusText.Text = "The update was cancelled.";
                ActionButton.Content = "Close";
            }
            catch (Exception ex)
            {
                TitleText.Text = "Update error";
                StatusText.Text = ex.Message;
                ActionButton.Content = "Close";
                App.Logger.WriteException("UpdateDialog", ex);
            }
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_restartPending)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(Environment.ProcessPath!)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("UpdateDialog::Restart", ex);
                }
                Application.Current.Shutdown();
                return;
            }

            string? buttonText = ActionButton.Content as string;
            if (buttonText == "Cancel")
            {
                _cts.Cancel();
                ActionButton.IsEnabled = false;
            }

            Close();
        }

        private static bool IsNewerVersion(string remote, string local)
        {
            if (Version.TryParse(remote, out var rv) && Version.TryParse(local, out var lv))
                return rv > lv;
            return false;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _cts.Cancel();
            base.OnClosing(e);
        }
    }
}
