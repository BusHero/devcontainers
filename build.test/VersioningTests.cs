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
        Feature feature,
        Version version)
    {
        await fixture.AddGitTag(feature.GetTag(version));
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(fixture.RootDirectory));
        feature.CreateTempFile(fixture.RootDirectory);
        await fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(fixture.RootDirectory));

        await fixture.RunVersionTarget(feature);

        var newVersion = await feature.GetVersion(fixture.RootDirectory);

        newVersion
            .Should()
            .Be(version.IncrementMinor());
    }

    [Theory, AutoData]
    public async Task NoPriorTagDontIncrementVersion(
        Feature feature,
        Version version)
    {
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(fixture.RootDirectory));

        await fixture.RunVersionTarget(feature);

        var newVersion = await feature.GetVersion(fixture.RootDirectory);

        newVersion
            .Should()
            .Be(version);
    }

    [Theory, AutoData]
    public async Task WrongTagNameAbort(
        Feature feature,
        Version version)
    {
        await fixture.CreateFeatureConfig(feature, version);

        await fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(fixture.RootDirectory));

        await fixture.RunVersionTarget(feature);

        var newVersion = await feature.GetVersion(fixture.RootDirectory);

        newVersion
            .Should()
            .Be(version);
    }

    [Theory, AutoData]
    public async Task TwoCommits_ChoreAndChore_UpdatesChore(
        Feature feature,
        Version version)
    {
        await fixture.AddGitTag(feature.GetTag(version));
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(fixture.RootDirectory));
        feature.CreateTempFile(fixture.RootDirectory);
        await fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(fixture.RootDirectory));

        await fixture.RunVersionTarget(feature);

        var newVersion = await fixture.GetVersion(feature);

        newVersion
            .Should()
            .Be(version.IncrementBuild());
    }

    [Theory, AutoData]
    public async Task UseTheRightTag(
        Feature feature,
        Version version,
        Tag wrongTag)
    {
        await fixture.AddGitTag(feature.GetTag(version));
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(fixture.RootDirectory));
        await fixture.AddGitTag(wrongTag);
        feature.CreateTempFile(fixture.RootDirectory);
        await fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(fixture.RootDirectory));

        await fixture.RunVersionTarget(feature);

        var newVersion = await fixture.GetVersion(feature);

        newVersion
            .Should()
            .Be(version.IncrementMinor());
    }

    [Theory, AutoData]
    public async Task UseTheRightCommit(
        Feature feature,
        Version version)
    {
        await fixture.AddGitTag(feature.GetTag(version));
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(fixture.RootDirectory));
        var tempFileName = fixture.CreateTempFile();
        await fixture.AddAndCommit(CommitMessage.New("feat"), tempFileName);

        await fixture.RunVersionTarget(feature);

        var newVersion = await fixture.GetVersion(feature);

        newVersion
            .Should()
            .Be(version.IncrementBuild());
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
            .Be($"Release: feature {feature} {version}");
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
            .Contain(feature.GetRelativePathToConfig());
    }

    [Theory, AutoData]
    public async Task CreateReleaseCommitContainsAllFilesFromFeatureRoot(
        Feature feature,
        Version version)
    {
        await fixture.CreateFeatureConfig(feature, version);
        var tempFile = feature.CreateTempFile(fixture.RootDirectory);
        await fixture.RunCreateReleaseCommitTarget(feature);

        var modifiedFiles = await fixture.GetModifiedFilesLatestCommit();

        modifiedFiles
            .Should()
            .Contain(fixture.RootDirectory.GetRelativePathTo(tempFile));
    }

    [Theory, AutoData]
    public async Task ReleaseCommitWasCreatedByTheRightUser(
        Feature feature,
        Version version)
    {
        await fixture.CreateFeatureConfig(feature, version);
        var tempFile = feature.CreateTempFile(fixture.RootDirectory);
        await fixture.RunCreateReleaseCommitTarget(feature);

        var username = await fixture.GetCommitterName();
        var email = await fixture.GetCommiterEmail();

        using (new AssertionScope())
        {
            username.Should().Be("Release Bot");
            email.Should().Be("noreply@github.com");
        }
    }

    [Theory, AutoData]
    public async Task CreateTagForReleaseWithRightName(
        Feature feature,
        Version version)
    {
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(fixture.RootDirectory));
        await fixture.RunCreateReleaseTagTarget(feature);

        var tag = await fixture.GetLatestTag(feature);

        tag.Should()
            .Be(feature.GetTag(version));
    }

    [Theory, AutoData]
    public async Task GenerateDocumentation(
        Feature feature,
        Version version)
    {
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.RunGenerateDocumentationTarget(feature);

        var documentation = feature.GetDocumentation(fixture.RootDirectory);

        File.Exists(documentation).Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task Release_UpdateVersion(
        Feature feature,
        Version version)
    {
        await fixture.OverrideOrigin();
        var expectedVersion = version.IncrementMinor();
        await fixture.AddGitTag(feature.GetTag(version));
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(fixture.RootDirectory));

        await fixture.RunReleaseFeature(feature);

        var newVersion = await feature.GetVersion(fixture.RootDirectory);
        var commitMessage = await fixture.GetLatestCommitMessage();
        var files = await fixture.GetModifiedFilesLatestCommit();
        var latestGitTag = await fixture.GetLatestTag(feature);
        var hashForTag = await fixture.GetGitHash(latestGitTag);
        var hashForHead = await fixture.GetGitHash("HEAD");
        using (new AssertionScope())
        {
            newVersion.Should().Be(expectedVersion);
            commitMessage.Should().Be($"Release: feature {feature} {expectedVersion}");
            files.Should().Contain(
                feature.GetRelativePathToConfig(),
                feature.GetRelativePathToDocumentation());
            latestGitTag.Should().Be($"feature_{feature}_{expectedVersion}");
            hashForHead.Should().Be(hashForTag);
        }
    }

    [Theory, AutoData]
    public async Task ReleasePushesToOrigin(
        Feature feature,
        Version version)
    {
        await fixture.OverrideOrigin();
        await fixture.AddGitTag(feature.GetTag(version));
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(fixture.RootDirectory));

        await fixture.RunReleaseFeature(feature);

        await fixture.GetLatestCommitMessage();
        var latestTag = await fixture.GetLatestTag(feature, fixture.GitOriginPath);
        var latestMessage = await fixture.GetLatestCommitMessage(fixture.GitOriginPath);
        var expectedMessage = await fixture.GetLatestCommitMessage();

        using (new AssertionScope())
        {
            latestTag.Should().Be(feature.GetTag(version.IncrementMajor()));
            latestMessage.Should().Be(expectedMessage);
            latestMessage.Should().NotBeNullOrEmpty();
        }
    }

    public async Task InitializeAsync()
    {
        await fixture.SaveCommit("HEAD");
        await fixture.SaveTags();
    }

    public async Task DisposeAsync() => await fixture.DisposeAsync();
}
