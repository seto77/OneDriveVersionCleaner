namespace OneDriveVersionCleaner.Core.Models;

public enum DeleteResultStatus
{
    Deleted,
    AlreadyDeleted,
    NotDeletable,
    AccessDenied,
    TransientError,
    PermanentError,
}

public sealed record DeleteResult
{
    public DeleteResultStatus Status { get; init; }

    public string? Error { get; init; }

    public bool IsSuccess => Status is DeleteResultStatus.Deleted or DeleteResultStatus.AlreadyDeleted;
}
