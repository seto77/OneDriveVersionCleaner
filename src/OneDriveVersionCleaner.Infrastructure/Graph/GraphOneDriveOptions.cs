using OneDriveVersionCleaner.Infrastructure.Authentication;

namespace OneDriveVersionCleaner.Infrastructure.Graph;

public sealed record GraphOneDriveOptions
{
    public required GraphAuthenticationOptions Authentication { get; init; }

    public string LocalOneDriveRoot { get; init; } = "";

    public int RetryCount { get; init; } = 5;

    public Uri GraphBaseUri { get; init; } = new("https://graph.microsoft.com/v1.0/");
}
