using System.Text.Json;
using OneDriveVersionCleaner.Core.Aggregation;
using OneDriveVersionCleaner.Core.Export;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class ScanResultExporterTests
{
    private readonly ScanAggregator _aggregator = new();
    private readonly ScanResultExporter _sut = new();

    [Fact]
    public void ToCsv_EmptyResultContainsOnlyHeader()
    {
        var csv = _sut.ToCsv(_aggregator.Aggregate([]));

        Assert.StartsWith("DriveId,ItemId,FileName", csv);
        Assert.Single(csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void ToCsv_QuotesCommasQuotesAndNewlinesAndPreservesUnicode()
    {
        var file = TestData.File() with
        {
            Name = "研究,\"結果\"\r\n最終.xlsx",
            CloudPath = "研究/結果.xlsx",
        };

        var csv = _sut.ToCsv(_aggregator.Aggregate([file]));

        Assert.Contains("\"研究,\"\"結果\"\"\r\n最終.xlsx\"", csv);
        Assert.Contains("研究/結果.xlsx", csv);
    }

    [Fact]
    public void ToCsv_ProtectsSpreadsheetFormulaCellsByDefault()
    {
        var file = TestData.File() with { Name = "=HYPERLINK(\"bad\")" };

        var csv = _sut.ToCsv(_aggregator.Aggregate([file]));

        Assert.Contains("\"'=HYPERLINK(\"\"bad\"\")\"", csv);
    }

    [Fact]
    public void ToCsv_LeavesUnknownSizeColumnEmptyAndExportsUnknownCount()
    {
        var file = TestData.File(TestData.Version("unknown", 10, size: null));

        var csv = _sut.ToCsv(_aggregator.Aggregate([file]));
        var dataRow = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)[1];

        Assert.Contains(",1,0,1,false,", dataRow);
    }

    [Fact]
    public void ToJson_ProducesVersionedDocumentAndJsonNullForUnknownTotals()
    {
        var generatedAt = new DateTimeOffset(2026, 8, 1, 1, 2, 3, TimeSpan.Zero);
        var file = TestData.File(TestData.Version("unknown", 10, size: null));

        var json = _sut.ToJson(_aggregator.Aggregate([file]), generatedAt);
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        Assert.Equal(ScanResultExporter.CurrentSchemaVersion, root.GetProperty("schemaVersion").GetString());
        Assert.Equal(generatedAt, root.GetProperty("generatedAtUtc").GetDateTimeOffset());
        var exportedFile = root.GetProperty("scan").GetProperty("files")[0];
        Assert.Equal(JsonValueKind.Null, exportedFile.GetProperty("previousVersionTotalBytes").ValueKind);
        Assert.Equal(1, exportedFile.GetProperty("unknownPreviousVersionSizeCount").GetInt32());
    }
}
