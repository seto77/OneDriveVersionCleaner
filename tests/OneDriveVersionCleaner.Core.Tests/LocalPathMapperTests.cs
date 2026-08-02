using OneDriveVersionCleaner.Core.Paths;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class LocalPathMapperTests
{
    private const string Root = @"C:\Users\Test\OneDrive - University";

    [Fact]
    public void Map_ConvertsChildPathToSlashSeparatedCloudPath()
    {
        var mapping = LocalPathMapper.Map(
            Root,
            @"C:\Users\Test\OneDrive - University\Research\Data");

        Assert.Equal("Research/Data", mapping.CloudRelativePath);
        Assert.Equal(["Research", "Data"], mapping.CloudPathSegments);
    }

    [Fact]
    public void Map_MapsRootItselfToEmptyCloudPath()
    {
        var mapping = LocalPathMapper.Map($"{Root}\\", Root);

        Assert.Equal(string.Empty, mapping.CloudRelativePath);
        Assert.Equal(string.Empty, mapping.EncodedCloudRelativePath);
        Assert.Empty(mapping.CloudPathSegments);
    }

    [Fact]
    public void Map_AllowsWindowsCaseDifferences()
    {
        var cloudPath = LocalPathMapper.ToCloudRelativePath(
            Root.ToUpperInvariant(),
            @"c:\users\test\onedrive - university\Research\Paper.docx");

        Assert.Equal("Research/Paper.docx", cloudPath);
    }

    [Fact]
    public void Map_RejectsPrefixSiblingOutsideRoot()
    {
        Assert.Throws<ArgumentException>(() => LocalPathMapper.Map(
            Root,
            @"C:\Users\Test\OneDrive - University Backup\Research"));
    }

    [Fact]
    public void Map_RejectsTraversalOutsideRootAfterNormalization()
    {
        Assert.Throws<ArgumentException>(() => LocalPathMapper.Map(
            Root,
            $@"{Root}\..\Secret"));
    }

    [Fact]
    public void Map_RejectsRelativePath()
    {
        Assert.Throws<ArgumentException>(() => LocalPathMapper.Map(Root, @"Research\Data"));
    }

    [Fact]
    public void EncodeCloudRelativePath_EncodesEachSegmentButNotSeparators()
    {
        var encoded = LocalPathMapper.EncodeCloudRelativePath(
            "研究 データ/a#b%c+d.txt");

        Assert.Equal(
            "%E7%A0%94%E7%A9%B6%20%E3%83%87%E3%83%BC%E3%82%BF/a%23b%25c%2Bd.txt",
            encoded);
    }

    [Fact]
    public void EncodeCloudRelativePath_DoesNotTreatExistingPercentTextAsEncoded()
    {
        var encoded = LocalPathMapper.EncodeCloudRelativePath("Data/already%23.txt");

        Assert.Equal("Data/already%2523.txt", encoded);
    }

    [Theory]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData(".")]
    [InlineData("..")]
    public void EncodePathSegment_RejectsSeparatorsAndTraversal(string segment)
    {
        Assert.Throws<ArgumentException>(() => LocalPathMapper.EncodePathSegment(segment));
    }
}
