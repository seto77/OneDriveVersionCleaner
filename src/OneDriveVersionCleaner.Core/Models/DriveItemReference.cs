namespace OneDriveVersionCleaner.Core.Models;

/// <summary>
/// The stable cloud identity returned when a local/cloud path is resolved.
/// </summary>
public sealed record DriveItemReference
{
    public string DriveId { get; init; } = string.Empty;

    public string ItemId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string CloudPath { get; init; } = string.Empty;

    public string WebUrl { get; init; } = string.Empty;

    public bool IsFolder { get; init; }
}
