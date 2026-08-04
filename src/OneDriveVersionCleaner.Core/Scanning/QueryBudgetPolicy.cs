namespace OneDriveVersionCleaner.Core.Scanning;

/// <summary>
/// How one run's version queries are divided into what is asked about now and what is left for
/// a later run.
/// </summary>
/// <param name="Selected">
/// The files to query, largest current size first. Never longer than the limit.
/// </param>
/// <param name="DeferredCount">
/// How many files the limit pushed out of this run. They are not lost: nothing was recorded for
/// them, so the next run selects them again.
/// </param>
public sealed record QueryBudget(IReadOnlyList<LocalCandidateFile> Selected, int DeferredCount);

/// <summary>
/// Caps how many files a single run asks the service about.
/// </summary>
/// <remarks>
/// The size threshold already bounds the request count, but on a large sync folder it can still
/// leave several thousand files to ask about, and SharePoint Online starts failing requests well
/// before that many go through. Around two thousand files is answered without trouble; ten
/// thousand is not. So the count is capped outright rather than only being confirmed, and the
/// remainder waits for the next run.
/// </remarks>
public static class QueryBudgetPolicy
{
    /// <summary>
    /// Chooses which of <paramref name="candidates"/> this run may query.
    /// </summary>
    /// <param name="maximumFiles">
    /// The most files one run may ask about. Zero lifts the limit, matching how the other rules
    /// read zero.
    /// </param>
    /// <remarks>
    /// The selection is by current file size, largest first, whether or not the limit is doing
    /// anything: a big file is where the history worth reclaiming is, so it is what a limited run
    /// should spend its requests on — and ordering the unlimited case the same way means a run cut
    /// short by throttling has still read the files that mattered most. Files of equal size are
    /// ordered by path so that two runs over an unchanged folder make the same choice.
    /// </remarks>
    public static QueryBudget Apply(IReadOnlyList<LocalCandidateFile> candidates, int maximumFiles)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumFiles);

        var ordered = candidates
            .OrderByDescending(candidate => candidate.Length)
            .ThenBy(candidate => candidate.LocalPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (maximumFiles == 0 || ordered.Length <= maximumFiles)
        {
            return new QueryBudget(ordered, 0);
        }

        return new QueryBudget(ordered[..maximumFiles], ordered.Length - maximumFiles);
    }
}
