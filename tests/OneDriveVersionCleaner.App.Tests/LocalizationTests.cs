using OneDriveVersionCleaner.App.Localization;
using static OneDriveVersionCleaner.App.Localization.Localization;

namespace OneDriveVersionCleaner.App.Tests;

public sealed class SupportedCulturesTests
{
    [Theory]
    [InlineData("ja", "ja")]
    [InlineData("ja-JP", "ja")]
    [InlineData("JA-jp", "ja")]
    [InlineData("en", "en")]
    [InlineData("en-GB", "en")]
    public void Resolve_MapsRegionalNamesOntoTheirLanguage(string cultureName, string expected)
    {
        Assert.Equal(expected, SupportedCultures.Resolve(cultureName).Name);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("zh-Hant-TW")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not a culture at all")]
    public void Resolve_FallsBackToEnglishForAnythingElse(string? cultureName)
    {
        Assert.Equal("en", SupportedCultures.Resolve(cultureName).Name);
    }

    [Fact]
    public void Resolve_DoesNotMatchOnABarePrefix()
    {
        // "java" starts with "ja" but is not Japanese. Matching had better require a separator.
        Assert.Equal("en", SupportedCultures.Resolve("java").Name);
    }

    [Fact]
    public void Default_IsEnglish() => Assert.Equal("en", SupportedCultures.Default.Name);

    [Fact]
    public void All_NamesAndFontsAreFilledIn()
    {
        Assert.All(SupportedCultures.All, culture =>
        {
            Assert.False(string.IsNullOrWhiteSpace(culture.Name));
            Assert.False(string.IsNullOrWhiteSpace(culture.NativeName));
            Assert.False(string.IsNullOrWhiteSpace(culture.FontFamily));
        });
    }
}

public sealed class LocalizationTests
{
    [Fact]
    public void Loc_PicksTheTranslationForTheCurrentLanguage()
    {
        using var _ = new UiCultureScope("ja");

        Assert.Equal("保持", Loc(en: "Keep", ja: "保持"));
    }

    [Fact]
    public void Loc_UsesEnglishForALanguageWithNoTranslation()
    {
        using var _ = new UiCultureScope("de");

        Assert.Equal("Keep", Loc(en: "Keep", ja: "保持"));
    }

    [Fact]
    public void Loc_UsesEnglishWhenTheTranslationIsMissing()
    {
        using var _ = new UiCultureScope("ja");

        // A string that has not been translated yet must read as English, not as empty.
        Assert.Equal("Not translated yet", Loc(en: "Not translated yet"));
    }

    [Fact]
    public void Loc_FollowsARegionalCulture()
    {
        using var _ = new UiCultureScope("ja-JP");

        Assert.Equal("保持", Loc(en: "Keep", ja: "保持"));
    }
}
