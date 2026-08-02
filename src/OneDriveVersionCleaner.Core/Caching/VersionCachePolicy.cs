namespace OneDriveVersionCleaner.Core.Caching;

/// <summary>
/// Decides whether a file's version list has to be read from the service again.
/// </summary>
public static class VersionCachePolicy
{
    /// <summary>
    /// A file is asked about again only when the cached answer is old enough <em>and</em> the
    /// synced local copy has changed since that answer was recorded. History only grows when
    /// the file is edited, so an untouched file never needs another request; the age
    /// requirement then keeps a busy file from being asked about on every run.
    /// </summary>
    /// <param name="cached">The previous answer, or null when the file was never queried.</param>
    /// <param name="currentLocalLastWriteUtc">
    /// The local copy's write time as it stands now, as the survey observed it. An entry that
    /// carries no local timestamp is re-read once and then carries one.
    /// </param>
    /// <param name="now">Current time.</param>
    /// <param name="minimumAge">
    /// How old a cached answer must be before the file is reconsidered. Zero means age is not
    /// a constraint, so a changed file is always re-read.
    /// </param>
    public static bool ShouldRequery(
        CachedFileVersions? cached,
        DateTimeOffset currentLocalLastWriteUtc,
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

        return cached.LocalLastWriteUtc is not { } cachedLocalLastWrite ||
            cachedLocalLastWrite != currentLocalLastWriteUtc;
    }
}
