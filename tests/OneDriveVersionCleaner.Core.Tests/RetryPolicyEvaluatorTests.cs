using System.Net;
using OneDriveVersionCleaner.Core.Retry;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class RetryPolicyEvaluatorTests
{
    private readonly RetryPolicyEvaluator _sut = new();

    [Fact]
    public void TooManyRequests_UsesServerRetryAfter()
    {
        var decision = _sut.EvaluateHttpFailure(
            HttpStatusCode.TooManyRequests,
            failedAttemptNumber: 1,
            maximumRetryAttempts: 5,
            retryAfter: TimeSpan.FromSeconds(17));

        Assert.True(decision.ShouldRetry);
        Assert.Equal(TimeSpan.FromSeconds(17), decision.Delay);
        Assert.Equal(RetryReason.Throttled, decision.Reason);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, RetryReason.RequestTimeout)]
    [InlineData(HttpStatusCode.InternalServerError, RetryReason.ServerError)]
    [InlineData(HttpStatusCode.BadGateway, RetryReason.ServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable, RetryReason.ServerError)]
    [InlineData(HttpStatusCode.GatewayTimeout, RetryReason.ServerError)]
    public void TransientHttpStatus_IsRetried(HttpStatusCode status, RetryReason expectedReason)
    {
        var decision = _sut.EvaluateHttpFailure(
            status,
            failedAttemptNumber: 1,
            maximumRetryAttempts: 5);

        Assert.True(decision.ShouldRetry);
        Assert.Equal(expectedReason, decision.Reason);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public void PermanentHttpStatus_IsNotBlindlyRetried(HttpStatusCode status)
    {
        var decision = _sut.EvaluateHttpFailure(
            status,
            failedAttemptNumber: 1,
            maximumRetryAttempts: 5);

        Assert.False(decision.ShouldRetry);
        Assert.Equal(RetryReason.None, decision.Reason);
    }

    [Fact]
    public void ExponentialBackoff_DoublesAndHonorsCap()
    {
        var third = _sut.EvaluateHttpFailure(
            HttpStatusCode.InternalServerError,
            failedAttemptNumber: 3,
            maximumRetryAttempts: 5,
            baseDelay: TimeSpan.FromSeconds(2),
            maximumBackoff: TimeSpan.FromSeconds(5));

        Assert.Equal(TimeSpan.FromSeconds(5), third.Delay);
    }

    [Fact]
    public void RetryLimit_IsNumberOfRetriesAfterInitialAttempt()
    {
        var allowed = _sut.EvaluateHttpFailure(
            HttpStatusCode.InternalServerError,
            failedAttemptNumber: 2,
            maximumRetryAttempts: 2);
        var exhausted = _sut.EvaluateHttpFailure(
            HttpStatusCode.InternalServerError,
            failedAttemptNumber: 3,
            maximumRetryAttempts: 2);

        Assert.True(allowed.ShouldRetry);
        Assert.False(exhausted.ShouldRetry);
        Assert.Equal(RetryReason.RetryLimitReached, exhausted.Reason);
    }

    [Fact]
    public void NetworkFailure_IsRetriedButCancellationIsNot()
    {
        var network = _sut.EvaluateNetworkFailure(
            new HttpRequestException("offline"),
            failedAttemptNumber: 1,
            maximumRetryAttempts: 5);
        var cancellation = _sut.EvaluateNetworkFailure(
            new OperationCanceledException(),
            failedAttemptNumber: 1,
            maximumRetryAttempts: 5);

        Assert.True(network.ShouldRetry);
        Assert.Equal(RetryReason.NetworkError, network.Reason);
        Assert.False(cancellation.ShouldRetry);
    }

    [Fact]
    public void InvalidAttemptNumber_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.EvaluateHttpFailure(
            HttpStatusCode.TooManyRequests,
            failedAttemptNumber: 0,
            maximumRetryAttempts: 5));
    }
}
