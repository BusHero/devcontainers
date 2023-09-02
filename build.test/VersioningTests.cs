namespace build.test;

public sealed class VersioningTests : IAsyncLifetime
{
    private readonly CustomFixture fixture;

    public VersioningTests()
    {
        this.fixture = new CustomFixture
        {
            KeepCommits = false,
            KeepFiles = false,
            KeepTags = false,
        };
    }

    [Theory, AutoData]
    public async Task TwoCommits_FeatAndChore_UpdatesMajor(
        Feature feature,
        Version version)
    {
        await fixture.AddGitTag(feature.GetTag());
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(fixture.RootDirectory));
        feature.CreateTempFile(fixture.RootDirectory);
        await fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(fixture.RootDirectory));

        await fixture.RunVersionTarget(feature);

        var newVersion = await feature.GetVersion(fixture.RootDirectory);

        newVersion
            .Should()
            .Be(version.IncrementMajor());
    }

    [Theory, AutoData]
    public async Task TwoCommits_ChoreAndChore_UpdatesChore(
        Feature feature,
        Version version)
    {
        await fixture.AddGitTag(feature.GetTag());
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(fixture.RootDirectory));
        feature.CreateTempFile(fixture.RootDirectory);
        await fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(fixture.RootDirectory));

        await fixture.RunVersionTarget(feature);

        var newVersion = await fixture.GetVersion(feature);

        newVersion
            .Should()
            .Be(version.IncrementMinor());
    }

    [Theory, AutoData]
    public async Task UseTheRightTag(
        Feature feature,
        Version version,
        Tag wrongTag)
    {
        await fixture.AddGitTag(feature.GetTag());
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(fixture.RootDirectory));
        await fixture.AddGitTag(wrongTag);
        feature.CreateTempFile(fixture.RootDirectory);
        await fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(fixture.RootDirectory));

        await fixture.RunVersionTarget(feature);

        var newVersion = await fixture.GetVersion(feature);

        newVersion
            .Should()
            .Be(version.IncrementMajor());
    }

    [Theory, AutoData]
    public async Task UseTheRightCommit(
        Feature feature,
        Version version)
    {
        await fixture.AddGitTag(feature.GetTag());
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(fixture.RootDirectory));
        var tempFileName = fixture.CreateTempFile();
        await fixture.AddAndCommit(CommitMessage.New("feat"), tempFileName);

        await fixture.RunVersionTarget(feature);

        var newVersion = await fixture.GetVersion(feature);

        newVersion
            .Should()
            .Be(version.IncrementMinor());
    }

    [Theory, AutoData]
    public async Task CreateVersionChangeCommitCreatesCommitWithRightMessage(
        Feature feature,
        Version version)
    {
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.RunCreateReleaseCommitTarget(feature);

        var expectedMessage = await fixture.GetLatestCommitMessage();

        expectedMessage
            .Should()
            .Be($"chore(version): Release feature/{feature} {version}");
    }

    [Theory, AutoData]
    public async Task CreateReleaseCommitContainsOnlyConfigFile(
        Feature feature,
        Version version)
    {
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.RunCreateReleaseCommitTarget(feature);

        var modifiedFiles = await fixture.GetModifiedFilesLatestCommit();

        modifiedFiles
            .Should()
            .Equal(feature.GetRelativePathToConfig());
    }

    public async Task InitializeAsync() => await fixture.SaveCommit("HEAD");

    public async Task DisposeAsync() => await fixture.DisposeAsync();
}
