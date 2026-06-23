using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hercules.Integrations
{
    public static class DiagnosticReportService
    {
        private const int MaximumLogLines = 200;
        private static readonly Regex SensitiveValueRegex = new(
            @"(?i)(roblosecurity|cookie|token|authorization|bearer|oauth|refresh_token|access_token|password|secret)([""'\s:=]+)([^""'\s,;]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static DiagnosticReport CreateReport()
        {
            string processPath = Environment.ProcessPath ?? Paths.Process;

            return new DiagnosticReport
            {
                GeneratedAtUtc = DateTime.UtcNow,
                App = new AppDiagnostics
                {
                    Name = App.ProjectName,
                    Version = App.Version,
                    Repository = App.ProjectRepository,
                    Website = App.ProjectWebsite,
                    BuildTime = App.BuildMetadata?.Timestamp,
                    BuildCommit = App.BuildMetadata?.CommitHash,
                    BuildRef = App.BuildMetadata?.CommitRef,
                    ExecutableSha256 = TryHashFile(processPath),
                    IsActionBuild = App.IsActionBuild,
                    IsProductionBuild = App.IsProductionBuild
                },
                System = new SystemDiagnostics
                {
                    OSVersion = Environment.OSVersion.VersionString,
                    Is64BitOS = Environment.Is64BitOperatingSystem,
                    ProcessorCount = Environment.ProcessorCount,
                    MachineNameHash = HashText(Environment.MachineName),
                    UserNameHash = HashText(Environment.UserName)
                },
                Paths = BuildPathDiagnostics(),
                Settings = BuildSettingsDiagnostics(),
                Integrity = RunIntegrityChecks(),
                RecentLog = GetSanitizedRecentLog()
            };
        }

        public static void WriteReport(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            string json = JsonSerializer.Serialize(CreateReport(), new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(path, json);
        }

        private static List<PathDiagnostics> BuildPathDiagnostics()
        {
            return new List<PathDiagnostics>
            {
                PathStatus("Base", Paths.Base, isDirectory: true),
                PathStatus("Downloads", Paths.Downloads, isDirectory: true),
                PathStatus("Logs", Paths.Logs, isDirectory: true),
                PathStatus("Integrations", Paths.Integrations, isDirectory: true),
                PathStatus("Versions", Paths.Versions, isDirectory: true),
                PathStatus("Mods", Paths.Mods, isDirectory: true),
                PathStatus("CustomThemes", Paths.CustomThemes, isDirectory: true),
                PathStatus("SavedBackups", Paths.SavedBackups, isDirectory: true),
                PathStatus("Application", Paths.Application, isDirectory: false),
                PathStatus("Settings", App.Settings.FileLocation, isDirectory: false),
                PathStatus("State", App.State.FileLocation, isDirectory: false),
                PathStatus("FastFlags", App.FastFlags.FileLocation, isDirectory: false)
            };
        }

        private static PathDiagnostics PathStatus(string name, string path, bool isDirectory)
        {
            bool exists = isDirectory ? Directory.Exists(path) : File.Exists(path);
            long? size = null;
            DateTime? lastWrite = null;

            if (!isDirectory && exists)
            {
                var info = new FileInfo(path);
                size = info.Length;
                lastWrite = info.LastWriteTimeUtc;
            }

            return new PathDiagnostics
            {
                Name = name,
                Exists = exists,
                IsDirectory = isDirectory,
                SizeBytes = size,
                LastWriteUtc = lastWrite
            };
        }

        private static SettingsDiagnostics BuildSettingsDiagnostics()
        {
            return new SettingsDiagnostics
            {
                Locale = App.Settings.Prop.Locale,
                Channel = App.Settings.Prop.Channel,
                Theme = App.Settings.Prop.Theme2.ToString(),
                FastFlagCount = App.FastFlags.Prop.Count,
                CustomIntegrationCount = App.Settings.Prop.CustomIntegrations.Count,
                ModFileCount = CountFilesSafe(Paths.Mods),
                CustomThemeFileCount = CountFilesSafe(Paths.CustomThemes),
                SavedBackupCount = CountFilesSafe(Paths.SavedBackups),
                SwiftTunnelEnabled = App.Settings.Prop.SwiftTunnelEnabled,
                OverlaysEnabled = App.Settings.Prop.OverlaysEnabled,
                MotionBlurOverlay = App.Settings.Prop.MotionBlurOverlay,
                DiscordRichPresence = App.Settings.Prop.UseDiscordRichPresence,
                MultiAccount = App.Settings.Prop.MultiAccount,
                WpfSoftwareRender = App.Settings.Prop.WPFSoftwareRender
            };
        }

        private static List<IntegrityCheck> RunIntegrityChecks()
        {
            var checks = new List<IntegrityCheck>();

            AddCheck(checks, "Application executable exists", File.Exists(Paths.Application), "Expected installed Hercules executable was not found.");
            AddCheck(checks, "Settings file exists", File.Exists(App.Settings.FileLocation), "Settings will be recreated with defaults if missing.");
            AddCheck(checks, "State file exists", File.Exists(App.State.FileLocation), "State will be recreated with defaults if missing.");
            AddCheck(checks, "FastFlags file readable", IsJsonReadable(App.FastFlags.FileLocation), "FastFlags JSON is missing or invalid.");
            AddCheck(checks, "Settings file readable", IsJsonReadable(App.Settings.FileLocation), "Settings JSON is missing or invalid.");
            AddCheck(checks, "Logs directory exists", Directory.Exists(Paths.Logs), "Logs directory is missing.");
            AddCheck(checks, "Mods directory exists", Directory.Exists(Paths.Mods), "Mods directory is missing.");
            AddCheck(checks, "No oversized custom themes", !HasOversizedFiles(Paths.CustomThemes, 4L * 1024 * 1024), "One or more custom theme files exceed 4 MB.");
            AddCheck(checks, "No oversized cache files", !HasOversizedFiles(Paths.Cache, 8L * 1024 * 1024), "One or more cache files exceed the diagnostic threshold.");

            return checks;
        }

        private static void AddCheck(List<IntegrityCheck> checks, string name, bool passed, string remediation)
        {
            checks.Add(new IntegrityCheck
            {
                Name = name,
                Passed = passed,
                Severity = passed ? "ok" : "warning",
                Remediation = passed ? "" : remediation
            });
        }

        private static bool IsJsonReadable(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                using var stream = File.OpenRead(path);
                using var _ = JsonDocument.Parse(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasOversizedFiles(string directory, long maxBytes)
        {
            try
            {
                if (!Directory.Exists(directory))
                    return false;

                return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                    .Any(path => new FileInfo(path).Length > maxBytes);
            }
            catch
            {
                return false;
            }
        }

        private static int CountFilesSafe(string directory)
        {
            try
            {
                return Directory.Exists(directory)
                    ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Count()
                    : 0;
            }
            catch
            {
                return -1;
            }
        }

        private static List<string> GetSanitizedRecentLog()
        {
            try
            {
                return App.Logger.AsDocument
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .TakeLast(MaximumLogLines)
                    .Select(Sanitize)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static string Sanitize(string value)
        {
            return SensitiveValueRegex.Replace(value, "$1$2[redacted]");
        }

        private static string? TryHashFile(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return null;

                using var stream = File.OpenRead(path);
                return Convert.ToHexString(SHA256.HashData(stream));
            }
            catch
            {
                return null;
            }
        }

        private static string HashText(string value)
        {
            return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
        }
    }

    public sealed class DiagnosticReport
    {
        public DateTime GeneratedAtUtc { get; set; }
        public AppDiagnostics App { get; set; } = new();
        public SystemDiagnostics System { get; set; } = new();
        public List<PathDiagnostics> Paths { get; set; } = new();
        public SettingsDiagnostics Settings { get; set; } = new();
        public List<IntegrityCheck> Integrity { get; set; } = new();
        public List<string> RecentLog { get; set; } = new();
    }

    public sealed class AppDiagnostics
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Repository { get; set; } = "";
        public string Website { get; set; } = "";
        public DateTime? BuildTime { get; set; }
        public string? BuildCommit { get; set; }
        public string? BuildRef { get; set; }
        public string? ExecutableSha256 { get; set; }
        public bool IsActionBuild { get; set; }
        public bool IsProductionBuild { get; set; }
    }

    public sealed class SystemDiagnostics
    {
        public string OSVersion { get; set; } = "";
        public bool Is64BitOS { get; set; }
        public int ProcessorCount { get; set; }
        public string MachineNameHash { get; set; } = "";
        public string UserNameHash { get; set; } = "";
    }

    public sealed class PathDiagnostics
    {
        public string Name { get; set; } = "";
        public bool Exists { get; set; }
        public bool IsDirectory { get; set; }
        public long? SizeBytes { get; set; }
        public DateTime? LastWriteUtc { get; set; }
    }

    public sealed class SettingsDiagnostics
    {
        public string Locale { get; set; } = "";
        public string Channel { get; set; } = "";
        public string Theme { get; set; } = "";
        public int FastFlagCount { get; set; }
        public int CustomIntegrationCount { get; set; }
        public int ModFileCount { get; set; }
        public int CustomThemeFileCount { get; set; }
        public int SavedBackupCount { get; set; }
        public bool SwiftTunnelEnabled { get; set; }
        public bool OverlaysEnabled { get; set; }
        public bool MotionBlurOverlay { get; set; }
        public bool DiscordRichPresence { get; set; }
        public bool MultiAccount { get; set; }
        public bool WpfSoftwareRender { get; set; }
    }

    public sealed class IntegrityCheck
    {
        public string Name { get; set; } = "";
        public bool Passed { get; set; }
        public string Severity { get; set; } = "ok";
        public string Remediation { get; set; } = "";
    }
}
