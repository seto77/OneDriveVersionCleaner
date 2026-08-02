using System.Text.Json;

namespace OneDriveVersionCleaner.App;

internal sealed class AppSettings
{
    public string LocalOneDriveRoot { get; set; } = string.Empty;

    public string TargetPath { get; set; } = string.Empty;

    /// <summary>How many of the newest versions are kept regardless of age. Zero disables the rule.</summary>
    public int KeepLatestVersions { get; set; }

    /// <summary>
    /// How far back from the file's own last modification its versions are kept.
    /// Zero disables the rule.
    /// </summary>
    public int KeepDays { get; set; }

    public int ProtectRecentlyModifiedFileDays { get; set; } = 7;

    public long MinimumHistorySizeMB { get; set; } = 200;

    /// <summary>
    /// Files smaller than this are never asked about. Read from the local sync folder, so the
    /// filter costs nothing and keeps small files from consuming the request budget.
    /// </summary>
    public long MinimumFileSizeMB { get; set; } = 50;

    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// How long an earlier version query stays good. A file is only asked about again once its
    /// cached answer is at least this old and the file itself has changed.
    /// </summary>
    public int VersionCacheMinimumAgeDays { get; set; } = 10;

    public string ExportDirectory { get; set; } =
        "%LOCALAPPDATA%\\OneDriveVersionCleaner\\Reports";

    public static AppSettings Load(out string? warning)
    {
        warning = null;
        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "appsettings.json"),
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        var path = candidates.FirstOrDefault(File.Exists);
        AppSettings settings;
        if (path is null)
        {
            settings = new AppSettings();
        }
        else
        {
            try
            {
                var json = File.ReadAllText(path);
                settings = JsonSerializer.Deserialize<AppSettings>(
                               json,
                               new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                           ?? new AppSettings();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                settings = new AppSettings();
                warning = $"appsettings.json を読み込めませんでした: {exception.Message}";
            }
        }

        if (string.IsNullOrWhiteSpace(settings.LocalOneDriveRoot))
        {
            settings.LocalOneDriveRoot = Environment.GetEnvironmentVariable("OneDriveCommercial")
                ?? Environment.GetEnvironmentVariable("OneDrive")
                ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(settings.TargetPath))
        {
            settings.TargetPath = settings.LocalOneDriveRoot;
        }

        return settings;
    }
}
