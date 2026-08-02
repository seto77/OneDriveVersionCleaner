using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionProbe;

internal static class SampleDataFactory
{
    public static IReadOnlyList<FileEntry> Create(DateTimeOffset now)
    {
        const long mebibyte = 1024L * 1024;

        var archiveVersions = new List<VersionEntry>
        {
            new()
            {
                VersionId = "current",
                Label = "current",
                Created = now.AddDays(-20),
                ModifiedBy = "Demo User",
                Size = 120 * mebibyte,
                IsCurrent = true,
                CanDelete = false,
            },
        };

        for (var index = 1; index <= 18; index++)
        {
            archiveVersions.Add(new VersionEntry
            {
                VersionId = $"{19 - index}.0",
                Label = $"{19 - index}.0",
                Created = now.AddDays(-(20 + (index * 7))),
                ModifiedBy = "Demo User",
                Size = (120 - index) * mebibyte,
                IsCurrent = false,
                CanDelete = null,
            });
        }

        return
        [
            new FileEntry
            {
                DriveId = "demo-drive",
                ItemId = "demo-archive",
                Name = "研究データ.zip",
                CloudPath = "Research/研究データ.zip",
                LocalPath = @"C:\DemoOneDrive\Research\研究データ.zip",
                WebUrl = "https://example.invalid/Research/demo",
                CurrentSize = 120 * mebibyte,
                LastModified = now.AddDays(-20),
                ModifiedBy = "Demo User",
                FileType = "application/zip",
                Versions = archiveVersions,
            },
            new FileEntry
            {
                DriveId = "demo-drive",
                ItemId = "demo-recent",
                Name = "最近更新.xlsx",
                CloudPath = "Research/最近更新.xlsx",
                LocalPath = @"C:\DemoOneDrive\Research\最近更新.xlsx",
                CurrentSize = 20 * mebibyte,
                LastModified = now.AddDays(-1),
                ModifiedBy = "Demo User",
                FileType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Versions =
                [
                    new VersionEntry
                    {
                        VersionId = "current",
                        Created = now.AddDays(-1),
                        Size = 20 * mebibyte,
                        IsCurrent = true,
                        CanDelete = false,
                    },
                    new VersionEntry
                    {
                        VersionId = "2.0",
                        Created = now.AddDays(-40),
                        Size = 18 * mebibyte,
                    },
                    new VersionEntry
                    {
                        VersionId = "1.0",
                        Created = now.AddDays(-80),
                        Size = 17 * mebibyte,
                    },
                ],
            },
            new FileEntry
            {
                DriveId = "demo-drive",
                ItemId = "demo-unknown",
                Name = "サイズ不明.txt",
                CloudPath = "Notes/サイズ不明.txt",
                LocalPath = @"C:\DemoOneDrive\Notes\サイズ不明.txt",
                CurrentSize = 1024,
                LastModified = now.AddDays(-90),
                FileType = "text/plain",
                RetrievalStatus = RetrievalStatus.Warning,
                Versions =
                [
                    new VersionEntry
                    {
                        VersionId = "current",
                        Created = now.AddDays(-90),
                        Size = 1024,
                        IsCurrent = true,
                        CanDelete = false,
                    },
                    new VersionEntry
                    {
                        VersionId = "unknown",
                        Created = null,
                        Size = null,
                        RetrievalStatus = RetrievalStatus.Warning,
                    },
                ],
            },
        ];
    }
}
