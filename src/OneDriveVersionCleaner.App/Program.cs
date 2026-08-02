using OneDriveVersionCleaner.App.Localization;

namespace OneDriveVersionCleaner.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var settings = AppSettings.Load(out var settingsReadFailure);

        // Before the first form exists, so nothing is ever built in one language and shown in
        // another.
        UiLanguage.Initialize(settings.Language);
        Application.Run(new MainForm(settings, settingsReadFailure));
    }
}
