using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace OneDriveVersionCleaner.Infrastructure.Authentication;

public sealed class MsalTokenProvider
{
    private const string CacheFileName = "msal-cache.bin3";

    private readonly GraphAuthenticationOptions _options;
    private readonly IPublicClientApplication _application;
    private readonly string[] _scopes;
    private readonly SemaphoreSlim _authenticationGate = new(1, 1);
    private readonly Lazy<Task<MsalCacheHelper>> _cacheInitialization;

    public MsalTokenProvider(GraphAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            throw new ArgumentException("Microsoft Entra application Client ID is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.TenantId))
        {
            throw new ArgumentException("Tenant ID must not be empty.", nameof(options));
        }

        _options = options;
        _scopes = options.AllowVersionDelete
            ? ["User.Read", "Files.ReadWrite"]
            : ["User.Read", "Files.Read"];

        var tenant = Uri.EscapeDataString(options.TenantId.Trim());
        _application = PublicClientApplicationBuilder
            .Create(options.ClientId.Trim())
            .WithAuthority($"https://login.microsoftonline.com/{tenant}")
            .WithDefaultRedirectUri()
            .Build();

        _cacheInitialization = new Lazy<Task<MsalCacheHelper>>(InitializeCacheAsync);
    }

    internal async Task<AuthenticationResult> AcquireTokenAsync(
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        await _cacheInitialization.Value.ConfigureAwait(false);
        await _authenticationGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var accounts = await _application.GetAccountsAsync().ConfigureAwait(false);
            var account = accounts.FirstOrDefault();

            if (account is not null)
            {
                try
                {
                    return await _application
                        .AcquireTokenSilent(_scopes, account)
                        .WithForceRefresh(forceRefresh)
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (MsalUiRequiredException)
                {
                    // The cached account cannot satisfy the request; continue interactively.
                }
            }

            return await _application
                .AcquireTokenWithDeviceCode(_scopes, NotifyDeviceCodeAsync)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _authenticationGate.Release();
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        await _cacheInitialization.Value.ConfigureAwait(false);
        await _authenticationGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var accounts = await _application.GetAccountsAsync().ConfigureAwait(false);
            foreach (var account in accounts)
            {
                await _application.RemoveAsync(account).ConfigureAwait(false);
            }
        }
        finally
        {
            _authenticationGate.Release();
        }
    }

    private async Task<MsalCacheHelper> InitializeCacheAsync()
    {
        var cacheDirectory = Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(_options.TokenCacheDirectory));
        Directory.CreateDirectory(cacheDirectory);

        var storageProperties = new StorageCreationPropertiesBuilder(CacheFileName, cacheDirectory)
            .Build();
        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties).ConfigureAwait(false);
        cacheHelper.RegisterCache(_application.UserTokenCache);
        return cacheHelper;
    }

    private Task NotifyDeviceCodeAsync(DeviceCodeResult result)
    {
        var prompt = new DeviceCodePrompt(
            result.Message,
            result.UserCode,
            result.VerificationUrl,
            result.ExpiresOn);

        return _options.DeviceCodePromptHandler?.Invoke(prompt) ?? Task.CompletedTask;
    }
}
