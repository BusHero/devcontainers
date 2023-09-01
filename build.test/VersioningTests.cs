using Xunit.Abstractions;

namespace build.test;

public sealed class VersioningTests : IAsyncLifetime
{
    private readonly CustomFixture fixture;

    public VersioningTests(ITestOutputHelper outputHelper)
    {
        this.fixture = new CustomFixture
        {
            KeepCommits = false,
            KeepFiles = false,
            KeepTags = false
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
        (major, minor, build) = (Math.Abs(major), Math.Abs(minor), Math.Abs(build));
        var featureRoot = fixture.GetFeatureRoot(feature);
        var featureFile = fixture.GetFeatureConfig(feature);

        await this.fixture.AddGitTag(gitTag);

        await fixture.CreateFeatureConfig(feature, major, minor, build);
        await fixture.Commit(featureRoot, $"feat: {message}");
        fixture.CreateTempFile(featureRoot / "foo");
        await fixture.Commit(featureRoot, $"chore: {message}");

        await fixture.RunBuild(feature);

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
        (major, minor, build) = (Math.Abs(major), Math.Abs(minor), Math.Abs(build));
        var featureRoot = fixture.GetFeatureRoot(feature);
        var featureFile = fixture.GetFeatureConfig(feature);

        await fixture.AddGitTag(gitTag);
        await fixture.CreateFeatureConfig(feature, major, minor, build);
        await fixture.Commit(featureRoot, $"chore: {message}");
        fixture.CreateTempFile(featureRoot / "foo");
        await fixture.Commit(featureRoot, $"chore: {message}");

        await fixture.RunBuild(feature);

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
        (major, minor, build) = (Math.Abs(major), Math.Abs(minor), Math.Abs(build));

        await fixture.AddGitTag(rightTag);
        await fixture.CreateFeatureConfig(feature, major, minor, build);
        await fixture.Commit(featureRoot, $"feat: {message}");
        await fixture.AddGitTag(wrongTag);
        fixture.CreateTempFile(featureRoot / "foo");
        await fixture.Commit(featureRoot, $"chore: {message}");

        await fixture.RunBuild(feature);

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
        (major, minor, build) = (Math.Abs(major), Math.Abs(minor), Math.Abs(build));

        await fixture.AddGitTag(rightTag);
        await fixture.CreateFeatureConfig(feature, major, minor, build);
        await fixture.Commit(featureRoot, $"chore: {message}");
        await fixture.AddGitTag(wrongTag);
        fixture.CreateTempFile(fixture.RootDirectory / tempFileName);
        await fixture.Commit(fixture.RootDirectory / tempFileName, $"feat: {message}");

        await fixture.RunBuild(feature);

        var version = await fixture.GetVersion(feature);

        version
            .Should()
            .Be($"{major}.{minor + 1}.0");
    }

    public async Task InitializeAsync() => await Task.CompletedTask;

    public async Task DisposeAsync() => await fixture.DisposeAsync();
}
