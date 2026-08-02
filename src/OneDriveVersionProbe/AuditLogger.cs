using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneDriveVersionProbe;

internal sealed class AuditLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _logPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AuditLogger()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneDriveVersionCleaner",
            "Logs");
        Directory.CreateDirectory(directory);
        _logPath = Path.Combine(directory, $"probe-{DateTimeOffset.Now:yyyyMMdd}.jsonl");
    }

    public async Task WriteAsync(
        string eventName,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            eventName,
            details,
        };

        var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(
                _logPath,
                line,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}
