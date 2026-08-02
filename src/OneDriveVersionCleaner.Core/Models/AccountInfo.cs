namespace OneDriveVersionCleaner.Core.Models;

/// <summary>
/// The signed-in Microsoft 365 account.
/// </summary>
public sealed record AccountInfo
{
    public string TenantId { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string UserPrincipalName { get; init; } = string.Empty;
}
