using System.Globalization;
using OneDriveVersionCleaner.Core.Formatting;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class ByteSizeFormatterTests
{
    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(1023L, "1023 B")]
    [InlineData(1024L, "1 KB")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    [InlineData(1099511627776L, "1 TB")]
    public void Format_UsesBinaryBoundaries(long bytes, string expected)
    {
        Assert.Equal(
            expected,
            ByteSizeFormatter.Format(bytes, formatProvider: CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Format_NullIsNotDisplayedAsZero()
    {
        // The wording belongs to whoever is going to show it, which is why there is no default.
        Assert.Equal("Unknown", ByteSizeFormatter.Format(null, unknownText: "Unknown"));
        Assert.Equal("", ByteSizeFormatter.Format(null, unknownText: ""));
    }

    [Fact]
    public void Format_NegativeValueIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ByteSizeFormatter.Format(-1));
    }
}
