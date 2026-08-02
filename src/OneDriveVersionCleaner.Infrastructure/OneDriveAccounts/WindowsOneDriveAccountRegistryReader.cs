using Microsoft.Win32;
using System.Security;

namespace OneDriveVersionCleaner.Infrastructure.OneDriveAccounts;

/// <summary>
/// Reads the current Windows user's OneDrive account configuration without modifying it.
/// </summary>
public sealed class WindowsOneDriveAccountRegistryReader : IOneDriveAccountRegistryReader
{
    internal const string AccountsRegistryPath = @"Software\Microsoft\OneDrive\Accounts";

    public IReadOnlyList<OneDriveAccountRegistrySnapshot> ReadAccounts()
    {
        try
        {
            using var accountsKey = Registry.CurrentUser.OpenSubKey(AccountsRegistryPath, writable: false);
            if (accountsKey is null)
            {
                return [];
            }

            var accounts = new List<OneDriveAccountRegistrySnapshot>();
            foreach (var accountKeyName in accountsKey.GetSubKeyNames())
            {
                if (!OneDriveBusinessAccountDiscovery.IsBusinessAccountKey(accountKeyName))
                {
                    continue;
                }

                TryReadAccount(accountsKey, accountKeyName, accounts);
            }

            return accounts;
        }
        catch (Exception exception) when (IsExpectedRegistryFailure(exception))
        {
            return [];
        }
    }

    private static void TryReadAccount(
        RegistryKey accountsKey,
        string accountKeyName,
        ICollection<OneDriveAccountRegistrySnapshot> destination)
    {
        try
        {
            using var accountKey = accountsKey.OpenSubKey(accountKeyName, writable: false);
            if (accountKey is null)
            {
                return;
            }

            destination.Add(new OneDriveAccountRegistrySnapshot
            {
                AccountKey = accountKeyName,
                DisplayName = ReadString(accountKey, "DisplayName"),
                UserFolder = ReadString(accountKey, "UserFolder"),
                ConfiguredTenantId = ReadString(accountKey, "ConfiguredTenantId"),
                TenantId = ReadString(accountKey, "TenantId"),
                ServiceEndpointUri = ReadString(accountKey, "ServiceEndpointUri"),
            });
        }
        catch (Exception exception) when (IsExpectedRegistryFailure(exception))
        {
            // A damaged or inaccessible account key must not hide other usable accounts.
        }
    }

    private static string? ReadString(RegistryKey key, string valueName) =>
        key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;

    private static bool IsExpectedRegistryFailure(Exception exception) =>
        exception is SecurityException
            or UnauthorizedAccessException
            or IOException
            or PlatformNotSupportedException;
}
