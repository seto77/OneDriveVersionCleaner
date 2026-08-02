namespace OneDriveVersionCleaner.Core.Paths;

/// <summary>
/// Converts a path below a Windows OneDrive sync root into raw and Graph-safe cloud paths.
/// Cloud identity must still be confirmed by resolving the result to a Drive Item ID.
/// </summary>
public static class LocalPathMapper
{
    public static LocalPathMapping Map(string localRootPath, string localPath)
    {
        var normalizedRoot = NormalizeAbsolutePath(localRootPath, nameof(localRootPath));
        var normalizedPath = NormalizeAbsolutePath(localPath, nameof(localPath));

        var relativePath = Path.GetRelativePath(normalizedRoot, normalizedPath);
        if (IsOutsideRoot(relativePath))
        {
            throw new ArgumentException(
                $"The path '{localPath}' is outside the OneDrive root '{localRootPath}'.",
                nameof(localPath));
        }

        var cloudRelativePath = relativePath == "."
            ? string.Empty
            : relativePath.Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

        var segments = SplitCloudRelativePath(cloudRelativePath);
        return new LocalPathMapping(
            normalizedRoot,
            normalizedPath,
            string.Join('/', segments),
            segments,
            string.Join('/', segments.Select(EncodePathSegment)));
    }

    public static string ToCloudRelativePath(string localRootPath, string localPath) =>
        Map(localRootPath, localPath).CloudRelativePath;

    public static string ToEncodedCloudRelativePath(string localRootPath, string localPath) =>
        Map(localRootPath, localPath).EncodedCloudRelativePath;

    public static IReadOnlyList<string> SplitCloudRelativePath(string cloudRelativePath)
    {
        ArgumentNullException.ThrowIfNull(cloudRelativePath);

        var segments = cloudRelativePath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException(
                "Cloud-relative paths cannot contain '.' or '..' segments.",
                nameof(cloudRelativePath));
        }

        return segments;
    }

    public static string EncodePathSegment(string segment)
    {
        ArgumentException.ThrowIfNullOrEmpty(segment);

        if (segment is "." or ".." || segment.Contains('/') || segment.Contains('\\'))
        {
            throw new ArgumentException(
                "A cloud path segment cannot contain separators or traversal components.",
                nameof(segment));
        }

        return Uri.EscapeDataString(segment);
    }

    public static string EncodeCloudRelativePath(string cloudRelativePath) =>
        string.Join('/', SplitCloudRelativePath(cloudRelativePath).Select(EncodePathSegment));

    private static string NormalizeAbsolutePath(string path, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, parameterName);

        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("An absolute path is required.", parameterName);
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private static bool IsOutsideRoot(string relativePath) =>
        Path.IsPathRooted(relativePath) ||
        relativePath == ".." ||
        relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
        relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
}
