using System;
using System.IO;
using System.Text.Json;
using Hercules;

public static class HerculesRobloxSettingsManager // lowk didnt know what tf to name this file
{
    public class HerculesRobloxSettings
    {
        public int MemoryCleanerIntervalSeconds { get; set; }
    }

    private static readonly string FolderPath = Paths.Base;

    private static readonly string FilePath =
        Path.Combine(FolderPath, "HerculesRobloxSaves.json");

    public static HerculesRobloxSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new HerculesRobloxSettings();

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<HerculesRobloxSettings>(json)
                   ?? new HerculesRobloxSettings();
        }
        catch
        {
            return new HerculesRobloxSettings();
        }
    }

    public static void Save(HerculesRobloxSettings settings)
    {
        try
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
        }
    }
}
