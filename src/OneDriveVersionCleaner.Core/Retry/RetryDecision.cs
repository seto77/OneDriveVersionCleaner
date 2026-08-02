namespace OneDriveVersionCleaner.Core.Retry;

public enum RetryReason
{
    None,
    Throttled,
    RequestTimeout,
    ServerError,
    NetworkError,
    RetryLimitReached,
}

public sealed record RetryDecision(
    bool ShouldRetry,
    TimeSpan Delay,
    RetryReason Reason);
