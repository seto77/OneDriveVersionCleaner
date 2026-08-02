namespace OneDriveVersionCleaner.App.Localization;

/// <summary>
/// Resolves a string written inline in the code to the current interface language.
/// </summary>
/// <remarks>
/// The translations live at the call site rather than in a satellite resource file. The forms
/// here are built on the Windows Forms design surface, and the designer owns the form's
/// <c>.resx</c> outright: it rewrites the file whenever the form is saved, so hand-written
/// entries there would survive only until the next time somebody opened the designer. The
/// design surface therefore carries English, and the translation is applied over it at run time.
/// </remarks>
internal static class Localization
{
    /// <summary>
    /// Returns the argument matching the current interface language, falling back to
    /// <paramref name="en"/> for a language that has no translation yet. Call it with named
    /// arguments, so both texts read as a pair:
    /// <code>button.Text = Loc(en: "Rescan", ja: "再検出");</code>
    /// </summary>
    public static string Loc(string en, string ja = "")
    {
        var picked = SupportedCultures.Current.Name switch
        {
            "ja" => ja,
            _ => en,
        };

        return string.IsNullOrEmpty(picked) ? en : picked;
    }
}
