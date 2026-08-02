namespace OneDriveVersionCleaner.Infrastructure.OneDriveAccounts;

/// <summary>
/// A locally configured OneDrive for Business account whose SharePoint endpoint
/// can be used to address the signed-in user's own document library.
/// </summary>
public sealed record OneDriveBusinessAccount
{
    public required string AccountKey { get; init; }

    public required string DisplayName { get; init; }

    public required string UserFolder { get; init; }

    public required string TenantId { get; init; }

    public required Uri ServiceEndpointUri { get; init; }

    public required Uri SiteUri { get; init; }
}
