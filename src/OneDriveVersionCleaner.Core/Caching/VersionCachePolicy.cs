namespace OneDriveVersionCleaner.Core.Caching;

/// <summary>
/// Decides whether a file's version list has to be read from the service again.
/// </summary>
public static class VersionCachePolicy
{
    /// <summary>
    /// A file is asked about again only when the cached answer is old enough
    /// <em>and</em> the file has changed since that answer was recorded. History only grows
    /// when the file is edited, so an untouched file never needs another request; the age
    /// requirement then keeps a busy file from being asked about on every run.
    /// </summary>
    /// <param name="cached">The previous answer, or null when the file was never queried.</param>
    /// <param name="currentLastModified">The file's last modification as it stands now.</param>
    /// <param name="now">Current time.</param>
    /// <param name="minimumAge">
    /// How old a cached answer must be before the file is reconsidered. Zero means age is not
    /// a constraint, so a changed file is always re-read.
    /// </param>
    public static bool ShouldRequery(
        CachedFileVersions? cached,
        DateTimeOffset? currentLastModified,
        DateTimeOffset now,
        TimeSpan minimumAge)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumAge, TimeSpan.Zero);

        if (cached is null)
        {
            return true;
        }

        if (now - cached.QueriedAtUtc < minimumAge)
        {
            return false;
        }

        return HasChangedSince(cached, currentLastModified);
    }

    /// <summary>
    /// True when the file looks different from the moment it was cached. A timestamp missing
    /// on either side is treated as a change: re-reading costs one request, while wrongly
    /// assuming nothing moved would leave the cached history quietly out of date.
    /// </summary>
    public static bool HasChangedSince(
        CachedFileVersions cached,
        DateTimeOffset? currentLastModified)
    {
        ArgumentNullException.ThrowIfNull(cached);

        if (cached.FileLastModified is not { } cachedModified ||
            currentLastModified is not { } current)
        {
            return true;
        }

        return current != cachedModified;
    }
}
