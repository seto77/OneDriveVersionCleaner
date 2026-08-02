namespace OneDriveVersionCleaner.Infrastructure.OneDriveAccounts;

/// <summary>
/// The small, non-secret subset of an OneDrive account registry key needed for discovery.
/// </summary>
public sealed record OneDriveAccountRegistrySnapshot
{
    public required string AccountKey { get; init; }

    public string? DisplayName { get; init; }

    public string? UserFolder { get; init; }

    public string? ConfiguredTenantId { get; init; }

    public string? TenantId { get; init; }

    public string? ServiceEndpointUri { get; init; }
}
