namespace Hercules.Integrations;

public class HerculesConfig
{
    public DateTime ExportedAt { get; set; }
    public string Version { get; set; } = "";
    public Dictionary<string, string> FastFlags { get; set; } = new();
    public string ClientSettings { get; set; } = "";
    public string BootstrapperSettings { get; set; } = "";
    public string StateSettings { get; set; } = "";
    public string RobloxStateSettings { get; set; } = "";
    public string ThemeSettings { get; set; } = "";
    public string GlobalBasicSettings { get; set; } = "";
}

public static class ConfigExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string FastFlagsFile => Path.Combine(Paths.Mods, "ClientSettings", "ClientAppSettings.json");
    private static string BootstrapperFile => Path.Combine(Paths.Base, "AppSettings.json");
    private static string StateFile => Path.Combine(Paths.Base, "State.json");
    private static string RobloxStateFile => Path.Combine(Paths.Base, "RobloxState.json");
    private static string ThemesFile => Path.Combine(Paths.Base, "flag_preset_themes.json");
    private static string GlobalBasicFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Roblox",
        "GlobalBasicSettings_13.xml"
    );

    public static string ExportToString()
    {
        var fastFlags = new Dictionary<string, string>();
        foreach (var kvp in App.FastFlags.Prop)
            fastFlags[kvp.Key] = kvp.Value?.ToString() ?? "";

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

    public static void ExportToFile(string filePath)
    {
        var json = ExportToString();
        File.WriteAllText(filePath, json);
    }

    public static HerculesConfig? ImportFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<HerculesConfig>(json);
    }

    public static void ApplyConfig(HerculesConfig config)
    {
        if (config.FastFlags.Count > 0)
        {
            App.FastFlags.Prop.Clear();
            foreach (var kvp in config.FastFlags)
                App.FastFlags.Prop[kvp.Key] = kvp.Value;
            App.FastFlags.Save();
        }

        if (!string.IsNullOrEmpty(config.ClientSettings))
            WriteFileSafe(FastFlagsFile, config.ClientSettings);

        if (!string.IsNullOrEmpty(config.BootstrapperSettings))
        {
            WriteFileSafe(BootstrapperFile, config.BootstrapperSettings);
            App.Settings.Load();
        }

        if (!string.IsNullOrEmpty(config.StateSettings))
        {
            WriteFileSafe(StateFile, config.StateSettings);
            App.State.Load();
        }

        if (!string.IsNullOrEmpty(config.RobloxStateSettings))
        {
            WriteFileSafe(RobloxStateFile, config.RobloxStateSettings);
            App.RobloxState.Load();
        }

        if (!string.IsNullOrEmpty(config.ThemeSettings))
            WriteFileSafe(ThemesFile, config.ThemeSettings);

        if (!string.IsNullOrEmpty(config.GlobalBasicSettings))
        {
            WriteFileSafe(GlobalBasicFile, config.GlobalBasicSettings);
            App.GlobalSettings.Load();
        }
    }

    public static int CountExportedItems(HerculesConfig config)
    {
        int count = 0;
        if (config.FastFlags.Count > 0) count++;
        if (!string.IsNullOrEmpty(config.ClientSettings)) count++;
        if (!string.IsNullOrEmpty(config.BootstrapperSettings)) count++;
        if (!string.IsNullOrEmpty(config.StateSettings)) count++;
        if (!string.IsNullOrEmpty(config.RobloxStateSettings)) count++;
        if (!string.IsNullOrEmpty(config.ThemeSettings)) count++;
        if (!string.IsNullOrEmpty(config.GlobalBasicSettings)) count++;
        return count;
    }

    private static string ReadFileSafe(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : ""; }
        catch { return ""; }
    }

    private static void WriteFileSafe(string path, string content)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }
        catch { }
    }
}
