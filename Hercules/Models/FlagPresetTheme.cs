namespace Hercules.Models;

public class FlagPresetTheme
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Icon { get; set; } = "🏷️";
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Flags { get; set; } = new();
    public bool IsBuiltIn { get; set; }
}
