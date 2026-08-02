using System.Net;

namespace OneDriveVersionCleaner.Infrastructure.Graph;

public sealed class GraphRequestException : Exception
{
    public GraphRequestException(HttpStatusCode statusCode, string? errorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ErrorCode { get; }

    public bool IsTransient => StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
        (int)StatusCode is >= 500 and <= 599;
}
