using System.Globalization;

namespace OneDriveVersionProbe;

internal sealed class CommandLine
{
    private readonly Dictionary<string, string?> _options;

    private CommandLine(string command, Dictionary<string, string?> options)
    {
        Command = command;
        _options = options;
    }

    public string Command { get; }

    public static CommandLine Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new CommandLine("help", new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));
        }

        var command = args[0].Trim().ToLowerInvariant();
        var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 1; index < args.Length; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"オプション名が必要です: {argument}");
            }

            var equalsIndex = argument.IndexOf('=');
            if (equalsIndex > 2)
            {
                options[argument[2..equalsIndex]] = argument[(equalsIndex + 1)..];
                continue;
            }

            var key = argument[2..];
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[key] = args[++index];
            }
            else
            {
                options[key] = null;
            }
        }

        return new CommandLine(command, options);
    }

    public bool Has(string name) => _options.ContainsKey(name);

    public string? Get(string name) => _options.GetValueOrDefault(name);

    public string? GetOptionalValue(string name, string? fallback = null)
    {
        if (!_options.TryGetValue(name, out var value))
        {
            return fallback;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"--{name} には値が必要です。");
        }

        return value;
    }

    public string GetRequiredValue(string name) =>
        GetOptionalValue(name)
        ?? throw new ArgumentException($"--{name} は必須です。");

    public int GetInt32(string name, int fallback, int minimum = 0)
    {
        var text = GetOptionalValue(name);
        if (text is null)
        {
            return fallback;
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < minimum)
        {
            throw new ArgumentException($"--{name} には {minimum} 以上の整数を指定してください。");
        }

        return value;
    }

    public long GetInt64(string name, long fallback, long minimum = 0)
    {
        var text = GetOptionalValue(name);
        if (text is null)
        {
            return fallback;
        }

        if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < minimum)
        {
            throw new ArgumentException($"--{name} には {minimum} 以上の整数を指定してください。");
        }

        return value;
    }
}
