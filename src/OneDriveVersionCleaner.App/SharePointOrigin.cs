namespace OneDriveVersionCleaner.App;

/// <summary>
/// The browser origin that the sign-in window and the REST session both have to agree on.
/// </summary>
internal static class SharePointOrigin
{
    public static bool IsHttps(Uri uri) =>
        uri.IsAbsoluteUri && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Accepts any document on the personal OneDrive origin. A browser origin is
    /// scheme + host + port, so the modern OneDrive routes (<c>/my</c>, <c>/</c>,
    /// <c>/shared</c>) can issue the personal site's REST calls with the same cookies as
    /// <c>/personal/&lt;user&gt;/...</c>. Which API paths may be called stays restricted
    /// separately, by the session's own endpoint check.
    /// </summary>
    public static bool IsSameOrigin(Uri siteUri, string? source) =>
        Uri.TryCreate(source, UriKind.Absolute, out var sourceUri) &&
        IsHttps(sourceUri) &&
        string.Equals(sourceUri.Host, siteUri.Host, StringComparison.OrdinalIgnoreCase) &&
        sourceUri.Port == siteUri.Port;
}
