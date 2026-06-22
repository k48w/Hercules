using System.Text.Json.Serialization;

namespace Hercules.UI.Elements.Settings.Pages
{
    public sealed class GithubRelease
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset PublishedAt { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("assets")]
        public GithubAsset[] Assets { get; set; } = Array.Empty<GithubAsset>();

        public int TotalDownloads { get; private set; }

        public void CalculateTotals() =>
            TotalDownloads = Assets.Sum(asset => asset.DownloadCount);
    }

    public sealed class GithubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("download_count")]
        public int DownloadCount { get; set; }

        public double SizeMb => Size / 1024d / 1024d;
    }
}
