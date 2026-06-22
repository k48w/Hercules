using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Hercules.Integrations;
using Hercules.UI.Elements.Dialogs;
using Hercules.UI.ViewModels.Settings;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Hercules
{
    public static class LaunchHandler
    {
        public static void ProcessNextAction(NextAction action, bool isUnfinishedInstall = false)
        {
            const string LOG_IDENT = "LaunchHandler::ProcessNextAction";

            switch (action)
            {
                case NextAction.LaunchSettings:
                    App.Logger.WriteLine(LOG_IDENT, "Opening settings");
                    LaunchSettings();
                    break;

                case NextAction.LaunchRoblox:
                    App.Logger.WriteLine(LOG_IDENT, "Opening Roblox");
                    LaunchRoblox(LaunchMode.Player);
                    break;

                case NextAction.LaunchRobloxStudio:
                    App.Logger.WriteLine(LOG_IDENT, "Opening Roblox Studio");
                    LaunchRoblox(LaunchMode.Studio);
                    break;

                default:
                    App.Logger.WriteLine(LOG_IDENT, "Closing");
                    App.Terminate(isUnfinishedInstall ? ErrorCode.ERROR_INSTALL_USEREXIT : ErrorCode.ERROR_SUCCESS);
                    break;
            }
        }

        public static void ProcessLaunchArgs()
        {
            const string LOG_IDENT = "LaunchHandler::ProcessLaunchArgs";
            if (App.LaunchSettings.UninstallFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening uninstaller");
                LaunchUninstaller();
            }
            else if (App.LaunchSettings.MenuFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening settings");
                LaunchSettings();
            }
            else if (App.LaunchSettings.WatcherFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening watcher");
                LaunchWatcher();
            }
            else if (App.LaunchSettings.RobloxLaunchMode != LaunchMode.None)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Opening bootstrapper ({App.LaunchSettings.RobloxLaunchMode})");
                LaunchRoblox(App.LaunchSettings.RobloxLaunchMode);
            }
            else if (App.LaunchSettings.ProtocolFlag.Active && !string.IsNullOrEmpty(App.LaunchSettings.ProtocolFlag.Data))
            {
                App.Logger.WriteLine(LOG_IDENT, $"Handling protocol: {App.LaunchSettings.ProtocolFlag.Data}");
                ProcessProtocol(App.LaunchSettings.ProtocolFlag.Data);
            }
            else if (App.LaunchSettings.BloxshadeFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening Bloxshade");
                LaunchBloxshadeConfig();
            }
            else if (!App.LaunchSettings.QuietFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening menu");
                LaunchMenu();
            }
            else
            {
                App.Logger.WriteLine(LOG_IDENT, "Closing - quiet flag active");
                App.Terminate();
            }
        }

        public static void LaunchInstaller()
        {
            var interlock = new InterProcessLock("Installer");

            try
            {
                if (!interlock.IsAcquired)
                {
                    Frontend.ShowMessageBox(Strings.Dialog_AlreadyRunning_Installer, MessageBoxImage.Stop);
                    App.Terminate();
                    return;
                }

                if (App.LaunchSettings.UninstallFlag.Active)
                {
                    Frontend.ShowMessageBox(Strings.Bootstrapper_FirstRunUninstall, MessageBoxImage.Error);
                    App.Terminate(ErrorCode.ERROR_INVALID_FUNCTION);
                    return;
                }

                if (App.LaunchSettings.QuietFlag.Active)
                {
                    var installer = new Installer();

                    if (!installer.CheckInstallLocation())
                        App.Terminate(ErrorCode.ERROR_INSTALL_FAILURE);

                    installer.DoInstall();
                    interlock.Dispose();

                    ProcessLaunchArgs();
                }
                else
                {
#if QA_BUILD
                    Frontend.ShowMessageBox(
                        "You are about to install a QA build of Hercules. The red window border indicates that this is a QA build.\n\n" +
                        "QA builds are handled completely separately of your standard installation, like a virtual environment.",
                        MessageBoxImage.Information);
#endif

                    new LanguageSelectorDialog().ShowDialog();

                    var installer = new UI.Elements.Installer.MainWindow();
                    installer.ShowDialog();
                    interlock.Dispose();

                    ProcessNextAction(installer.CloseAction, !installer.Finished);
                }
            }
            finally
            {
                interlock.Dispose();
            }
        }

        public static void LaunchUninstaller()
        {
            using var interlock = new InterProcessLock("Uninstaller");

            if (!interlock.IsAcquired)
            {
                Frontend.ShowMessageBox(Strings.Dialog_AlreadyRunning_Uninstaller, MessageBoxImage.Stop);
                App.Terminate();
                return;
            }

            bool confirmed;
            bool keepData = true;

            if (App.LaunchSettings.QuietFlag.Active)
            {
                confirmed = true;
            }
            else
            {
                var dialog = new UninstallerDialog();
                dialog.ShowDialog();

                confirmed = dialog.Confirmed;
                keepData = dialog.KeepData;
            }

            if (!confirmed)
            {
                App.Terminate();
                return;
            }

            Installer.DoUninstall(keepData);

            Frontend.ShowMessageBox(Strings.Bootstrapper_SuccessfullyUninstalled, MessageBoxImage.Information);
            App.Terminate();
        }

        public static void LaunchSettings()
        {
            const string LOG_IDENT = "LaunchHandler::LaunchSettings";

            using var interlock = new InterProcessLock("Settings");

            if (interlock.IsAcquired)
            {
                bool showAlreadyRunningWarning = Process.GetProcessesByName(App.ProjectName).Length > 1;

                var window = new UI.Elements.Settings.MainWindow(showAlreadyRunningWarning);
                window.ShowDialog();
            }
            else
            {
                App.Logger.WriteLine(LOG_IDENT, "Found an already existing menu window");

                var process = Utilities.GetProcessesSafe()
                    .FirstOrDefault(x => x.MainWindowTitle == Strings.Menu_Title);

                if (process is not null && process.MainWindowHandle != IntPtr.Zero)
                {
                    PInvoke.SetForegroundWindow(new HWND(process.MainWindowHandle));
                }

                App.Terminate();
            }
        }

        public static void LaunchMenu()
        {
            var dialog = new LaunchMenuDialog();
            dialog.ShowDialog();

            ProcessNextAction(dialog.CloseAction);
        }

        public static void LaunchRoblox(LaunchMode launchMode)
        {
            const string LOG_IDENT = "LaunchHandler::LaunchRoblox";
            const string GlobalMutexName = @"Global\ROBLOX_singletonMutex";
            const string LocalMutexName = "ROBLOX_singletonMutex"; // fallback idk, was cuz someone had a issue with this so added a fallback

            if (launchMode == LaunchMode.None)
                throw new InvalidOperationException("No Roblox launch mode set");

            if (!File.Exists(Path.Combine(Paths.System, "mfplat.dll")))
            {
                Frontend.ShowMessageBox(Strings.Bootstrapper_WMFNotFound, MessageBoxImage.Error);

                if (!App.LaunchSettings.QuietFlag.Active)
                {
                    Utilities.ShellExecute(
                        "https://support.microsoft.com/en-us/topic/media-feature-pack-list-for-windows-n-editions-c1c6fffa-d052-8338-7a79-a4bb980a700a");
                }

                App.Terminate(ErrorCode.ERROR_FILE_NOT_FOUND);
                return;
            }

            bool robloxRunning = false;
            try
            {
                robloxRunning = Mutex.TryOpenExisting(GlobalMutexName, out _);
            }
            catch (UnauthorizedAccessException)
            {
                robloxRunning = false;
            }
            catch
            {
                robloxRunning = false;
            }

            if (!robloxRunning)
            {
                try
                {
                    robloxRunning = Mutex.TryOpenExisting(LocalMutexName, out _);
                }
                catch
                {
                    robloxRunning = false;
                }
            }

            if (App.Settings.Prop.ConfirmLaunches
                && robloxRunning
                && !(App.Settings.Prop.IsGameEnabled && !string.IsNullOrWhiteSpace(App.Settings.Prop.LaunchGameID)))
            {
                var result = Frontend.ShowMessageBox(
                    Strings.Bootstrapper_ConfirmLaunch,
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                {
                    App.Terminate();
                    return;
                }
            }

            App.Logger.WriteLine(LOG_IDENT, "Initializing bootstrapper");
            App.Bootstrapper = new Bootstrapper(launchMode);

            IBootstrapperDialog? dialog = null;
            if (!App.LaunchSettings.QuietFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Initializing bootstrapper dialog");
                dialog = App.Settings.Prop.BootstrapperStyle.GetNew();
                App.Bootstrapper.Dialog = dialog;
                dialog.Bootstrapper = App.Bootstrapper;
            }

            Mutex? mutex = null;

            if (App.Settings.Prop.ExclusiveFullscreen)
            {
                _ = Task.Run(RobloxFullscreen.WaitAndTriggerFullscreen);
            }

            Task.Run(App.Bootstrapper.Run).ContinueWith(t =>
            {
                App.Logger.WriteLine(LOG_IDENT, "Bootstrapper task has finished");

                try
                {
                    if (t.IsFaulted)
                    {
                        App.Logger.WriteLine(LOG_IDENT, "An exception occurred when running the bootstrapper");

                        if (t.Exception is not null)
                            App.FinalizeExceptionHandling(t.Exception);
                    }

                    if (mutex != null)
                    {
                        string processName = App.RobloxPlayerAppName.Split('.')[0];
                        App.Logger.WriteLine(LOG_IDENT, $"Resolved Roblox name {processName}.exe, running in background.");

                        while (Process.GetProcessesByName(processName).Any())
                            Thread.Sleep(5000);

                        App.Logger.WriteLine(LOG_IDENT, "Every Roblox instance is closed, terminating the process");
                    }
                }
                finally
                {
                    if (mutex != null)
                    {
                        try { mutex.ReleaseMutex(); } catch { }
                        mutex.Dispose();
                    }

                    App.Terminate();
                }
            });

            dialog?.ShowBootstrapper();
            App.Logger.WriteLine(LOG_IDENT, "Exiting");
        }

        public static void LaunchWatcher()
        {
            const string LOG_IDENT = "LaunchHandler::LaunchWatcher";

            var watcher = new Watcher();

            Task.Run(watcher.Run).ContinueWith(t =>
            {
                App.Logger.WriteLine(LOG_IDENT, "Watcher task has finished");

                watcher.Dispose();

                if (t.IsFaulted)
                {
                    App.Logger.WriteLine(LOG_IDENT, "An exception occurred when running the watcher");

                    if (t.Exception is not null)
                        App.FinalizeExceptionHandling(t.Exception);
                }

                if (App.Settings.Prop.CleanerOptions != CleanerOptions.Never)
                    Cleaner.DoCleaning();

                App.Terminate();
            });
        }

        public static void LaunchBloxshadeConfig()
        {
            const string LOG_IDENT = "LaunchHandler::LaunchBloxshade";

            App.Logger.WriteLine(LOG_IDENT, "Showing unsupported warning");

            new BloxshadeDialog().ShowDialog();
            App.SoftTerminate();
        }

        public static async void ProcessProtocol(string url)
        {
            const string LOG_IDENT = "LaunchHandler::ProcessProtocol";

            if (!url.StartsWith("hercules://", StringComparison.OrdinalIgnoreCase))
            {
                App.Logger.WriteLine(LOG_IDENT, $"Unknown protocol: {url}");
                return;
            }

            var path = url["hercules://".Length..].TrimEnd('/');
            var parts = path.Split('?');
            var action = parts[0].ToLowerInvariant();
            var query = parts.Length > 1 ? System.Web.HttpUtility.ParseQueryString(parts[1]) : null;

            App.Logger.WriteLine(LOG_IDENT, $"Protocol action: {action}");

            try
            {
                switch (action)
                {
                    case "preset" or "apply-preset":
                        var presetName = query?["name"] ?? parts.ElementAtOrDefault(1) ?? "";
                        ApplyPreset(presetName);
                        break;

                    case "settings" or "menu":
                        LaunchSettings();
                        break;

                    case "open-settings-page":
                        var page = query?["page"] ?? "";
                        LaunchSettings(); // opens settings; user navigates
                        break;

                    case "import":
                        var importUrl = query?["url"] ?? "";
                        if (!string.IsNullOrEmpty(importUrl))
                            await ImportFromUrl(importUrl);
                        break;

                    case "download":
                        App.Logger.WriteLine(LOG_IDENT, "Opening download page");
                        Utilities.OpenWebsite(App.ProjectDownloadLink);
                        App.SoftTerminate();
                        break;

                    case "donate":
                        App.Logger.WriteLine(LOG_IDENT, "Opening donate page");
                        Utilities.OpenWebsite("https://ko-fi.com/hercules");
                        App.SoftTerminate();
                        break;

                    default:
                        App.Logger.WriteLine(LOG_IDENT, $"Unknown protocol action: {action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private static void ApplyPreset(string presetName)
        {
            const string LOG_IDENT = "LaunchHandler::ApplyPreset";

            var theme = Integrations.FlagPresetThemesProvider.GetAllThemes()
                .FirstOrDefault(t =>
                    t.Name.Equals(presetName, StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Replace(" ", "-").Equals(presetName, StringComparison.OrdinalIgnoreCase));

            if (theme == null)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Preset not found: {presetName}");
                return;
            }

            App.Logger.WriteLine(LOG_IDENT, $"Applying preset: {theme.Name}");
            FlagPresetThemesProvider.ApplyTheme(theme);
            App.SoftTerminate();
        }

        private static async Task ImportFromUrl(string url)
        {
            const string LOG_IDENT = "LaunchHandler::ImportFromUrl";

            try
            {
                using var http = new HttpClient();
                var response = await http.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var tempFile = Path.Combine(Path.GetTempPath(), $"hercules-import-{Guid.NewGuid()}.hercules");
                await using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    await response.Content.CopyToAsync(fs);

                var config = ConfigExporter.ImportFromFile(tempFile);
                if (config != null)
                {
                    ConfigExporter.ApplyConfig(config);
                    App.Logger.WriteLine(LOG_IDENT, "Config imported successfully");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
    }
}
