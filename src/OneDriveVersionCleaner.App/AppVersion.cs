using System.Reflection;

namespace OneDriveVersionCleaner.App;

/// <summary>
/// The version this build carries, in the two shapes the application needs it in.
/// </summary>
/// <remarks>
/// Both come from <see cref="AssemblyInformationalVersionAttribute"/> rather than from
/// <c>AssemblyVersion</c>, because the SDK writes "1.0.0+&lt;commit&gt;" there and only the
/// three-part number into the others. A user reporting a problem can read <see cref="Display"/>
/// off the title bar; <see cref="Full"/> goes into the deletion log, where the commit settles
/// which build actually did the deleting — including one somebody built themselves.
/// </remarks>
internal static class AppVersion
{
    /// <summary>The version alone, for the window title.</summary>
    public static string Display { get; } = Informational().Split('+')[0];

    /// <summary>The version with the commit it was built from, for the record.</summary>
    public static string Full { get; } = Informational();

    private static string Informational()
    {
        var assembly = typeof(AppVersion).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(informational)
            ? assembly.GetName().Version?.ToString(3) ?? "unknown"
            : informational;
    }
}
