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
            KeepTags = false
        };
    }

    [Theory]
    [AutoData]
    public async Task TwoCommits_FeatAndChore_UpdatesMajor(
        string feature,
        int major,
        int minor,
        int build,
        string message)
    {
        feature = feature.Replace("-", "");
        var gitTag = fixture.GetRightTag(feature);
        (major, minor, build) = (Math.Abs(major), Math.Abs(minor), Math.Abs(build));
        var featureRoot = Nuke.Common.NukeBuild.RootDirectory
            / "features"
            / "src"
            / feature;
        var featureFile = featureRoot
            / "devcontainer-feature.json";

        await this.fixture.AddGitTag(gitTag);

        await fixture.CreateFeatureFile(featureFile, major, minor, build);
        await fixture.Commit(featureRoot, $"feat: {message}");
        File.WriteAllText(featureRoot / "foo", string.Empty);
        await fixture.Commit(featureRoot, $"chore: {message}");

        await fixture.RunBuild(feature);

        var version = await fixture.GetVersion(featureFile);

        version
            .Should()
            .Be($"{major + 1}.0.0");
    }

    [Theory]
    [AutoData]
    public async Task TwoCommits_ChoreAndChore_UpdatesChore(
        string feature,
        int major,
        int minor,
        int build,
        string message)
    {
        feature = feature.Replace("-", "");
        var gitTag = fixture.GetRightTag(feature);
        (major, minor, build) = (Math.Abs(major), Math.Abs(minor), Math.Abs(build));
        var featureRoot = Nuke.Common.NukeBuild.RootDirectory
            / "features"
            / "src"
            / feature;
        var featureFile = featureRoot
            / "devcontainer-feature.json";

        await fixture.AddGitTag(gitTag);

        await fixture.CreateFeatureFile(featureFile, major, minor, build);
        await fixture.Commit(featureRoot, $"chore: {message}");
        File.WriteAllText(featureRoot / "foo", string.Empty);
        await fixture.Commit(featureRoot, $"chore: {message}");

        await fixture.RunBuild(feature);

        var version = await fixture.GetVersion(featureFile);

        version
            .Should()
            .Be($"{major}.{minor + 1}.0");
    }

    [Theory]
    [AutoData]
    public async Task UseTheRightTag(
        string feature,
        int major,
        int minor,
        int build,
        string wrongTag,
        string message)
    {
        feature = feature.Replace("-", "");
        var rightTag = fixture.GetRightTag(feature);
        wrongTag = wrongTag.Replace("-", "");
        (major, minor, build) = (Math.Abs(major), Math.Abs(minor), Math.Abs(build));
        var featureRoot = Nuke.Common.NukeBuild.RootDirectory
            / "features"
            / "src"
            / feature;
        var featureFile = featureRoot
            / "devcontainer-feature.json";

        await fixture.AddGitTag(rightTag);
        await fixture.CreateFeatureFile(featureFile, major, minor, build);
        await fixture.Commit(featureRoot, $"feat: {message}");
        await fixture.AddGitTag(wrongTag);
        File.WriteAllText(featureRoot / "foo", string.Empty);
        await fixture.Commit(featureRoot, $"chore: {message}");

        await fixture.RunBuild(feature);

        var version = await fixture.GetVersion(featureFile);

        version
            .Should()
            .Be($"{major + 1}.0.0");
    }

    public async Task InitializeAsync() => await Task.CompletedTask;

    public async Task DisposeAsync() => await fixture.DisposeAsync();
}
