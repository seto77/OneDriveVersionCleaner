namespace OneDriveVersionCleaner.Core.Models;

/// <summary>
/// A OneDrive drive and its optional local sync root.
/// </summary>
public sealed record DriveInfo
{
    public string DriveId { get; init; } = string.Empty;

    public string DriveType { get; init; } = string.Empty;

    public string WebUrl { get; init; } = string.Empty;

    public string LocalRootPath { get; init; } = string.Empty;
}
