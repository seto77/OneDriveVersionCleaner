using System.Net;

namespace OneDriveVersionCleaner.Core.Retry;

/// <summary>
/// Classifies transient failures. <paramref name="failedAttemptNumber"/> is one-based;
/// <paramref name="maximumRetryAttempts"/> is the number of retries after the initial request.
/// </summary>
public sealed class RetryPolicyEvaluator
{
    public RetryDecision EvaluateHttpFailure(
        HttpStatusCode statusCode,
        int failedAttemptNumber,
        int maximumRetryAttempts,
        TimeSpan? retryAfter = null,
        TimeSpan? baseDelay = null,
        TimeSpan? maximumBackoff = null)
    {
        Validate(failedAttemptNumber, maximumRetryAttempts, retryAfter, baseDelay, maximumBackoff);

        var reason = statusCode switch
        {
            HttpStatusCode.TooManyRequests => RetryReason.Throttled,
            HttpStatusCode.RequestTimeout => RetryReason.RequestTimeout,
            _ when (int)statusCode is >= 500 and <= 599 => RetryReason.ServerError,
            _ => RetryReason.None,
        };

        return CreateDecision(
            reason,
            failedAttemptNumber,
            maximumRetryAttempts,
            retryAfter,
            baseDelay,
            maximumBackoff);
    }

    public RetryDecision EvaluateNetworkFailure(
        Exception exception,
        int failedAttemptNumber,
        int maximumRetryAttempts,
        TimeSpan? baseDelay = null,
        TimeSpan? maximumBackoff = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Validate(failedAttemptNumber, maximumRetryAttempts, null, baseDelay, maximumBackoff);

        if (exception is OperationCanceledException)
        {
            return new RetryDecision(false, TimeSpan.Zero, RetryReason.None);
        }

        var isTransient = exception is HttpRequestException or IOException or TimeoutException;
        return CreateDecision(
            isTransient ? RetryReason.NetworkError : RetryReason.None,
            failedAttemptNumber,
            maximumRetryAttempts,
            retryAfter: null,
            baseDelay,
            maximumBackoff);
    }

    private static RetryDecision CreateDecision(
        RetryReason reason,
        int failedAttemptNumber,
        int maximumRetryAttempts,
        TimeSpan? retryAfter,
        TimeSpan? baseDelay,
        TimeSpan? maximumBackoff)
    {
        if (reason == RetryReason.None)
        {
            return new RetryDecision(false, TimeSpan.Zero, RetryReason.None);
        }

        if (failedAttemptNumber > maximumRetryAttempts)
        {
            return new RetryDecision(false, TimeSpan.Zero, RetryReason.RetryLimitReached);
        }

        var delay = retryAfter ?? CalculateBackoff(
            failedAttemptNumber,
            baseDelay ?? TimeSpan.FromSeconds(1),
            maximumBackoff ?? TimeSpan.FromSeconds(30));

        return new RetryDecision(true, delay, reason);
    }

    private static TimeSpan CalculateBackoff(
        int failedAttemptNumber,
        TimeSpan baseDelay,
        TimeSpan maximumBackoff)
    {
        var exponent = Math.Min(failedAttemptNumber - 1, 30);
        var ticks = Math.Min(
            (decimal)maximumBackoff.Ticks,
            (decimal)baseDelay.Ticks * (1L << exponent));
        return TimeSpan.FromTicks((long)ticks);
    }

    private static void Validate(
        int failedAttemptNumber,
        int maximumRetryAttempts,
        TimeSpan? retryAfter,
        TimeSpan? baseDelay,
        TimeSpan? maximumBackoff)
    {
        if (failedAttemptNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(failedAttemptNumber));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(maximumRetryAttempts);

        if (retryAfter < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryAfter));
        }

        if (baseDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(baseDelay));
        }

        if (maximumBackoff <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBackoff));
        }
    }
}
