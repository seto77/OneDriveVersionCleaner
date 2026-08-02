using OneDriveVersionCleaner.Infrastructure.OneDriveAccounts;

namespace OneDriveVersionCleaner.Infrastructure.Tests;

public sealed class OneDriveBusinessAccountDiscoveryTests
{
    // Obviously synthetic, so nobody has to wonder whether a real tenant leaked into a fixture.
    private const string TenantId = "00000000-0000-4000-8000-000000000001";

    [Fact]
    public void Discover_NormalizesAValidBusinessAccountAndDerivesSiteUri()
    {
        var discovery = CreateDiscovery(
            Snapshot(
                accountKey: "Business1",
                root: @"C:\Users\Test\OneDrive - University\",
                endpoint: "https://example-my.sharepoint.com/personal/user_example_edu/_api/"));

        var account = Assert.Single(discovery.Discover());

        Assert.Equal("Business1", account.AccountKey);
        Assert.Equal("University", account.DisplayName);
        Assert.Equal(@"C:\Users\Test\OneDrive - University", account.UserFolder);
        Assert.Equal(TenantId, account.TenantId);
        Assert.Equal(
            "https://example-my.sharepoint.com/personal/user_example_edu/_api",
            account.ServiceEndpointUri.AbsoluteUri.TrimEnd('/'));
        Assert.Equal(
            "https://example-my.sharepoint.com/personal/user_example_edu",
            account.SiteUri.AbsoluteUri.TrimEnd('/'));
    }

    [Fact]
    public void Discover_UsesTenantIdFallbackAndAccountKeyDisplayName()
    {
        var snapshot = Snapshot("Business2", @"D:\OneDrive", ValidEndpoint) with
        {
            DisplayName = " ",
            ConfiguredTenantId = "damaged-registry-value",
            TenantId = TenantId.ToUpperInvariant(),
        };

        var account = Assert.Single(CreateDiscovery(snapshot).Discover());

        Assert.Equal("Business2", account.DisplayName);
        Assert.Equal(TenantId, account.TenantId);
    }

    [Theory]
    [InlineData("Personal", @"C:\OneDrive", "https://example-my.sharepoint.com/personal/user/_api")]
    [InlineData("Business", @"C:\OneDrive", "https://example-my.sharepoint.com/personal/user/_api")]
    [InlineData("BusinessX", @"C:\OneDrive", "https://example-my.sharepoint.com/personal/user/_api")]
    [InlineData("Business1", "relative", "https://example-my.sharepoint.com/personal/user/_api")]
    [InlineData("Business1", @"C:\OneDrive", "http://example-my.sharepoint.com/personal/user/_api")]
    [InlineData("Business1", @"C:\OneDrive", "https://example.invalid/personal/user/_api")]
    [InlineData("Business1", @"C:\OneDrive", "https://example-my.sharepoint.com/personal/user")]
    [InlineData("Business1", @"C:\OneDrive", "https://example-my.sharepoint.com/personal/user/_api?x=1")]
    public void Discover_SkipsInvalidOrNonBusinessEntries(
        string accountKey,
        string root,
        string endpoint)
    {
        var discovery = CreateDiscovery(Snapshot(accountKey, root, endpoint));

        Assert.Empty(discovery.Discover());
    }

    [Fact]
    public void Discover_SkipsMalformedEntryWithoutHidingValidEntry()
    {
        var invalid = Snapshot("Business1", @"C:\OneDrive", ValidEndpoint) with
        {
            ConfiguredTenantId = "not-a-tenant-id",
            TenantId = null,
        };
        var valid = Snapshot("Business2", @"D:\OneDrive", ValidEndpoint);

        var account = Assert.Single(CreateDiscovery(invalid, valid).Discover());

        Assert.Equal("Business2", account.AccountKey);
    }

    [Fact]
    public void FindBestMatch_UsesTheLongestContainingRoot()
    {
        var parent = Account("Business1", @"C:\Users\Test\OneDrive - University");
        var nested = Account("Business2", @"C:\Users\Test\OneDrive - University\Team");

        var match = OneDriveBusinessAccountDiscovery.FindBestMatch(
            [parent, nested],
            @"c:\users\test\onedrive - university\team\Research\paper.docx");

        Assert.Same(nested, match);
    }

    [Fact]
    public void FindBestMatch_MatchesRootItselfButNotAPrefixSibling()
    {
        var account = Account("Business1", @"C:\Users\Test\OneDrive - University");

        Assert.Same(
            account,
            OneDriveBusinessAccountDiscovery.FindBestMatch([account], account.UserFolder));
        Assert.Null(OneDriveBusinessAccountDiscovery.FindBestMatch(
            [account],
            @"C:\Users\Test\OneDrive - University Backup\file.docx"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative\\file.docx")]
    public void FindBestMatch_ReturnsNullForInvalidInput(string? path)
    {
        Assert.Null(OneDriveBusinessAccountDiscovery.FindBestMatch(
            [Account("Business1", @"C:\OneDrive")],
            path));
    }

    [Fact]
    public void Discover_ReturnsEmptyWhenRegistryAccessFails()
    {
        var discovery = new OneDriveBusinessAccountDiscovery(
            new ThrowingRegistryReader(new UnauthorizedAccessException()));

        Assert.Empty(discovery.Discover());
    }

    private const string ValidEndpoint =
        "https://example-my.sharepoint.com/personal/user_example_edu/_api";

    private static OneDriveBusinessAccountDiscovery CreateDiscovery(
        params OneDriveAccountRegistrySnapshot[] snapshots) =>
        new(new StubRegistryReader(snapshots));

    private static OneDriveAccountRegistrySnapshot Snapshot(
        string accountKey,
        string root,
        string endpoint) =>
        new()
        {
            AccountKey = accountKey,
            DisplayName = "University",
            UserFolder = root,
            ConfiguredTenantId = TenantId,
            ServiceEndpointUri = endpoint,
        };

    private static OneDriveBusinessAccount Account(string accountKey, string root) =>
        new()
        {
            AccountKey = accountKey,
            DisplayName = accountKey,
            UserFolder = root,
            TenantId = TenantId,
            ServiceEndpointUri = new(ValidEndpoint),
            SiteUri = new("https://example-my.sharepoint.com/personal/user_example_edu"),
        };

    private sealed class StubRegistryReader(
        IReadOnlyList<OneDriveAccountRegistrySnapshot> snapshots)
        : IOneDriveAccountRegistryReader
    {
        public IReadOnlyList<OneDriveAccountRegistrySnapshot> ReadAccounts() => snapshots;
    }

    private sealed class ThrowingRegistryReader(Exception exception) : IOneDriveAccountRegistryReader
    {
        public IReadOnlyList<OneDriveAccountRegistrySnapshot> ReadAccounts() => throw exception;
    }
}
