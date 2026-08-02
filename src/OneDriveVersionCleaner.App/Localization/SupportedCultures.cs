using System.Globalization;

namespace OneDriveVersionCleaner.App.Localization;

/// <summary>One language the interface is offered in.</summary>
/// <param name="Name">
/// The neutral culture name. It is what <see cref="CultureInfo.CurrentUICulture"/> is set to and
/// what the saved preference stores.
/// </param>
/// <param name="NativeName">
/// How the language names itself. Menu entries use it as-is and never translate it, so a reader
/// who cannot read the current language can still find their own.
/// </param>
/// <param name="FontFamily">The interface font this language reads best in.</param>
internal sealed record UiCulture(string Name, string NativeName, string FontFamily);

/// <summary>
/// The languages the interface ships in, in one place. Everything that has to decide something
/// per language — the startup culture, the font, the language menu, the saved preference —
/// resolves it through here rather than testing for Japanese on its own.
/// </summary>
internal static class SupportedCultures
{
    /// <summary>Adding a language is one entry here plus the <c>ja:</c>-style arguments at each
    /// call site of <see cref="Localization.Loc"/>.</summary>
    public static readonly IReadOnlyList<UiCulture> All =
    [
        new("en", "English", "Segoe UI"),
        new("ja", "日本語", "Yu Gothic UI"),
    ];

    /// <summary>The language an unrecognised culture falls back to.</summary>
    public static UiCulture Default => All[0];

    /// <summary>
    /// Maps a culture name onto a supported language: exact match, then longest matching
    /// prefix (so "ja-JP" finds "ja"), then the culture's parent chain, then <see cref="Default"/>.
    /// </summary>
    public static UiCulture Resolve(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return Default;
        }

        var name = cultureName.Trim();
        var exact = All.FirstOrDefault(culture =>
            string.Equals(culture.Name, name, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        // Longest first, so a future "zh-Hant" is preferred over a "zh" that also matches.
        foreach (var culture in All.OrderByDescending(culture => culture.Name.Length))
        {
            if (name.StartsWith(culture.Name + "-", StringComparison.OrdinalIgnoreCase))
            {
                return culture;
            }
        }

        // Locales whose script is implied rather than spelled out are only reachable through
        // the parent chain, which the prefix test above cannot see.
        try
        {
            for (var parent = CultureInfo.GetCultureInfo(name).Parent;
                 !string.IsNullOrEmpty(parent.Name);
                 parent = parent.Parent)
            {
                var byParent = All.FirstOrDefault(culture =>
                    string.Equals(culture.Name, parent.Name, StringComparison.OrdinalIgnoreCase));
                if (byParent is not null)
                {
                    return byParent;
                }
            }
        }
        catch (CultureNotFoundException)
        {
            return Default;
        }

        return Default;
    }

    /// <summary>The language the current thread renders in.</summary>
    public static UiCulture Current => Resolve(CultureInfo.CurrentUICulture.Name);
}
