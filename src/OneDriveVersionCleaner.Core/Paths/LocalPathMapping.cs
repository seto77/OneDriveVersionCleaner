namespace OneDriveVersionCleaner.Core.Paths;

public sealed record LocalPathMapping(
    string LocalRootPath,
    string LocalPath,
    string CloudRelativePath,
    IReadOnlyList<string> CloudPathSegments,
    string EncodedCloudRelativePath);
