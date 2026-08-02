using OneDriveVersionCleaner.App.Localization;
using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Retention;

namespace OneDriveVersionCleaner.App.Tests;

/// <summary>
/// Guards the enumerations the grids and the dialogs render. Adding a value to one of them
/// without a translation is easy to miss: the fallback quietly shows the identifier instead.
/// </summary>
/// <remarks>
/// The Japanese name is what makes the miss detectable. A value with no case of its own falls
/// through to <c>ToString()</c>, which is an ASCII identifier, so a Japanese name equal to the
/// identifier is exactly the miss being looked for. English cannot be checked that way, because
/// several values are legitimately named after themselves ("Error", "Warning", "Keep").
/// </remarks>
public sealed class DisplayNamesTests
{
    [Fact]
    public void EveryRetentionDispositionIsTranslated() =>
        AssertAllTranslated(Enum.GetValues<RetentionDisposition>(), DisplayNames.Of);

    [Fact]
    public void EveryRetentionReasonIsTranslated() =>
        AssertAllTranslated(Enum.GetValues<RetentionReason>(), DisplayNames.Of);

    [Fact]
    public void EveryRetrievalStatusIsTranslated() =>
        AssertAllTranslated(Enum.GetValues<RetrievalStatus>(), DisplayNames.Of);

    [Fact]
    public void EveryDeleteResultStatusIsTranslated() =>
        AssertAllTranslated(Enum.GetValues<DeleteResultStatus>(), DisplayNames.Of);

    [Fact]
    public void EveryLocalFileChangeIsTranslated() =>
        AssertAllTranslated(
            Enum.GetValues<LocalFileChange>(),
            VersionDeletionRunner.DescribeLocalChange);

    private static void AssertAllTranslated<T>(T[] values, Func<T, string> nameOf)
        where T : struct, Enum
    {
        foreach (var value in values)
        {
            string english;
            string japanese;
            using (var _ = new UiCultureScope("en"))
            {
                english = nameOf(value);
            }

            using (var _ = new UiCultureScope("ja"))
            {
                japanese = nameOf(value);
            }

            Assert.False(
                string.IsNullOrWhiteSpace(english),
                $"{typeof(T).Name}.{value} has no English name");
            Assert.NotEqual(value.ToString(), japanese);
            Assert.NotEqual(english, japanese);
        }
    }
}
