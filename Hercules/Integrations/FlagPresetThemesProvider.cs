using System.Text.Json;
using Hercules.Models;

namespace Hercules.Integrations;

public static class FlagPresetThemesProvider
{
    private static readonly string ThemesFilePath = Path.Combine(Paths.Base, "flag_preset_themes.json");
    private static List<FlagPresetTheme>? _cachedThemes;

    public static List<FlagPresetTheme> GetBuiltInThemes() => new()
    {
        new FlagPresetTheme
        {
            Name = "Competitive",
            Description = "Otimizado para performance máxima em competitivo",
            Category = "Performance",
            Icon = "⚡",
            Tags = new() { "fps", "competitivo", "baixa latência" },
            IsBuiltIn = true,
            Flags = new()
            {
                ["FFlagDebugGraphicsDisableGLSL"] = "True",
                ["FFlagDebugGraphicsPreferVulkan"] = "True",
                ["FFlagDisablePostFx"] = "True",
                ["FFlagDisableSky"] = "True",
                ["DFIntTaskSchedulerTargetFps"] = "9999",
                ["FIntRobloxGuiBlurIntensity"] = "0"
            }
        },
        new FlagPresetTheme
        {
            Name = "Max Graphics",
            Description = "Máxima qualidade visual",
            Category = "Visual",
            Icon = "🎨",
            Tags = new() { "gráficos", "visual", "qualidade" },
            IsBuiltIn = true,
            Flags = new()
            {
                ["FFlagDebugGraphicsPreferVulkan"] = "False",
                ["FFlagDisablePostFx"] = "False",
                ["FFlagDisableSky"] = "False",
                ["FFlagDisableShadows"] = "False"
            }
        },
        new FlagPresetTheme
        {
            Name = "Low-End PC",
            Description = "Para computadores mais fracos",
            Category = "Performance",
            Icon = "💻",
            Tags = new() { "low-end", "fraco", "performance" },
            IsBuiltIn = true,
            Flags = new()
            {
                ["FFlagDebugGraphicsDisableGLSL"] = "True",
                ["FFlagDebugGraphicsPreferVulkan"] = "True",
                ["FFlagDisablePostFx"] = "True",
                ["FFlagDisableSky"] = "True",
                ["FFlagDisableShadows"] = "True",
                ["FIntRobloxGuiBlurIntensity"] = "0",
                ["FFlagFixGraphicsQuality"] = "True",
                ["DFIntTaskSchedulerTargetFps"] = "60"
            }
        },
        new FlagPresetTheme
        {
            Name = "Privacy",
            Description = "Desabilita telemetria e rastreamento",
            Category = "Privacy",
            Icon = "🔒",
            Tags = new() { "privacidade", "telemetria", "tracking" },
            IsBuiltIn = true,
            Flags = new()
            {
                ["FFlagDebugDisableTelemetry"] = "True",
                ["FFlagDebugDisableAnalytics"] = "True",
                ["FFlagDebugDisableCrashReporting"] = "True",
                ["FFlagDisableDataPersistence"] = "True"
            }
        },
        new FlagPresetTheme
        {
            Name = "Network Boost",
            Description = "Otimizações de rede para menor ping",
            Category = "Network",
            Icon = "🌐",
            Tags = new() { "rede", "ping", "latência" },
            IsBuiltIn = true,
            Flags = new()
            {
                ["FFlagDebugForceInternalNetworking"] = "True",
                ["FIntWebSocketMaxSocketCount"] = "64",
                ["FIntHttpMaxConnectionsPerServer"] = "32"
            }
        }
    };

    public static List<FlagPresetTheme> GetAllThemes()
    {
        if (_cachedThemes != null) return _cachedThemes;

        var themes = GetBuiltInThemes();

        try
        {
            if (File.Exists(ThemesFilePath))
            {
                var json = File.ReadAllText(ThemesFilePath);
                var custom = JsonSerializer.Deserialize<List<FlagPresetTheme>>(json);
                if (custom != null)
                    themes.AddRange(custom);
            }
        }
        catch { }

        _cachedThemes = themes;
        return themes;
    }

    public static void SaveCustomThemes(List<FlagPresetTheme> customThemes)
    {
        try
        {
            var dir = Path.GetDirectoryName(ThemesFilePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(customThemes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ThemesFilePath, json);
            _cachedThemes = null;
        }
        catch { }
    }

    public static void ApplyTheme(FlagPresetTheme theme)
    {
        foreach (var kvp in theme.Flags)
        {
            App.FastFlags.Prop[kvp.Key] = kvp.Value;
        }
        App.FastFlags.Save();
    }
}
