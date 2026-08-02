using System.Security;

namespace OneDriveVersionCleaner.Infrastructure.OneDriveAccounts;

/// <summary>
/// Discovers usable OneDrive for Business accounts from the current Windows profile.
/// Invalid or incomplete entries are ignored and never used to construct a remote request.
/// </summary>
public sealed class OneDriveBusinessAccountDiscovery
{
    private static readonly string[] AllowedSharePointHostSuffixes =
    [
        "sharepoint.com",
        "sharepoint.us",
        "sharepoint.de",
        "sharepoint.cn",
        "sharepoint-mil.us",
    ];

    private readonly IOneDriveAccountRegistryReader _registryReader;

    public OneDriveBusinessAccountDiscovery()
        : this(new WindowsOneDriveAccountRegistryReader())
    {
    }

    public OneDriveBusinessAccountDiscovery(IOneDriveAccountRegistryReader registryReader)
    {
        _registryReader = registryReader ?? throw new ArgumentNullException(nameof(registryReader));
    }

    public IReadOnlyList<OneDriveBusinessAccount> Discover()
    {
        IReadOnlyList<OneDriveAccountRegistrySnapshot> snapshots;
        try
        {
            snapshots = _registryReader.ReadAccounts() ?? [];
        }
        catch (Exception exception) when (IsExpectedDiscoveryFailure(exception))
        {
            return [];
        }

        var discovered = new List<OneDriveBusinessAccount>();
        foreach (var snapshot in snapshots)
        {
            if (TryCreateAccount(snapshot, out var account))
            {
                discovered.Add(account);
            }
        }

        return discovered
            .OrderBy(account => account.AccountKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public OneDriveBusinessAccount? FindByLocalPath(string? localPath) =>
        FindBestMatch(Discover(), localPath);

    /// <summary>
    /// Finds the account containing <paramref name="localPath"/>. Nested roots win over
    /// their parents, and path comparisons follow Windows case-insensitive semantics.
    /// </summary>
    public static OneDriveBusinessAccount? FindBestMatch(
        IEnumerable<OneDriveBusinessAccount> accounts,
        string? localPath)
    {
        ArgumentNullException.ThrowIfNull(accounts);

        if (!TryNormalizeAbsolutePath(localPath, out var normalizedPath))
        {
            return null;
        }

        return accounts
            .Where(account => IsAtOrBelow(normalizedPath, account.UserFolder))
            .OrderByDescending(account => account.UserFolder.Length)
            .ThenBy(account => account.AccountKey, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    internal static bool IsBusinessAccountKey(string? accountKey)
    {
        const string prefix = "Business";
        if (string.IsNullOrEmpty(accountKey) ||
            !accountKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            accountKey.Length == prefix.Length)
        {
            return false;
        }

        return accountKey.AsSpan(prefix.Length).IndexOfAnyExceptInRange('0', '9') < 0;
    }

    private static bool TryCreateAccount(
        OneDriveAccountRegistrySnapshot? snapshot,
        out OneDriveBusinessAccount account)
    {
        account = null!;
        if (snapshot is null ||
            !IsBusinessAccountKey(snapshot.AccountKey) ||
            !TryNormalizeAbsolutePath(snapshot.UserFolder, out var userFolder) ||
            !TryNormalizeTenantId(snapshot.ConfiguredTenantId, snapshot.TenantId, out var tenantId) ||
            !TryNormalizeEndpoint(snapshot.ServiceEndpointUri, out var serviceEndpoint, out var siteUri))
        {
            return false;
        }

        account = new OneDriveBusinessAccount
        {
            AccountKey = snapshot.AccountKey,
            DisplayName = NormalizeDisplayName(snapshot.DisplayName, snapshot.AccountKey),
            UserFolder = userFolder,
            TenantId = tenantId,
            ServiceEndpointUri = serviceEndpoint,
            SiteUri = siteUri,
        };

        return true;
    }

    private static bool TryNormalizeTenantId(
        string? configuredTenantId,
        string? tenantId,
        out string normalizedTenantId)
    {
        foreach (var candidate in new[] { configuredTenantId, tenantId })
        {
            if (Guid.TryParse(candidate, out var parsedTenantId) && parsedTenantId != Guid.Empty)
            {
                normalizedTenantId = parsedTenantId.ToString("D");
                return true;
            }
        }

        normalizedTenantId = string.Empty;
        return false;
    }

    private static bool TryNormalizeEndpoint(
        string? rawEndpoint,
        out Uri serviceEndpoint,
        out Uri siteUri)
    {
        serviceEndpoint = null!;
        siteUri = null!;

        if (string.IsNullOrWhiteSpace(rawEndpoint) ||
            !Uri.TryCreate(rawEndpoint.Trim(), UriKind.Absolute, out var parsedEndpoint) ||
            !string.Equals(parsedEndpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !parsedEndpoint.IsDefaultPort ||
            parsedEndpoint.UserInfo.Length != 0 ||
            parsedEndpoint.Query.Length != 0 ||
            parsedEndpoint.Fragment.Length != 0 ||
            !IsAllowedSharePointHost(parsedEndpoint.IdnHost))
        {
            return false;
        }

        var endpointPath = parsedEndpoint.AbsolutePath.TrimEnd('/');
        const string apiSuffix = "/_api";
        if (!endpointPath.EndsWith(apiSuffix, StringComparison.OrdinalIgnoreCase) ||
            endpointPath.Contains("%2f", StringComparison.OrdinalIgnoreCase) ||
            endpointPath.Contains("%5c", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var authority = parsedEndpoint.GetLeftPart(UriPartial.Authority);
        var sitePath = endpointPath[..^apiSuffix.Length];
        var siteText = sitePath.Length == 0 ? $"{authority}/" : $"{authority}{sitePath}";

        if (!Uri.TryCreate(
                $"{authority}{endpointPath}",
                UriKind.Absolute,
                out var normalizedServiceEndpoint) ||
            !Uri.TryCreate(siteText, UriKind.Absolute, out var normalizedSiteUri))
        {
            return false;
        }

        serviceEndpoint = normalizedServiceEndpoint;
        siteUri = normalizedSiteUri;
        return true;
    }

    private static bool IsAllowedSharePointHost(string host) =>
        AllowedSharePointHostSuffixes.Any(suffix =>
            host.Equals(suffix, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith($".{suffix}", StringComparison.OrdinalIgnoreCase));

    private static bool TryNormalizeAbsolutePath(string? path, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var candidate = path.Trim();
            if (!Path.IsPathFullyQualified(candidate))
            {
                return false;
            }

            normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
            return normalizedPath.Length != 0;
        }
        catch (Exception exception) when (exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            normalizedPath = string.Empty;
            return false;
        }
    }

    private static bool IsAtOrBelow(string normalizedPath, string normalizedRoot)
    {
        if (normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (Path.EndsInDirectorySeparator(normalizedRoot))
        {
            return true;
        }

        return normalizedPath.Length > normalizedRoot.Length &&
            normalizedPath[normalizedRoot.Length] is '\\' or '/';
    }

    private static string NormalizeDisplayName(string? displayName, string accountKey) =>
        string.IsNullOrWhiteSpace(displayName) ? accountKey : displayName.Trim();

    private static bool IsExpectedDiscoveryFailure(Exception exception) =>
        exception is SecurityException
            or UnauthorizedAccessException
            or IOException
            or PlatformNotSupportedException;
}
