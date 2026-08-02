namespace OneDriveVersionCleaner.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var settings = AppSettings.Load(out var settingsWarning);
        Application.Run(new MainForm(settings, settingsWarning));
    }
}
