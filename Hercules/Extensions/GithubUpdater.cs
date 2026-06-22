using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Hercules;

public static class GithubUpdater
{
    private const long MaximumUpdateSize = 512L * 1024 * 1024;
    private static readonly HttpClient http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "Hercules-Updater" } }
    };

    public static async Task<string?> GetLatestVersionTagAsync()
    {
        if (!App.IsRepositoryConfigured)
        {
            App.Logger.WriteLine("GitHubUpdater", "Update repository is not configured; skipping automatic updates.");
            return null;
        }

        try
        {
            string url = $"https://api.github.com/repos/{App.ProjectRepository}/releases/latest";
            string response = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.GetProperty("tag_name").GetString();
        }
        catch (Exception ex)
        {
            App.Logger.WriteLine("GitHubUpdater", $"Failed to get latest release tag: {ex}");
            return null;
        }
    }

    public static async Task<bool> DownloadAndInstallUpdate(string tag)
    {
        if (!App.IsRepositoryConfigured)
            return false;

        try
        {
            string url = $"https://api.github.com/repos/{App.ProjectRepository}/releases/latest";
            string response = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            string? releaseTag = doc.RootElement.GetProperty("tag_name").GetString();
            if (!string.Equals(releaseTag, tag, StringComparison.Ordinal))
            {
                App.Logger.WriteLine("GitHubUpdater", "Latest release changed during update check; retrying next launch.");
                return false;
            }

            var assets = doc.RootElement.GetProperty("assets");

            foreach (var asset in assets.EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                string digest = asset.TryGetProperty("digest", out var digestElement)
                    ? digestElement.GetString() ?? ""
                    : "";

                if (string.Equals(name, "Hercules.exe", StringComparison.OrdinalIgnoreCase))
                    return await UpdateExe(downloadUrl, name, digest);
            }

            App.Logger.WriteLine("GitHubUpdater", "No Hercules.exe release asset found.");
            return false;
        }
        catch (Exception ex)
        {
            App.Logger.WriteLine("GitHubUpdater", $"Update failed: {ex}");
            return false;
        }
    }

    private static async Task<bool> UpdateExe(string url, string name, string digest)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var downloadUri) || downloadUri.Scheme != Uri.UriSchemeHttps)
        {
            App.Logger.WriteLine("GitHubUpdater", "Rejected an invalid or non-HTTPS update URL.");
            return false;
        }

        if (!digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            App.Logger.WriteLine("GitHubUpdater", "Release asset has no SHA-256 digest; refusing an unverifiable update.");
            return false;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "Hercules_Update");
        Directory.CreateDirectory(tempDir);

        string exePath = Path.Combine(tempDir, name);
        using (var response = await http.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > MaximumUpdateSize)
                throw new InvalidDataException("Update exceeds the maximum allowed size.");

            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = new FileStream(exePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await input.ReadAsync(buffer)) > 0)
            {
                total += read;
                if (total > MaximumUpdateSize)
                    throw new InvalidDataException("Update exceeds the maximum allowed size.");
                await output.WriteAsync(buffer.AsMemory(0, read));
            }
        }

        await using (var downloaded = File.OpenRead(exePath))
        {
            string actualDigest = Convert.ToHexString(await SHA256.HashDataAsync(downloaded));
            string expectedDigest = digest["sha256:".Length..].Trim();
            if (!string.Equals(actualDigest, expectedDigest, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(exePath);
                App.Logger.WriteLine("GitHubUpdater", "Update SHA-256 verification failed.");
                return false;
            }
        }

        string currentExe = Environment.ProcessPath!;
        string backupExe = currentExe + ".old";
        if (File.Exists(backupExe)) File.Delete(backupExe);
        try
        {
            File.Move(currentExe, backupExe);
            File.Copy(exePath, currentExe, true);
            return true;
        }
        catch
        {
            if (!File.Exists(currentExe) && File.Exists(backupExe))
                File.Move(backupExe, currentExe);
            throw;
        }
    }
}
