using System.Diagnostics;
using System.Text.Json;
using OneDriveVersionCleaner.Core.Aggregation;
using OneDriveVersionCleaner.Core.Export;
using OneDriveVersionCleaner.Core.Formatting;
using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Paths;
using OneDriveVersionCleaner.Core.Retention;
using OneDriveVersionCleaner.Infrastructure.Authentication;
using OneDriveVersionCleaner.Infrastructure.Graph;

namespace OneDriveVersionProbe;

internal static class Program
{
    private const long Mebibyte = 1024L * 1024;

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
            Console.Error.WriteLine("停止要求を受け付けました。進行中の要求の完了を待ちます...");
        };

        AuditLogger? logger = null;
        CommandLine? commandLine = null;

        try
        {
            commandLine = CommandLine.Parse(args);
            var settings = await ProbeSettings.LoadAsync(
                commandLine.GetOptionalValue("config"),
                cancellation.Token).ConfigureAwait(false);
            ApplyOverrides(settings, commandLine);

            logger = new AuditLogger();
            await logger.WriteAsync("app_started", new { commandLine.Command }, cancellation.Token)
                .ConfigureAwait(false);

            var exitCode = commandLine.Command switch
            {
                "demo" => await RunDemoAsync(settings, commandLine, cancellation.Token).ConfigureAwait(false),
                "scan" => await RunScanAsync(settings, commandLine, logger, cancellation.Token).ConfigureAwait(false),
                "versions" => await RunVersionsAsync(settings, commandLine, cancellation.Token).ConfigureAwait(false),
                "delete-version" => await RunDeleteVersionAsync(
                        settings,
                        commandLine,
                        logger,
                        cancellation.Token)
                    .ConfigureAwait(false),
                "sign-out" => await RunSignOutAsync(settings, commandLine, cancellation.Token).ConfigureAwait(false),
                "help" or "--help" or "-h" => ShowHelp(),
                _ => throw new ArgumentException($"不明なコマンドです: {commandLine.Command}"),
            };

            await logger.WriteAsync("app_finished", new { commandLine.Command, exitCode }, CancellationToken.None)
                .ConfigureAwait(false);
            return exitCode;
        }
        catch (OperationCanceledException)
        {
            if (logger is not null)
            {
                await logger.WriteAsync("operation_cancelled", cancellationToken: CancellationToken.None)
                    .ConfigureAwait(false);
            }

            Console.Error.WriteLine("処理を停止しました。");
            return 130;
        }
        catch (Exception exception)
        {
            if (logger is not null)
            {
                await logger.WriteAsync(
                    "unhandled_error",
                    new { type = exception.GetType().Name, exception.Message },
                    CancellationToken.None).ConfigureAwait(false);
            }

            Console.Error.WriteLine($"エラー: {exception.Message}");
            if (commandLine?.Has("verbose") == true)
            {
                Console.Error.WriteLine(exception);
            }

            return 1;
        }
    }

    private static async Task<int> RunDemoAsync(
        ProbeSettings settings,
        CommandLine commandLine,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var files = SampleDataFactory.Create(now);
        var evaluation = new RetentionPolicyEvaluator().Evaluate(files, CreatePolicy(settings), now);
        var aggregate = new ScanAggregator().Aggregate(files, evaluation, TimeSpan.Zero);

        PrintSummary(aggregate.Summary, dryRun: true);
        PrintTopFiles(aggregate, maximumRows: 10);
        var paths = await ExportAsync(
            aggregate,
            commandLine.GetOptionalValue("output", settings.GetExpandedExportDirectory())!,
            "demo",
            cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"CSV : {paths.CsvPath}");
        Console.WriteLine($"JSON: {paths.JsonPath}");
        Console.WriteLine("デモではネットワーク接続も削除 API も呼び出していません。");
        return 0;
    }

    private static async Task<int> RunScanAsync(
        ProbeSettings settings,
        CommandLine commandLine,
        AuditLogger logger,
        CancellationToken cancellationToken)
    {
        var mapping = GetTargetMapping(settings, commandLine);
        EnsureClientId(settings.ClientId);

        using var service = CreateService(settings, commandLine, allowVersionDelete: false);
        Console.WriteLine("Microsoft 365 にサインインします（読み取り専用権限）。");
        var account = await service.SignInAsync(cancellationToken).ConfigureAwait(false);
        var drive = await service.GetDriveAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"アカウント: {account.DisplayName} <{account.UserPrincipalName}>");
        Console.WriteLine($"Drive: {drive.DriveType} / {drive.DriveId}");

        var rootItem = await service.ResolveItemAsync(
                drive.DriveId,
                mapping.CloudRelativePath,
                cancellationToken)
            .ConfigureAwait(false);
        if (rootItem is null)
        {
            throw new InvalidOperationException(
                $"クラウド上の対象が見つかりません: {mapping.CloudRelativePath}");
        }

        await logger.WriteAsync(
            "scan_started",
            new
            {
                account.UserId,
                drive.DriveId,
                targetLocalPath = mapping.LocalPath,
                mapping.CloudRelativePath,
            },
            cancellationToken).ConfigureAwait(false);

        var files = new List<FileEntry>();
        var stopwatch = Stopwatch.StartNew();
        await foreach (var file in service.EnumerateFilesAsync(
                           drive.DriveId,
                           rootItem.ItemId,
                           cancellationToken))
        {
            FileEntry scanned;
            try
            {
                var versions = await service.GetVersionsAsync(
                        file.DriveId,
                        file.ItemId,
                        cancellationToken)
                    .ConfigureAwait(false);
                scanned = file with { Versions = versions };
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            catch (GraphRequestException exception)
            {
                scanned = file with
                {
                    RetrievalStatus = MapRetrievalStatus(exception),
                    Error = exception.Message,
                };
            }
            catch (Exception exception) when (IsExpectedFileRetrievalFailure(exception))
            {
                scanned = file with
                {
                    RetrievalStatus = RetrievalStatus.Error,
                    Error = exception.Message,
                };
            }

            files.Add(scanned);
            Console.Write($"\r取得済み: {files.Count:N0} ファイル / エラー: {files.Count(item => IsError(item.RetrievalStatus)):N0}");
        }

        stopwatch.Stop();
        Console.WriteLine();

        var policy = CreatePolicy(settings);
        var evaluation = new RetentionPolicyEvaluator().Evaluate(files, policy, DateTimeOffset.UtcNow);
        var aggregate = new ScanAggregator().Aggregate(files, evaluation, stopwatch.Elapsed);
        PrintSummary(aggregate.Summary, dryRun: true);
        PrintTopFiles(aggregate, maximumRows: 20);

        var outputDirectory = commandLine.GetOptionalValue(
            "output",
            settings.GetExpandedExportDirectory())!;
        var paths = await ExportAsync(aggregate, outputDirectory, "scan", cancellationToken)
            .ConfigureAwait(false);

        await logger.WriteAsync(
            "scan_finished",
            new
            {
                aggregate.Summary.TargetFileCount,
                aggregate.Summary.PreviousVersionCount,
                aggregate.Summary.KnownPreviousVersionBytes,
                aggregate.Summary.DeleteCandidateCount,
                aggregate.Summary.KnownReclaimableBytes,
                aggregate.Summary.ErrorCount,
                csv = paths.CsvPath,
                json = paths.JsonPath,
            },
            cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"CSV : {paths.CsvPath}");
        Console.WriteLine($"JSON: {paths.JsonPath}");
        Console.WriteLine("Dry Run のみです。削除 API は呼び出していません。");
        return aggregate.Summary.ErrorCount == 0 ? 0 : 2;
    }

    private static async Task<int> RunVersionsAsync(
        ProbeSettings settings,
        CommandLine commandLine,
        CancellationToken cancellationToken)
    {
        EnsureClientId(settings.ClientId);
        var cloudPath = GetCloudPath(settings, commandLine);
        using var service = CreateService(settings, commandLine, allowVersionDelete: false);
        await service.SignInAsync(cancellationToken).ConfigureAwait(false);
        var drive = await service.GetDriveAsync(cancellationToken).ConfigureAwait(false);
        var file = await LoadSingleFileAsync(service, drive.DriveId, cloudPath, cancellationToken)
            .ConfigureAwait(false);

        var evaluation = new RetentionPolicyEvaluator().Evaluate(
            file,
            CreatePolicy(settings),
            DateTimeOffset.UtcNow);
        var decisions = evaluation.Decisions
            .GroupBy(decision => decision.Version.VersionId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        Console.WriteLine($"ファイル: {file.CloudPath}");
        ConsoleOutput.WriteVersions(file.Versions, decisions);
        Console.WriteLine($"削除候補: {evaluation.DeleteCandidates.Count:N0} 件 / " +
                          $"削減見込み: {ByteSizeFormatter.Format(evaluation.KnownReclaimableBytes)}");
        Console.WriteLine("表示は Dry Run です。削除 API は呼び出していません。");
        return 0;
    }

    private static async Task<int> RunDeleteVersionAsync(
        ProbeSettings settings,
        CommandLine commandLine,
        AuditLogger logger,
        CancellationToken cancellationToken)
    {
        EnsureClientId(settings.ClientId);
        if (!settings.EnableDelete)
        {
            throw new InvalidOperationException(
                "設定ファイルの EnableDelete が false です。専用テストファイルでのみ true にしてください。");
        }

        if (!commandLine.Has("experimental-graph-delete"))
        {
            throw new InvalidOperationException(
                "Graph のバージョン削除は実験機能です。実行には --experimental-graph-delete が必要です。");
        }

        if (!string.Equals(commandLine.GetOptionalValue("confirm"), "DELETE", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("実行には --confirm DELETE が必要です。");
        }

        var cloudPath = GetCloudPath(settings, commandLine);
        var versionId = commandLine.GetRequiredValue("version-id");
        using var service = CreateService(settings, commandLine, allowVersionDelete: true);
        var account = await service.SignInAsync(cancellationToken).ConfigureAwait(false);
        var drive = await service.GetDriveAsync(cancellationToken).ConfigureAwait(false);
        var file = await LoadSingleFileAsync(service, drive.DriveId, cloudPath, cancellationToken)
            .ConfigureAwait(false);

        var evaluation = new RetentionPolicyEvaluator().Evaluate(
            file,
            CreatePolicy(settings),
            DateTimeOffset.UtcNow);
        var targetDecision = evaluation.Decisions.FirstOrDefault(decision =>
            string.Equals(decision.Version.VersionId, versionId, StringComparison.Ordinal));

        if (targetDecision is null)
        {
            throw new InvalidOperationException("指定されたバージョンは現在の一覧にありません。");
        }

        if (!targetDecision.IsDeleteCandidate)
        {
            var reasons = string.Join(", ", targetDecision.Reasons);
            throw new InvalidOperationException($"保持条件により削除できません: {reasons}");
        }

        Console.Error.WriteLine("警告: この Graph DELETE は公式 API リファレンス未掲載の実験機能です。");
        Console.Error.WriteLine($"対象: {file.CloudPath} / version {versionId} / " +
                                $"{ByteSizeFormatter.Format(targetDecision.Version.Size)}");

        await logger.WriteAsync(
            "delete_requested",
            new
            {
                account.UserId,
                drive.DriveId,
                file.ItemId,
                file.CloudPath,
                versionId,
                targetDecision.Version.Created,
                targetDecision.Version.Size,
            },
            cancellationToken).ConfigureAwait(false);

        DeleteResult? result = null;
        Exception? deleteException = null;
        var verifiedMissing = false;
        string? verificationError = null;
        try
        {
            try
            {
                result = await service.DeleteVersionAsync(
                        drive.DriveId,
                        file.ItemId,
                        versionId,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                deleteException = exception;
                throw;
            }

            if (result.IsSuccess)
            {
                try
                {
                    var refreshed = await service.GetVersionsAsync(
                            drive.DriveId,
                            file.ItemId,
                            cancellationToken)
                        .ConfigureAwait(false);
                    verifiedMissing = refreshed.All(version =>
                        !string.Equals(version.VersionId, versionId, StringComparison.Ordinal));
                    if (!verifiedMissing)
                    {
                        verificationError = "The deleted version is still present in the refreshed version list.";
                    }
                }
                catch (Exception exception)
                {
                    verificationError = $"{exception.GetType().Name}: {exception.Message}";
                    throw;
                }
            }
        }
        finally
        {
            await logger.WriteAsync(
                "delete_finished",
                new
                {
                    account.UserId,
                    drive.DriveId,
                    file.ItemId,
                    file.CloudPath,
                    versionId,
                    status = result?.Status.ToString() ?? "Exception",
                    error = result?.Error ?? deleteException?.Message,
                    deleteExceptionType = deleteException?.GetType().Name,
                    verifiedMissing,
                    verificationError,
                },
                CancellationToken.None).ConfigureAwait(false);
        }

        var completedResult = result ??
            throw new InvalidOperationException("Version deletion completed without a result.");

        Console.WriteLine($"結果: {completedResult.Status}");
        if (!string.IsNullOrWhiteSpace(completedResult.Error))
        {
            Console.WriteLine(completedResult.Error);
        }

        Console.WriteLine($"再取得検証: {(verifiedMissing ? "削除済み" : "未確認または残存")}");
        if (!string.IsNullOrWhiteSpace(verificationError))
        {
            Console.WriteLine($"検証エラー: {verificationError}");
        }

        return completedResult.IsSuccess && verifiedMissing ? 0 : 3;
    }

    private static async Task<int> RunSignOutAsync(
        ProbeSettings settings,
        CommandLine commandLine,
        CancellationToken cancellationToken)
    {
        EnsureClientId(settings.ClientId);
        using var service = CreateService(settings, commandLine, allowVersionDelete: false);
        await service.SignOutAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine("ローカルの認証キャッシュからアカウントを削除しました。");
        return 0;
    }

    private static async Task<FileEntry> LoadSingleFileAsync(
        GraphOneDriveVersionService service,
        string driveId,
        string cloudPath,
        CancellationToken cancellationToken)
    {
        var item = await service.ResolveItemAsync(driveId, cloudPath, cancellationToken).ConfigureAwait(false)
                   ?? throw new InvalidOperationException($"クラウド上のファイルが見つかりません: {cloudPath}");
        if (item.IsFolder)
        {
            throw new InvalidOperationException("versions/delete-version にはファイルを指定してください。");
        }

        FileEntry? file = null;
        await foreach (var entry in service.EnumerateFilesAsync(driveId, item.ItemId, cancellationToken))
        {
            file = entry;
            break;
        }

        file ??= new FileEntry
        {
            DriveId = driveId,
            ItemId = item.ItemId,
            Name = item.Name,
            CloudPath = item.CloudPath,
            WebUrl = item.WebUrl,
        };

        var versions = await service.GetVersionsAsync(driveId, item.ItemId, cancellationToken)
            .ConfigureAwait(false);
        return file with { Versions = versions };
    }

    private static GraphOneDriveVersionService CreateService(
        ProbeSettings settings,
        CommandLine commandLine,
        bool allowVersionDelete)
    {
        var authentication = new GraphAuthenticationOptions
        {
            ClientId = settings.ClientId,
            TenantId = settings.TenantId,
            AllowVersionDelete = allowVersionDelete,
            DeviceCodePromptHandler = prompt =>
            {
                Console.WriteLine(prompt.Message);
                if (!commandLine.Has("no-browser"))
                {
                    TryOpenBrowser(prompt.VerificationUrl);
                }

                return Task.CompletedTask;
            },
        };

        return new GraphOneDriveVersionService(new GraphOneDriveOptions
        {
            Authentication = authentication,
            LocalOneDriveRoot = settings.LocalOneDriveRoot,
            RetryCount = settings.RetryCount,
        });
    }

    private static RetentionPolicy CreatePolicy(ProbeSettings settings) => new()
    {
        KeepLatestVersions = settings.KeepLatestVersions,
        KeepDays = settings.KeepDays,
        ProtectRecentlyModifiedFileDays = settings.ProtectRecentlyModifiedFileDays,
        MinimumHistoryBytes = checked(settings.MinimumHistorySizeMB * Mebibyte),
        MinimumPreviousVersionCount = settings.MinimumPreviousVersionCount,
        MaximumDeletesPerRun = settings.MaximumDeletesPerRun,
    };

    private static LocalPathMapping GetTargetMapping(ProbeSettings settings, CommandLine commandLine)
    {
        var root = commandLine.GetOptionalValue("root", settings.LocalOneDriveRoot);
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("--root または LocalOneDriveRoot を指定してください。");
        }

        var path = commandLine.GetOptionalValue("path", root)!;
        return LocalPathMapper.Map(root, path);
    }

    private static string GetCloudPath(ProbeSettings settings, CommandLine commandLine)
    {
        var explicitCloudPath = commandLine.GetOptionalValue("cloud-path");
        if (explicitCloudPath is not null)
        {
            return string.Join('/', LocalPathMapper.SplitCloudRelativePath(explicitCloudPath));
        }

        return GetTargetMapping(settings, commandLine).CloudRelativePath;
    }

    private static void ApplyOverrides(ProbeSettings settings, CommandLine commandLine)
    {
        settings.ClientId = commandLine.GetOptionalValue("client-id", settings.ClientId) ?? "";
        settings.TenantId = commandLine.GetOptionalValue("tenant", settings.TenantId) ?? "organizations";
        settings.LocalOneDriveRoot = commandLine.GetOptionalValue("root", settings.LocalOneDriveRoot) ?? "";
        settings.KeepLatestVersions = commandLine.GetInt32(
            "keep-versions",
            settings.KeepLatestVersions);
        settings.KeepDays = commandLine.GetInt32("keep-days", settings.KeepDays);
        settings.ProtectRecentlyModifiedFileDays = commandLine.GetInt32(
            "protect-days",
            settings.ProtectRecentlyModifiedFileDays);
        settings.MinimumHistorySizeMB = commandLine.GetInt64(
            "min-history-mb",
            settings.MinimumHistorySizeMB);
        settings.MinimumPreviousVersionCount = commandLine.GetInt32(
            "min-version-count",
            settings.MinimumPreviousVersionCount);
        settings.MaximumDeletesPerRun = commandLine.GetInt32(
            "max-delete",
            settings.MaximumDeletesPerRun);
        settings.RetryCount = commandLine.GetInt32("retry-count", settings.RetryCount);
    }

    private static async Task<(string CsvPath, string JsonPath)> ExportAsync(
        ScanAggregate aggregate,
        string directory,
        string prefix,
        CancellationToken cancellationToken)
    {
        var outputDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory));
        Directory.CreateDirectory(outputDirectory);
        var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
        var csvPath = Path.Combine(outputDirectory, $"{prefix}-{stamp}.csv");
        var jsonPath = Path.Combine(outputDirectory, $"{prefix}-{stamp}.json");
        var exporter = new ScanResultExporter();
        await exporter.WriteCsvAsync(csvPath, aggregate, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await exporter.WriteJsonAsync(jsonPath, aggregate, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return (csvPath, jsonPath);
    }

    private static void PrintSummary(ScanSummary summary, bool dryRun)
    {
        Console.WriteLine(dryRun ? "=== Dry Run 集計 ===" : "=== 集計 ===");
        Console.WriteLine($"対象ファイル       : {summary.TargetFileCount:N0}");
        Console.WriteLine($"履歴ありファイル   : {summary.FilesWithPreviousVersions:N0}");
        Console.WriteLine($"過去バージョン     : {summary.PreviousVersionCount:N0}");
        Console.WriteLine($"既知の履歴容量     : {ByteSizeFormatter.Format(summary.KnownPreviousVersionBytes)}" +
                          FormatUnknown(summary.UnknownPreviousVersionSizeCount));
        Console.WriteLine($"削除候補           : {summary.DeleteCandidateCount:N0}");
        Console.WriteLine($"既知の削減見込み   : {ByteSizeFormatter.Format(summary.KnownReclaimableBytes)}" +
                          FormatUnknown(summary.UnknownDeleteCandidateSizeCount));
        Console.WriteLine($"エラー             : {summary.ErrorCount:N0}");
        if (summary.ScanDuration.HasValue)
        {
            Console.WriteLine($"所要時間           : {summary.ScanDuration.Value:g}");
        }

        Console.WriteLine("※ 容量は API が返す版サイズの推定合計で、管理画面の使用量とは一致しない場合があります。");
    }

    private static void PrintTopFiles(ScanAggregate aggregate, int maximumRows)
    {
        Console.WriteLine();
        Console.WriteLine("履歴容量上位:");
        foreach (var file in aggregate.Files
                     .OrderByDescending(item => item.KnownPreviousVersionBytes)
                     .ThenBy(item => item.File.CloudPath, StringComparer.OrdinalIgnoreCase)
                     .Take(maximumRows))
        {
            Console.WriteLine(
                $"  {ByteSizeFormatter.Format(file.KnownPreviousVersionBytes),10}  " +
                $"{file.PreviousVersionCount,5} versions  " +
                $"{file.DeleteCandidateCount,4} candidates  {file.File.CloudPath}");
        }

        Console.WriteLine();
    }

    private static RetrievalStatus MapRetrievalStatus(GraphRequestException exception) =>
        exception.StatusCode switch
        {
            System.Net.HttpStatusCode.Forbidden => RetrievalStatus.AccessDenied,
            System.Net.HttpStatusCode.NotFound => RetrievalStatus.NotFound,
            _ => RetrievalStatus.Error,
        };

    private static bool IsExpectedFileRetrievalFailure(Exception exception) =>
        exception is HttpRequestException or IOException or JsonException or
            TimeoutException or OperationCanceledException;

    private static bool IsError(RetrievalStatus status) => status is
        RetrievalStatus.AccessDenied or RetrievalStatus.NotFound or RetrievalStatus.Error;

    private static string FormatUnknown(int unknownCount) =>
        unknownCount == 0 ? "" : $"（サイズ不明 {unknownCount:N0} 件を除外）";

    private static void EnsureClientId(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId) ||
            string.Equals(clientId, "00000000-0000-0000-0000-000000000000", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Entra 公開クライアントの ClientId を appsettings.json または --client-id で指定してください。");
        }
    }

    private static void TryOpenBrowser(string verificationUrl)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = verificationUrl,
                UseShellExecute = true,
            });
        }
        catch
        {
            // The device-code message still contains a URL that the user can open manually.
        }
    }

    private static int ShowHelp()
    {
        Console.WriteLine(
            """
            OneDriveVersionProbe - OneDrive バージョン履歴整理ツール Phase 0

            すぐ試す（クラウド接続なし）:
              OneDriveVersionProbe demo [--output DIR]

            分析（常に Dry Run）:
              OneDriveVersionProbe scan --config appsettings.json [--path LOCAL_PATH]
              OneDriveVersionProbe versions --config appsettings.json --path LOCAL_FILE
              OneDriveVersionProbe versions --config appsettings.json --cloud-path RELATIVE_PATH

            実験的な旧版1件削除（3つの安全ゲートが必要）:
              1. appsettings.json の EnableDelete を true
              2. --experimental-graph-delete
              3. --confirm DELETE

              OneDriveVersionProbe delete-version --config appsettings.json \
                --path LOCAL_FILE --version-id VERSION_ID \
                --experimental-graph-delete --confirm DELETE

            認証キャッシュを削除:
              OneDriveVersionProbe sign-out --config appsettings.json

            共通オプション:
              --client-id ID       Entra アプリの Client ID（設定より優先）
              --tenant TENANT      Tenant ID/ドメイン（既定 organizations）
              --root PATH          ローカル OneDrive 同期ルート
              --no-browser         Device Code のページを自動で開かない
              --verbose            詳細エラーを表示

            保持条件:
              --keep-versions N    新しい過去版を保持（既定 10）
              --keep-days N        N日以内の版を保持（既定 30）
              --protect-days N     最近更新ファイルの保護（既定 7）
              --min-history-mb N   処理する最小履歴容量（既定 10 MB以上）
              --min-version-count N
              --max-delete N       実行計画の最大候補数（既定 100）

            Graph の版削除は公式 API リファレンス未掲載です。必ず専用テスト
            フォルダーで確認し、本番データに対して最初から有効化しないでください。
            """);
        return 0;
    }
}
