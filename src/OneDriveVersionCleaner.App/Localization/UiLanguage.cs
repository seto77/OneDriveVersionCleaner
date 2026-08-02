using System.Globalization;
using System.Text.Json;
using OneDriveVersionCleaner.App.Localization;

namespace OneDriveVersionCleaner.App.Localization;

/// <summary>
/// Decides which language the interface starts in, applies it to the thread, and remembers the
/// user's choice for the next run.
/// </summary>
/// <remarks>
/// The preference is kept next to the log and cache directories rather than in
/// <c>appsettings.json</c>: that file sits beside the executable and is read-only as far as this
/// application is concerned, and a published single-file build may well be unpacked somewhere
/// the user cannot write to.
/// </remarks>
internal static class UiLanguage
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    private sealed record StoredPreference(string Language);

    private static string PreferencePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OneDriveVersionCleaner",
        "ui-language.json");

    /// <summary>
    /// Applies the language this run should use: the one the user last chose, otherwise the one
    /// the settings file names, otherwise the one Windows is displayed in, otherwise English.
    /// </summary>
    public static UiCulture Initialize(string? configuredLanguage)
    {
        var culture = SupportedCultures.Resolve(
            LoadSavedLanguage()
            ?? (string.IsNullOrWhiteSpace(configuredLanguage)
                ? CultureInfo.CurrentUICulture.Name
                : configuredLanguage));
        Apply(culture);
        return culture;
    }

    /// <summary>
    /// Switches the interface language of this thread and of every thread started afterwards.
    /// Only the UI culture moves: numbers and dates keep the format the user's Windows uses,
    /// which is what they read everywhere else on the machine.
    /// </summary>
    public static void Apply(UiCulture culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var cultureInfo = CultureInfo.GetCultureInfo(culture.Name);
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
    }

    /// <summary>
    /// Records the choice for the next run. Failing to write it only costs the preference, so
    /// it never interrupts the language change the user just asked for.
    /// </summary>
    public static void Save(UiCulture culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        try
        {
            var path = PreferencePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(
                path,
                JsonSerializer.Serialize(new StoredPreference(culture.Name), JsonOptions));
        }
        catch (Exception exception) when (exception is IOException or
            UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            // The language is already applied; only remembering it failed.
        }
    }

    private static string? LoadSavedLanguage()
    {
        try
        {
            var path = PreferencePath;
            if (!File.Exists(path))
            {
                return null;
            }

            var stored = JsonSerializer.Deserialize<StoredPreference>(
                File.ReadAllText(path),
                JsonOptions);
            return string.IsNullOrWhiteSpace(stored?.Language) ? null : stored.Language;
        }
        catch (Exception exception) when (exception is IOException or
            UnauthorizedAccessException or JsonException or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }
}
