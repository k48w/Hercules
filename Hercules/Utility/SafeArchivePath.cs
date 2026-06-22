namespace Hercules.Utility;

internal static class SafeArchivePath
{
    public static string Resolve(string extractionRoot, string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            throw new InvalidDataException("Archive entry has no name.");

        string root = Path.GetFullPath(extractionRoot);
        string rootPrefix = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
        string destination = Path.GetFullPath(Path.Combine(root, entryName));

        if (!destination.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Archive entry escapes the extraction directory: {entryName}");

        return destination;
    }
}
