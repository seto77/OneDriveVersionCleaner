namespace OneDriveVersionCleaner.Core.Models;

/// <summary>
/// A current or historical version of a OneDrive file.
/// </summary>
public sealed record VersionEntry
{
    public string VersionId { get; init; } = string.Empty;

    public string? Label { get; init; }

    public DateTimeOffset? Created { get; init; }

    public string? ModifiedBy { get; init; }

    public long? Size { get; init; }

    public bool IsCurrent { get; init; }

    /// <summary>
    /// False when the API has already established that this version cannot be deleted.
    /// Null means that deleteability cannot be known until a delete is attempted.
    /// </summary>
    public bool? CanDelete { get; init; }

    public RetrievalStatus RetrievalStatus { get; init; } = RetrievalStatus.Success;

    public string? Error { get; init; }
}
