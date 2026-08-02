using System.Globalization;

namespace OneDriveVersionCleaner.App.Tests;

/// <summary>
/// Runs a test under a chosen interface language and puts the previous one back afterwards, so
/// a test never depends on the language of the machine it runs on.
/// </summary>
internal sealed class UiCultureScope : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentUICulture;

    public UiCultureScope(string cultureName) =>
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

    public void Dispose() => CultureInfo.CurrentUICulture = _previous;
}
