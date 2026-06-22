using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Hercules.Models;
using Hercules.Models.Persistable;

namespace Hercules.Integrations;

public sealed class HerculesConfig
{
    public int FormatVersion { get; set; } = ConfigExporter.CurrentFormatVersion;
    public DateTime ExportedAt { get; set; }
    public string Version { get; set; } = "";
    public Dictionary<string, JsonElement> FastFlags { get; set; } = new();
    public string ClientSettings { get; set; } = "";
    public string BootstrapperSettings { get; set; } = "";
    public string StateSettings { get; set; } = "";
    public string RobloxStateSettings { get; set; } = "";
    public string ThemeSettings { get; set; } = "";
    public string GlobalBasicSettings { get; set; } = "";
}

public static class ConfigExporter
{
    public const int CurrentFormatVersion = 1;
    private const long MaximumImportBytes = 16L * 1024 * 1024;
    private const int MaximumSectionCharacters = 8 * 1024 * 1024;
    private const int MaximumFastFlags = 50_000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string FastFlagsFile => Path.Combine(Paths.Mods, "ClientSettings", "ClientAppSettings.json");
    private static string BootstrapperFile => Path.Combine(Paths.Base, "AppSettings.json");
    private static string StateFile => Path.Combine(Paths.Base, "State.json");
    private static string RobloxStateFile => Path.Combine(Paths.Base, "RobloxState.json");
    private static string ThemesFile => Path.Combine(Paths.Base, "flag_preset_themes.json");
    private static string GlobalBasicFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Roblox",
        "GlobalBasicSettings_13.xml");

    public static string ExportToString()
    {
        var fastFlags = App.FastFlags.Prop.ToDictionary(
            pair => pair.Key,
            pair => JsonSerializer.SerializeToElement(pair.Value, JsonOptions));

        var config = new HerculesConfig
        {
            ExportedAt = DateTime.UtcNow,
            Version = App.Version,
            FastFlags = fastFlags,
            ClientSettings = ReadFileSafe(FastFlagsFile),
            BootstrapperSettings = ReadFileSafe(BootstrapperFile),
            StateSettings = ReadFileSafe(StateFile),
            RobloxStateSettings = ReadFileSafe(RobloxStateFile),
            ThemeSettings = ReadFileSafe(ThemesFile),
            GlobalBasicSettings = ReadFileSafe(GlobalBasicFile)
        };

        return JsonSerializer.Serialize(config, JsonOptions);
    }

    public static void ExportToFile(string filePath) =>
        WriteAtomically(filePath, ExportToString());

    public static HerculesConfig ImportFromFile(string filePath)
    {
        var file = new FileInfo(filePath);
        if (!file.Exists)
            throw new FileNotFoundException("Configuration file was not found.", filePath);
        if (file.Length > MaximumImportBytes)
            throw new InvalidDataException($"Configuration exceeds the {MaximumImportBytes / 1024 / 1024} MB limit.");

        string json = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<HerculesConfig>(json, JsonOptions)
            ?? throw new InvalidDataException("Configuration is empty or invalid.");
        Validate(config);
        return config;
    }

    public static void ApplyConfig(HerculesConfig config)
    {
        Validate(config);

        var changes = BuildChanges(config);
        var originals = changes.Keys.ToDictionary(
            path => path,
            path => File.Exists(path) ? File.ReadAllBytes(path) : null);

        try
        {
            foreach (var change in changes)
                WriteAtomically(change.Key, change.Value);

            ReloadChangedManagers(changes.Keys);
        }
        catch
        {
            foreach (var original in originals)
            {
                if (original.Value is null)
                    File.Delete(original.Key);
                else
                    WriteBytesAtomically(original.Key, original.Value);
            }

            ReloadChangedManagers(originals.Keys);
            throw;
        }
    }

    public static int CountExportedItems(HerculesConfig config)
    {
        int count = 0;
        if (config.FastFlags?.Count > 0 || !string.IsNullOrEmpty(config.ClientSettings)) count++;
        if (!string.IsNullOrEmpty(config.BootstrapperSettings)) count++;
        if (!string.IsNullOrEmpty(config.StateSettings)) count++;
        if (!string.IsNullOrEmpty(config.RobloxStateSettings)) count++;
        if (!string.IsNullOrEmpty(config.ThemeSettings)) count++;
        if (!string.IsNullOrEmpty(config.GlobalBasicSettings)) count++;
        return count;
    }

    private static Dictionary<string, string> BuildChanges(HerculesConfig config)
    {
        var changes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(config.ClientSettings))
        {
            changes[FastFlagsFile] = config.ClientSettings;
        }
        else if (config.FastFlags.Count > 0)
        {
            var values = config.FastFlags.ToDictionary(
                pair => pair.Key,
                pair => ConvertJsonValue(pair.Value));
            changes[FastFlagsFile] = JsonSerializer.Serialize(values, JsonOptions);
        }

        AddIfPresent(changes, BootstrapperFile, config.BootstrapperSettings);
        AddIfPresent(changes, StateFile, config.StateSettings);
        AddIfPresent(changes, RobloxStateFile, config.RobloxStateSettings);
        AddIfPresent(changes, ThemesFile, config.ThemeSettings);
        AddIfPresent(changes, GlobalBasicFile, config.GlobalBasicSettings);
        return changes;
    }

    private static void AddIfPresent(Dictionary<string, string> changes, string path, string content)
    {
        if (!string.IsNullOrEmpty(content))
            changes[path] = content;
    }

    private static object? ConvertJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out long integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Null => null,
        _ => value.Clone()
    };

    private static void Validate(HerculesConfig config)
    {
        if (config.FormatVersion is < 1 or > CurrentFormatVersion)
            throw new InvalidDataException($"Unsupported Hercules configuration format: {config.FormatVersion}.");
        if (config.FastFlags is null)
            throw new InvalidDataException("FastFlags cannot be null.");
        if (config.FastFlags.Count > MaximumFastFlags)
            throw new InvalidDataException($"Configuration contains more than {MaximumFastFlags} FastFlags.");
        if (config.FastFlags.Keys.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("FastFlag names cannot be empty.");

        ValidateSectionLength(config.ClientSettings, nameof(config.ClientSettings));
        ValidateSectionLength(config.BootstrapperSettings, nameof(config.BootstrapperSettings));
        ValidateSectionLength(config.StateSettings, nameof(config.StateSettings));
        ValidateSectionLength(config.RobloxStateSettings, nameof(config.RobloxStateSettings));
        ValidateSectionLength(config.ThemeSettings, nameof(config.ThemeSettings));
        ValidateSectionLength(config.GlobalBasicSettings, nameof(config.GlobalBasicSettings));

        ValidateJson<Dictionary<string, JsonElement>>(config.ClientSettings, nameof(config.ClientSettings));
        ValidateJson<AppSettings>(config.BootstrapperSettings, nameof(config.BootstrapperSettings));
        ValidateJson<State>(config.StateSettings, nameof(config.StateSettings));
        ValidateJson<RobloxState>(config.RobloxStateSettings, nameof(config.RobloxStateSettings));
        ValidateJson<List<FlagPresetTheme>>(config.ThemeSettings, nameof(config.ThemeSettings));
        ValidateXml(config.GlobalBasicSettings, nameof(config.GlobalBasicSettings));
    }

    private static void ValidateSectionLength(string content, string name)
    {
        if (content?.Length > MaximumSectionCharacters)
            throw new InvalidDataException($"{name} exceeds the allowed size.");
    }

    private static void ValidateJson<T>(string content, string name)
    {
        if (string.IsNullOrEmpty(content))
            return;

        try
        {
            if (JsonSerializer.Deserialize<T>(content, JsonOptions) is null)
                throw new JsonException("Deserialization returned null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{name} contains invalid JSON.", ex);
        }
    }

    private static void ValidateXml(string content, string name)
    {
        if (string.IsNullOrEmpty(content))
            return;

        try
        {
            using var textReader = new StringReader(content);
            using var xmlReader = XmlReader.Create(textReader, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });
            _ = XDocument.Load(xmlReader, LoadOptions.None);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException($"{name} contains invalid XML.", ex);
        }
    }

    private static void ReloadChangedManagers(IEnumerable<string> paths)
    {
        var changed = paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (changed.Contains(FastFlagsFile)) App.FastFlags.Load(alertFailure: false);
        if (changed.Contains(BootstrapperFile)) App.Settings.Load(alertFailure: false);
        if (changed.Contains(StateFile)) App.State.Load(alertFailure: false);
        if (changed.Contains(RobloxStateFile)) App.RobloxState.Load(alertFailure: false);
        if (changed.Contains(ThemesFile)) FlagPresetThemesProvider.InvalidateCache();
        if (changed.Contains(GlobalBasicFile)) App.GlobalSettings.Load();
    }

    private static string ReadFileSafe(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : ""; }
        catch (Exception ex)
        {
            App.Logger.WriteException("ConfigExporter::ReadFileSafe", ex);
            return "";
        }
    }

    private static void WriteAtomically(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static void WriteBytesAtomically(string path, byte[] content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(temporaryPath, content);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
