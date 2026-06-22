using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Hercules.UI.Elements.Dialogs;

namespace Hercules.Integrations
{
    public sealed class AutoUpdateService : IDisposable
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
        private CancellationTokenSource? _cts;
        private Task? _loop;

        public void Start()
        {
            if (_cts is not null)
                return;

            _cts = new CancellationTokenSource();
            _loop = RunAsync(_cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _loop = null;
        }

        private async Task RunAsync(CancellationToken ct)
        {
            if (!App.Settings.Prop.CheckForUpdates)
            {
                App.Logger.WriteLine("AutoUpdateService", "Update checking is disabled in settings.");
                return;
            }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    App.Logger.WriteLine("AutoUpdateService", "Background update check...");
                    string? latestTag = await GithubUpdater.GetLatestVersionTagAsync();
                    if (!string.IsNullOrEmpty(latestTag) && IsNewerThanLocal(latestTag))
                    {
                        App.Logger.WriteLine("AutoUpdateService", $"Update available: {latestTag}");
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            App.Logger.WriteLine("AutoUpdateService", "Showing update dialog (background check)");
                            var dialog = new UpdateDialog();
                            if (Application.Current.MainWindow is Window owner)
                                dialog.Owner = owner;
                            dialog.ShowDialog();
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("AutoUpdateService", ex);
                }

                try
                {
                    await Task.Delay(CheckInterval, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static bool IsNewerThanLocal(string tag)
        {
            string local = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            string remote = tag.TrimStart('v', 'V');
            if (Version.TryParse(remote, out var rv) && Version.TryParse(local, out var lv))
                return rv > lv;
            return false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
