namespace OneDriveVersionCleaner.Infrastructure.OneDriveAccounts;

/// <summary>
/// Reads OneDrive account settings. The abstraction keeps registry access testable.
/// </summary>
public interface IOneDriveAccountRegistryReader
{
    IReadOnlyList<OneDriveAccountRegistrySnapshot> ReadAccounts();
}
