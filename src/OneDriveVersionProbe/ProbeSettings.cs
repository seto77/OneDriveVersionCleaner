using System.Text.Json;

namespace OneDriveVersionProbe;

internal sealed class ProbeSettings
{
    public string ClientId { get; set; } = "";

    public string TenantId { get; set; } = "organizations";

    public string LocalOneDriveRoot { get; set; } = "";

    public bool EnableDelete { get; set; }

    public int KeepLatestVersions { get; set; } = 10;

    public int KeepDays { get; set; } = 30;

    public int ProtectRecentlyModifiedFileDays { get; set; } = 7;

    public long MinimumHistorySizeMB { get; set; } = 10;

    public int MinimumPreviousVersionCount { get; set; } = 1;

    public int MaximumDeletesPerRun { get; set; } = 100;

    public int MaxConcurrency { get; set; } = 3;

    public int RetryCount { get; set; } = 5;

    public string ExportDirectory { get; set; } = "%LOCALAPPDATA%\\OneDriveVersionCleaner\\Reports";

    public static async Task<ProbeSettings> LoadAsync(string? path, CancellationToken cancellationToken)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(path)
            ? Path.Combine(Environment.CurrentDirectory, "appsettings.json")
            : Path.GetFullPath(path);

        if (!File.Exists(resolvedPath))
        {
            return new ProbeSettings();
        }

        await using var stream = File.OpenRead(resolvedPath);
        return await JsonSerializer.DeserializeAsync<ProbeSettings>(
                   stream,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                   cancellationToken)
               ?? new ProbeSettings();
    }

    public string GetExpandedExportDirectory() =>
        Path.GetFullPath(Environment.ExpandEnvironmentVariables(ExportDirectory));
}
