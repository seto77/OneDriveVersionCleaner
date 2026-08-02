using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneDriveVersionCleaner.Core.Aggregation;

namespace OneDriveVersionCleaner.Core.Export;

public sealed record ScanExportDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAtUtc,
    ScanAggregate Scan);

public sealed class ScanResultExporter
{
    public const string CurrentSchemaVersion = "1.0";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public string ToCsv(ScanAggregate aggregate, bool protectAgainstFormulaInjection = true)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var builder = new StringBuilder();
        AppendCsvRow(builder,
        [
            "DriveId",
            "ItemId",
            "FileName",
            "CloudPath",
            "LocalPath",
            "CurrentSizeBytes",
            "PreviousVersionCount",
            "KnownPreviousVersionBytes",
            "UnknownPreviousVersionSizeCount",
            "HistorySizeComplete",
            "DeleteCandidateCount",
            "KnownReclaimableBytes",
            "UnknownDeleteCandidateSizeCount",
            "OldestPreviousVersionCreatedUtc",
            "LatestPreviousVersionCreatedUtc",
            "Status",
            "Error",
        ], protectAgainstFormulaInjection: false);

        foreach (var file in aggregate.Files)
        {
            AppendCsvRow(builder,
            [
                file.File.DriveId,
                file.File.ItemId,
                file.File.Name,
                file.File.CloudPath,
                file.File.LocalPath,
                Invariant(file.CurrentSizeBytes),
                Invariant(file.PreviousVersionCount),
                Invariant(file.KnownPreviousVersionBytes),
                Invariant(file.UnknownPreviousVersionSizeCount),
                file.IsHistorySizeComplete ? "true" : "false",
                Invariant(file.DeleteCandidateCount),
                Invariant(file.KnownReclaimableBytes),
                Invariant(file.UnknownDeleteCandidateSizeCount),
                Invariant(file.OldestPreviousVersionCreated),
                Invariant(file.LatestPreviousVersionCreated),
                file.File.RetrievalStatus.ToString(),
                file.File.Error ?? string.Empty,
            ], protectAgainstFormulaInjection);
        }

        return builder.ToString();
    }

    public string ToJson(
        ScanAggregate aggregate,
        DateTimeOffset? generatedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var generated = (generatedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var document = new ScanExportDocument(CurrentSchemaVersion, generated, aggregate);
        return JsonSerializer.Serialize(document, JsonOptions);
    }

    public async Task WriteCsvAsync(
        string path,
        ScanAggregate aggregate,
        bool protectAgainstFormulaInjection = true,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = PrepareOutputPath(path);
        await File.WriteAllTextAsync(
            normalizedPath,
            ToCsv(aggregate, protectAgainstFormulaInjection),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteJsonAsync(
        string path,
        ScanAggregate aggregate,
        DateTimeOffset? generatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = PrepareOutputPath(path);
        await File.WriteAllTextAsync(
            normalizedPath,
            ToJson(aggregate, generatedAtUtc),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);
    }

    private static string PrepareOutputPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
        var directory = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return normalizedPath;
    }

    private static void AppendCsvRow(
        StringBuilder builder,
        IReadOnlyList<string> values,
        bool protectAgainstFormulaInjection)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(EscapeCsv(values[index], protectAgainstFormulaInjection));
        }

        builder.Append("\r\n");
    }

    private static string EscapeCsv(string value, bool protectAgainstFormulaInjection)
    {
        if (protectAgainstFormulaInjection && IsPotentialFormula(value))
        {
            value = $"'{value}";
        }

        var requiresQuotes = value.Contains(',') ||
            value.Contains('"') ||
            value.Contains('\r') ||
            value.Contains('\n') ||
            value.StartsWith(' ') ||
            value.EndsWith(' ');

        if (!requiresQuotes)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static bool IsPotentialFormula(string value) =>
        value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r';

    private static string Invariant(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Invariant(long? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Invariant(DateTimeOffset? value) =>
        value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
