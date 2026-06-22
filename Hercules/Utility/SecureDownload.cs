using System.Security.Cryptography;

namespace Hercules.Utility;

internal static class SecureDownload
{
    public static async Task<byte[]> DownloadBytesBoundedAsync(
        HttpClient client,
        Uri source,
        long maximumBytes,
        string? requiredMediaTypePrefix = null,
        CancellationToken cancellationToken = default)
    {
        if (source.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Downloads must use HTTPS.");

        if (maximumBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));

        using var response = await client.GetAsync(
            source,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        if (response.RequestMessage?.RequestUri?.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("The download redirected to a non-HTTPS address.");

        string? mediaType = response.Content.Headers.ContentType?.MediaType;
        if (requiredMediaTypePrefix is not null &&
            (mediaType is null || !mediaType.StartsWith(requiredMediaTypePrefix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException($"Unexpected content type: {mediaType ?? "unknown"}.");
        }

        if (response.Content.Headers.ContentLength is long length && length > maximumBytes)
            throw new InvalidDataException($"Download exceeds the {maximumBytes} byte limit.");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var output = new MemoryStream();
        byte[] buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > maximumBytes)
                throw new InvalidDataException($"Download exceeds the {maximumBytes} byte limit.");

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return output.ToArray();
    }

    public static bool HasExpectedSha256(string path, string expectedSha256)
    {
        if (!File.Exists(path) || string.IsNullOrWhiteSpace(expectedSha256))
            return false;

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            string actualSha256 = Convert.ToHexString(SHA256.HashData(stream));
            return string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static async Task DownloadToFileBoundedAsync(
        HttpClient client,
        Uri source,
        string destination,
        long maximumBytes,
        CancellationToken cancellationToken = default)
    {
        if (source.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Downloads must use HTTPS.");

        if (maximumBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));

        string temporaryPath = destination + ".download";
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Delete(temporaryPath);

        try
        {
            using var response = await client.GetAsync(
                source,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (response.RequestMessage?.RequestUri?.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("The download redirected to a non-HTTPS address.");

            if (response.Content.Headers.ContentLength is long length && length > maximumBytes)
                throw new InvalidDataException($"Download exceeds the {maximumBytes} byte limit.");

            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true);
            byte[] buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > maximumBytes)
                    throw new InvalidDataException($"Download exceeds the {maximumBytes} byte limit.");

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }

            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            output.Close();
            File.Move(temporaryPath, destination, overwrite: true);
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }

    public static async Task DownloadVerifiedAsync(
        HttpClient client,
        Uri source,
        string destination,
        string expectedSha256,
        long maximumBytes,
        CancellationToken cancellationToken = default)
    {
        if (source.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Downloads must use HTTPS.");

        if (maximumBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));

        string temporaryPath = destination + ".download";
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Delete(temporaryPath);

        try
        {
            using var response = await client.GetAsync(
                source,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (response.RequestMessage?.RequestUri?.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("The download redirected to a non-HTTPS address.");

            if (response.Content.Headers.ContentLength is long length && length > maximumBytes)
                throw new InvalidDataException($"Download exceeds the {maximumBytes} byte limit.");

            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                byte[] buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > maximumBytes)
                        throw new InvalidDataException($"Download exceeds the {maximumBytes} byte limit.");

                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
            }

            await using var downloaded = File.OpenRead(temporaryPath);
            string actualSha256 = Convert.ToHexString(
                await SHA256.HashDataAsync(downloaded, cancellationToken).ConfigureAwait(false));

            if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new CryptographicException(
                    $"SHA-256 mismatch. Expected {expectedSha256}, got {actualSha256}.");

            File.Move(temporaryPath, destination, overwrite: true);
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }
}
