using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Retention;
using OneDriveVersionCleaner.Core.Formatting;

namespace OneDriveVersionProbe;

internal static class ConsoleOutput
{
    public static void WriteVersions(
        IReadOnlyList<VersionEntry> versions,
        IReadOnlyDictionary<string, VersionRetentionDecision>? decisions = null)
    {
        Console.WriteLine("Current  Version        Date (local)             Size         Decision");
        Console.WriteLine("-------  -------------  -----------------------  -----------  ----------------");

        foreach (var version in versions.OrderByDescending(item => item.Created))
        {
            var current = version.IsCurrent ? "YES" : "";
            var date = version.Created?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "unknown";
            var size = ByteSizeFormatter.Format(version.Size);
            var decision = decisions is not null && decisions.TryGetValue(version.VersionId, out var value)
                ? value.Disposition.ToString()
                : "-";
            Console.WriteLine($"{current,-7}  {Truncate(version.VersionId, 13),-13}  {date,-23}  {size,11}  {decision}");
        }
    }

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..(length - 1)] + "…";
}
