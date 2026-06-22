using System.Text.Json;
using Hercules.Models;

namespace Hercules.Integrations;

public static class FlagPresetThemesProvider
{
    private const long MaximumThemeFileBytes = 4L * 1024 * 1024;
    private const int MaximumCustomThemes = 500;
    private const int MaximumFlagsPerTheme = 5_000;
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
                if (new FileInfo(ThemesFilePath).Length > MaximumThemeFileBytes)
                    throw new InvalidDataException("Custom theme file exceeds the 4 MB limit.");

                var json = File.ReadAllText(ThemesFilePath);
                var custom = JsonSerializer.Deserialize<List<FlagPresetTheme>>(json);
                if (custom != null)
                {
                    ValidateCustomThemes(custom);
                    themes.AddRange(custom);
                }
            }
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("FlagPresetThemesProvider::GetAllThemes", ex);
        }

        _cachedThemes = themes;
        return themes;
    }

    public static void SaveCustomThemes(List<FlagPresetTheme> customThemes)
    {
        ValidateCustomThemes(customThemes);

        try
        {
            var dir = Path.GetDirectoryName(ThemesFilePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(customThemes, new JsonSerializerOptions { WriteIndented = true });
            string temporaryPath = ThemesFilePath + $".{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, ThemesFilePath, overwrite: true);
            }
            finally
            {
                File.Delete(temporaryPath);
            }
            _cachedThemes = null;
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("FlagPresetThemesProvider::SaveCustomThemes", ex);
            throw;
        }
    }

    public static void InvalidateCache() => _cachedThemes = null;

    public static void ApplyTheme(FlagPresetTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ValidateTheme(theme);

        foreach (var kvp in theme.Flags)
        {
            App.FastFlags.Prop[kvp.Key] = kvp.Value;
        }
        App.FastFlags.Save();
    }

    private static void ValidateCustomThemes(List<FlagPresetTheme> themes)
    {
        ArgumentNullException.ThrowIfNull(themes);
        if (themes.Count > MaximumCustomThemes)
            throw new InvalidDataException($"A maximum of {MaximumCustomThemes} custom themes is allowed.");

        foreach (FlagPresetTheme theme in themes)
            ValidateTheme(theme);
    }

    private static void ValidateTheme(FlagPresetTheme theme)
    {
        if (string.IsNullOrWhiteSpace(theme.Name))
            throw new InvalidDataException("Theme name cannot be empty.");
        if (theme.Flags is null || theme.Flags.Count > MaximumFlagsPerTheme)
            throw new InvalidDataException($"Theme '{theme.Name}' contains too many flags.");
        if (theme.Flags.Keys.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException($"Theme '{theme.Name}' contains an empty flag name.");
    }
}
