using FluentAssertions.Execution;
using Nuke.Common.IO;

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
        string feature,
        int major,
        int minor,
        int build,
        string message)
    {
        feature = feature.Replace("-", string.Empty);
        var gitTag = fixture.GetTagForFeature(feature);
        var featureRoot = fixture.GetFeatureRoot(feature);
        var featureFile = fixture.GetFeatureConfig(feature);

        await this.fixture.AddGitTag(gitTag);
        await fixture.CreateFeatureConfig(feature, major, minor, build);
        await fixture.Commit(featureRoot, $"feat: {message}");
        fixture.CreateTempFile(featureRoot / "foo");
        await fixture.Commit(featureRoot, $"chore: {message}");

        await fixture.RunVersionTarget(feature);

        var version = await fixture.GetVersion(feature);

        version
            .Should()
            .Be($"{major + 1}.0.0");
    }

    [Theory, AutoData]
    public async Task TwoCommits_ChoreAndChore_UpdatesChore(
        string feature,
        int major,
        int minor,
        int build,
        string message)
    {
        feature = feature.Replace("-", string.Empty);
        var gitTag = fixture.GetTagForFeature(feature);
        var featureRoot = fixture.GetFeatureRoot(feature);
        var featureFile = fixture.GetFeatureConfig(feature);

        await fixture.AddGitTag(gitTag);
        await fixture.CreateFeatureConfig(feature, major, minor, build);
        await fixture.Commit(featureRoot, $"chore: {message}");
        fixture.CreateTempFile(featureRoot / "foo");
        await fixture.Commit(featureRoot, $"chore: {message}");

        await fixture.RunVersionTarget(feature);

        var version = await fixture.GetVersion(feature);

        version
            .Should()
            .Be($"{major}.{minor + 1}.0");
    }

    [Theory, AutoData]
    public async Task UseTheRightTag(
        string feature,
        int major,
        int minor,
        int build,
        string wrongTag,
        string message)
    {
        feature = feature.Replace("-", string.Empty);
        var featureRoot = fixture.GetFeatureRoot(feature);
        var rightTag = fixture.GetTagForFeature(feature);
        wrongTag = wrongTag.Replace("-", "");

        await fixture.AddGitTag(rightTag);
        await fixture.CreateFeatureConfig(feature, major, minor, build);
        await fixture.Commit(featureRoot, $"feat: {message}");
        await fixture.AddGitTag(wrongTag);
        fixture.CreateTempFile(featureRoot / "foo");
        await fixture.Commit(featureRoot, $"chore: {message}");

        await fixture.RunVersionTarget(feature);

        var version = await fixture.GetVersion(feature);

        version
            .Should()
            .Be($"{major + 1}.0.0");
    }

    [Theory, AutoData]
    public async Task UseTheRightCommit(
        string feature,
        string tempFileName,
        int major,
        int minor,
        int build,
        string wrongTag,
        string message)
    {
        feature = feature.Replace("-", string.Empty);
        var featureRoot = fixture.GetFeatureRoot(feature);
        var rightTag = fixture.GetTagForFeature(feature);
        wrongTag = wrongTag.Replace("-", "");

        await fixture.AddGitTag(rightTag);
        await fixture.CreateFeatureConfig(feature, major, minor, build);
        await fixture.Commit(featureRoot, $"chore: {message}");
        await fixture.AddGitTag(wrongTag);
        fixture.CreateTempFile(fixture.RootDirectory / tempFileName);
        await fixture.Commit(fixture.RootDirectory / tempFileName, $"feat: {message}");

        await fixture.RunVersionTarget(feature);

        var version = await fixture.GetVersion(feature);

        version
            .Should()
            .Be($"{major}.{minor + 1}.0");
    }

    [Theory, AutoData]
    public async Task Commit_NewCommitIsCreated(
        string feature,
        int major,
        int minor,
        int build)
    {
        feature = feature.Replace("-", string.Empty);
        var featureConfig = fixture
            .RootDirectory
            .GetRelativePathTo(fixture.GetFeatureConfig(feature));

        await fixture.CreateFeatureConfig(feature, major, minor, build);

        await fixture.RunBuild(args => args
            .Add("--target")
            .Add("CreateVersionChangeCommit")
            .Add("--feature")
            .Add(feature));

        var expectedMessage = await fixture.GetLatestCommitMessage();
        var modifiedFiles = await fixture.GetModifiedFilesLatestCommit();

        using (new AssertionScope())
        {
            expectedMessage
                .Should()
                .Be($"chore(version): Release feature/{feature} {major}.{minor}.{build}");

            modifiedFiles
                .Should()
                .Equal(featureConfig);
        }
    }

    public async Task InitializeAsync() => await fixture.SaveCommit("HEAD");

    public async Task DisposeAsync() => await fixture.DisposeAsync();
}
