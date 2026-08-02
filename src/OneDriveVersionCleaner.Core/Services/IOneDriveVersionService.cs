using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Services;

/// <summary>
/// Hides Microsoft Graph/SharePoint-specific access behind a testable boundary.
/// Implementations must never expose a file-body delete operation.
/// </summary>
public interface IOneDriveVersionService
{
    Task<AccountInfo> SignInAsync(CancellationToken cancellationToken = default);

    Task<DriveInfo> GetDriveAsync(CancellationToken cancellationToken = default);

    Task<DriveItemReference?> ResolveItemAsync(
        string driveId,
        string cloudRelativePath,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<FileEntry> EnumerateFilesAsync(
        string driveId,
        string rootItemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VersionEntry>> GetVersionsAsync(
        string driveId,
        string itemId,
        CancellationToken cancellationToken = default);

    Task<DeleteResult> DeleteVersionAsync(
        string driveId,
        string itemId,
        string versionId,
        CancellationToken cancellationToken = default);
}
