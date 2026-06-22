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
            Description = "Máximo FPS, mínimo lag. Desliga sombras, grama, vento, SSAO, pós-processamento e força qualidade gráfica mínima.",
            Category = "Performance",
            Icon = "⚡",
            Tags = new() { "fps", "competitivo", "performance", "max-fps" },
            IsBuiltIn = true,
            Flags = new()
            {
                ["DFIntTaskSchedulerTargetFps"] = "9999",
                ["FFlagDisablePostFx"] = "True",
                ["FFlagDisablePlayerShadows"] = "True",
                ["FIntRenderShadowIntensity"] = "0",
                ["FIntRenderShadowmapBias"] = "0",
                ["FFlagDisableGrass"] = "True",
                ["FFlagDisableWind"] = "True",
                ["DFIntDebugFRMQualityLevelOverride"] = "1",
                ["DFIntTextureQualityOverride"] = "0",
                ["FIntDebugForceMSAASamples"] = "0",
                ["FIntFRMMinGrassDistance"] = "0",
                ["FIntFRMMaxGrassDistance"] = "0",
                ["DFIntCSGLevelOfDetailSwitchingDistance"] = "0",
                ["FIntRenderDistance"] = "100",
                ["FIntLightUpdateLimit"] = "0",
                ["FIntSSAO"] = "0",
                ["DFFlagDebugPauseVoxelizer"] = "True",
                ["FFlagDebugSkyGray"] = "True",
                ["FFlagHandleAltEnterFullscreenManually"] = "False",
                ["FFlagAdServiceEnabled"] = "False",
                ["FLogNetwork"] = "7"
            }
        },
        new FlagPresetTheme
        {
            Name = "Max Graphics",
            Description = "Iluminação Future Is Bright Phase 3, MSAA 8x, sombras máximas, texturas em 4K, grama distante e vento ativados.",
            Category = "Visual",
            Icon = "🎨",
            Tags = new() { "graficos", "visual", "qualidade", "ultra" },
            IsBuiltIn = true,
            Flags = new()
            {
                ["DFIntTaskSchedulerTargetFps"] = "9999",
                ["FFlagDebugForceFutureIsBrightPhase3"] = "True",
                ["FFlagGlobalWindRendering"] = "True",
                ["FIntRenderShadowIntensity"] = "100",
                ["FIntDebugForceMSAASamples"] = "8",
                ["FIntRenderDistance"] = "10000",
                ["FIntFRMMaxGrassDistance"] = "120",
                ["FIntFRMMinGrassDistance"] = "50",
                ["DFIntTextureQualityOverride"] = "4",
                ["FFlagFastGPULightCulling3"] = "True",
                ["FFlagNewLightAttenuation"] = "True",
                ["FIntRenderShadowmapBias"] = "2",
                ["FIntRenderShadowmapSize"] = "2048",
                ["FFlagDisablePostFx"] = "False",
                ["FFlagDisableSky"] = "False",
                ["FFlagDisableShadows"] = "False"
            }
        },
        new FlagPresetTheme
        {
            Name = "Low-End PC",
            Description = "Para PCs fracos. Desliga partículas, efeitos, SSAO, sombras, materiais, grama — força DX10 e qualidade mínima.",
            Category = "Performance",
            Icon = "💻",
            Tags = new() { "low-end", "fraco", "performance", "dx10" },
            IsBuiltIn = true,
            Flags = new()
            {
                ["DFIntTaskSchedulerTargetFps"] = "60",
                ["DFIntDebugFRMQualityLevelOverride"] = "1",
                ["FFlagDisablePostFx"] = "True",
                ["FFlagDisablePlayerShadows"] = "True",
                ["FIntRenderShadowIntensity"] = "0",
                ["FFlagDisableGrass"] = "True",
                ["FFlagDisableWind"] = "True",
                ["FFlagDisableParticles"] = "True",
                ["FFlagDisableEffects"] = "True",
                ["DFIntTextureQualityOverride"] = "0",
                ["FIntDebugForceMSAASamples"] = "0",
                ["FIntFRMMinGrassDistance"] = "0",
                ["FIntFRMMaxGrassDistance"] = "0",
                ["FIntSSAO"] = "0",
                ["FIntSSAOMipLevels"] = "0",
                ["FIntRenderDistance"] = "50",
                ["FIntLightUpdateLimit"] = "0",
                ["FFlagDebugSkyGray"] = "True",
                ["FFlagDebugGraphicsPreferD3D11FL10"] = "True",
                ["DFIntCSGLevelOfDetailSwitchingDistance"] = "0",
                ["FIntFontSizePadding"] = "2",
                ["FFlagRenderFixFog"] = "True",
                ["FFlagDisableMaterials"] = "True"
            }
        },
        new FlagPresetTheme
        {
            Name = "Privacy",
            Description = "Desliga toda telemetria, contadores, eventos, points, analytics e trackeamento do Roblox.",
            Category = "Privacy",
            Icon = "🔒",
            Tags = new() { "privacidade", "telemetria", "tracking", "no-telemetry" },
            IsBuiltIn = true,
            Flags = new()
            {
                ["FFlagDebugDisableTelemetryV2Stat"] = "True",
                ["FFlagDebugDisableTelemetryV2Counter"] = "True",
                ["FFlagDebugDisableTelemetryPoint"] = "True",
                ["FFlagDebugDisableTelemetryEventIngest"] = "True",
                ["FFlagDebugDisableTelemetry"] = "True",
                ["FFlagAdServiceEnabled"] = "False",
                ["FStringVoiceBetaBadgeLearnMoreLink"] = "",
                ["FFlagChatTranslationEnableSystemMessage"] = "False",
                ["FFlagReportGpuLimitedToPerfControl"] = "False",
                ["FLogNetwork"] = "0"
            }
        },
        new FlagPresetTheme
        {
            Name = "Network Boost",
            Description = "Otimiza envio de pacotes, paralelismo de replicação, latência de rede e taxa de atualização do servidor.",
            Category = "Network",
            Icon = "🌐",
            Tags = new() { "rede", "ping", "latencia", "network" },
            IsBuiltIn = true,
            Flags = new()
            {
                ["DFIntS2PhysicsSenderRate"] = "128",
                ["DFIntRakNetResendRttMultiple"] = "1",
                ["DFIntRakNetNakResendDelayMsMax"] = "100",
                ["DFIntRakNetNakResendDelayRttPercent"] = "50",
                ["DFIntNetworkClusterPacketCacheNumParallelTasks"] = "12",
                ["DFIntMegaReplicatorNumParallelTasks"] = "12",
                ["DFIntReplicationDataCacheNumParallelTasks"] = "12",
                ["DFIntClientPacketMaxFrameMicroseconds"] = "200",
                ["DFIntCodecMaxOutgoingFrames"] = "1000",
                ["DFIntPlayerNetworkUpdateRate"] = "60",
                ["DFIntMaxDataPacketPerSend"] = "2147483647",
                ["DFFlagRakNetCalculateApplicationFeedback2"] = "True",
                ["DFIntGraphicsOptimizationModeMaxFrameTimeTargetMs"] = "25",
                ["DFIntGraphicsOptimizationModeMinFrameTimeTargetMs"] = "16",
                ["FFlagOptimizeNetworkRouting"] = "True",
                ["DFFlagNetworkUseZstdWrapper"] = "False",
                ["FFlagOptimizeServerTickRate"] = "True",
                ["DFIntNetworkLatencyTolerance"] = "1"
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
