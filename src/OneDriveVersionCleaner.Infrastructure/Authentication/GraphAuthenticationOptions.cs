namespace OneDriveVersionCleaner.Infrastructure.Authentication;

public sealed record GraphAuthenticationOptions
{
    public required string ClientId { get; init; }

    public string TenantId { get; init; } = "organizations";

    public bool AllowVersionDelete { get; init; }

    public string TokenCacheDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OneDriveVersionCleaner",
        "Auth");

    public Func<DeviceCodePrompt, Task>? DeviceCodePromptHandler { get; init; }
}

public sealed record DeviceCodePrompt(
    string Message,
    string UserCode,
    string VerificationUrl,
    DateTimeOffset ExpiresOn);
