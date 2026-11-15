using FluentAssertions.Execution;
using GitSnapshotter;
using Nuke.Common.IO;
using Xunit.Abstractions;

namespace build.test;

[Collection(RepositoryCollection.Name)]
public sealed class VersioningTests(ITestOutputHelper output) : IAsyncLifetime
{
    private readonly CustomFixture _fixture = new(output);

    [Theory, AutoData]
    public async Task TwoCommits_FeatAndChore_UpdatesMajor(
        Feature feature,
        Version version)
    {
        await _fixture.AddGitTag(feature.GetTag(version));
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));
        feature.CreateTempFile(_fixture.RootDirectory);
        await _fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(_fixture.RootDirectory));

        await _fixture.RunVersionTarget(feature);

        var newVersion = await feature.GetVersion(_fixture.RootDirectory);

        newVersion
            .Should()
            .Be(version.IncrementMinor());
    }

    [Theory, AutoData]
    public async Task NoPriorTagDontIncrementVersion(
        Feature feature,
        Version version)
    {
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));

        await _fixture.RunVersionTarget(feature);

        var newVersion = await feature.GetVersion(_fixture.RootDirectory);

        newVersion
            .Should()
            .Be(version);
    }

    [Theory, AutoData]
    public async Task WrongTagNameAbort(
        Feature feature,
        Version version,
        Tag unrelatedTag)
    {
        await _fixture.AddGitTag(unrelatedTag);
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));

        await _fixture.RunVersionTarget(feature);

        var newVersion = await feature.GetVersion(_fixture.RootDirectory);

        newVersion
            .Should()
            .Be(version);
    }

    [Theory, AutoData]
    public async Task TwoCommits_ChoreAndChore_UpdatesChore(
        Feature feature,
        Version version)
    {
        await _fixture.AddGitTag(feature.GetTag(version));
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(_fixture.RootDirectory));
        feature.CreateTempFile(_fixture.RootDirectory);
        await _fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(_fixture.RootDirectory));

        await _fixture.RunVersionTarget(feature);

        var newVersion = await _fixture.GetVersion(feature);

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
        await _fixture.AddGitTag(feature.GetTag(version));
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));
        await _fixture.AddGitTag(wrongTag);
        feature.CreateTempFile(_fixture.RootDirectory);
        await _fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(_fixture.RootDirectory));

        await _fixture.RunVersionTarget(feature);

        var newVersion = await _fixture.GetVersion(feature);

        newVersion
            .Should()
            .Be(version.IncrementMinor());
    }

    [Theory, AutoData]
    public async Task UseTheRightCommit(
        Feature feature,
        Version version)
    {
        await _fixture.AddGitTag(feature.GetTag(version));
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("chore"), feature.GetRoot(_fixture.RootDirectory));
        var tempFileName = _fixture.CreateTempFile();
        await _fixture.AddAndCommit(CommitMessage.New("feat"), tempFileName);

        await _fixture.RunVersionTarget(feature);

        var newVersion = await _fixture.GetVersion(feature);

        newVersion
            .Should()
            .Be(version.IncrementBuild());
    }

    [Theory, AutoData]
    public async Task CreateVersionChangeCommitCreatesCommitWithRightMessage(
        Feature feature,
        Version version)
    {
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.RunCreateReleaseCommitTarget(feature);

        var expectedMessage = await _fixture.GetLatestCommitMessage();

        expectedMessage
            .Should()
            .Be($"Release: feature {feature} {version}");
    }

    [Theory, AutoData]
    public async Task CreateReleaseCommitContainsOnlyConfigFile(
        Feature feature,
        Version version)
    {
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.RunCreateReleaseCommitTarget(feature);

        var modifiedFiles = await _fixture.GetModifiedFilesLatestCommit();

        modifiedFiles
            .Should()
            .Contain(feature.GetRelativePathToConfig());
    }

    [Theory, AutoData]
    public async Task CreateReleaseCommitContainsAllFilesFromFeatureRoot(
        Feature feature,
        Version version)
    {
        await _fixture.CreateFeatureConfig(feature, version);
        var tempFile = feature.CreateTempFile(_fixture.RootDirectory);
        await _fixture.RunCreateReleaseCommitTarget(feature);

        var modifiedFiles = await _fixture.GetModifiedFilesLatestCommit();

        modifiedFiles
            .Should()
            .Contain(_fixture.RootDirectory.GetRelativePathTo(tempFile));
    }

    [Theory, AutoData]
    public async Task ReleaseCommitWasCreatedByTheRightUser(
        Feature feature,
        Version version)
    {
        await _fixture.CreateFeatureConfig(feature, version);
        feature.CreateTempFile(_fixture.RootDirectory);
        await _fixture.RunCreateReleaseCommitTarget(feature);

        var username = await _fixture.GetCommitterName();
        var email = await _fixture.GetCommiterEmail();

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
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));
        await _fixture.RunCreateReleaseTagTarget(feature);

        var tag = await _fixture.GetLatestTag(feature);

        tag.Should()
            .Be(feature.GetTag(version));
    }

    [Theory, AutoData]
    public async Task GenerateDocumentation(
        Feature feature,
        Version version)
    {
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.RunGenerateDocumentationTarget(feature);

        var documentation = feature.GetDocumentation(_fixture.RootDirectory);

        File.Exists(documentation).Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task Release_UpdateVersion(
        Feature feature,
        Version version)
    {
        await _fixture.OverrideOrigin();
        var expectedVersion = version.IncrementMinor();
        await _fixture.AddGitTag(feature.GetTag(version));
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));

        await _fixture.RunReleaseFeature(feature);

        var newVersion = await feature.GetVersion(_fixture.RootDirectory);
        var commitMessage = await _fixture.GetLatestCommitMessage();
        var files = await _fixture.GetModifiedFilesLatestCommit();
        var latestGitTag = await _fixture.GetLatestTag(feature);
        var hashForTag = await _fixture.GetGitHash(latestGitTag);
        var hashForHead = await _fixture.GetGitHash("HEAD");
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
    public async Task Release_NoPriorVersionWorksAsExpected(
        Feature feature,
        Version version)
    {
        await _fixture.OverrideOrigin();
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));

        await _fixture.RunReleaseFeature(feature);

        var newVersion = await feature.GetVersion(_fixture.RootDirectory);
        var commitMessage = await _fixture.GetLatestCommitMessage();
        var files = await _fixture.GetModifiedFilesLatestCommit();
        var latestGitTag = await _fixture.GetLatestTag(feature);
        var hashForTag = await _fixture.GetGitHash(latestGitTag);
        var hashForHead = await _fixture.GetGitHash("HEAD");
        using (new AssertionScope())
        {
            newVersion.Should().Be(version);
            commitMessage.Should().Be($"Release: feature {feature} {version}");
            files.Should().Contain(
                feature.GetRelativePathToDocumentation());
            latestGitTag.Should().Be($"feature_{feature}_{version}");
            hashForHead.Should().Be(hashForTag);
        }
    }

    [Theory, AutoData]
    public async Task ReleasePushesToOrigin(
        Feature feature,
        Version version)
    {
        await _fixture.OverrideOrigin();
        await _fixture.AddGitTag(feature.GetTag(version));
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));

        await _fixture.RunReleaseFeature(feature);

        await _fixture.GetLatestCommitMessage();
        var latestTag = await _fixture.GetLatestTag(feature, _fixture.GitOriginPath);
        var latestMessage = await _fixture.GetLatestCommitMessage(_fixture.GitOriginPath);
        var expectedMessage = await _fixture.GetLatestCommitMessage();

        using (new AssertionScope())
        {
            latestTag.Should().Be(feature.GetTag(version.IncrementMinor()));
            latestMessage.Should().Be(expectedMessage);
            latestMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Theory, AutoData]
    public async Task NoUpdateDontUpdateVersion(
        Feature feature,
        Version version)
    {
        var commitMessage = CommitMessage.New("feat");
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(commitMessage, feature.GetRoot(_fixture.RootDirectory));
        await _fixture.AddGitTag(feature.GetTag(version));
        await _fixture.OverrideOrigin();

        await _fixture.RunReleaseFeature(feature);

        var latestCommitMessage = await _fixture.GetLatestCommitMessage();
        var latestTag = await _fixture.GetLatestTag(feature, _fixture.GitOriginPath);
        var latestMessage = await _fixture.GetLatestCommitMessage(_fixture.GitOriginPath);

        using (new AssertionScope())
        {
            latestTag.Should().Be(feature.GetTag(version));
            latestCommitMessage.Should().Be(commitMessage);
            latestMessage.Should().Be(commitMessage);
        }
    }

    [Theory, AutoData]
    public async Task VersionTargetDoesNotChangeReferences(
        Feature feature,
        Version version)
    {
        await _fixture.AddGitTag(feature.GetTag(version));
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));
        var before = _fixture.TakeSnapshot();

        await _fixture.RunVersionTarget(feature);

        var diff = SnapshotDiff.Compare(before, _fixture.TakeSnapshot());
        diff.Changes.Should().BeEmpty();
    }

    [Theory, AutoData]
    public async Task ReleaseChangesOnlyTheExpectedReferences(
        Feature feature,
        Version version)
    {
        await _fixture.OverrideOrigin();
        await _fixture.AddGitTag(feature.GetTag(version));
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));
        var before = _fixture.TakeSnapshot();

        await _fixture.RunReleaseFeature(feature);

        var after = _fixture.TakeSnapshot();
        var head = await _fixture.GetGitHash("HEAD");
        var branch = before.Head;
        var changes = SnapshotDiff.Compare(before, after).Changes;
        using (new AssertionScope())
        {
            before.IsHeadDetached.Should().BeFalse("the release pushes the checked-out branch");
            changes.Should().NotContain(c => c.Category == ChangeCategory.Head, "the release stays on the same branch");
            changes.Should().NotContain(c => c.Category == ChangeCategory.Remote, "the release doesn't change remotes");
            changes.Should().NotContain(c => c.Category == ChangeCategory.Reference);
            changes.Should().NotContain(c => c.Kind == SnapshotChangeKind.Removed);
            changes.Where(c => c.Category == ChangeCategory.Branch).Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new { Kind = SnapshotChangeKind.Modified, Name = branch, After = head });
            changes.Where(c => c.Category == ChangeCategory.Tag).Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new
                {
                    Kind = SnapshotChangeKind.Added,
                    Name = feature.GetTag(version.IncrementMinor()).Name,
                });
            changes.Where(c => c.Category == ChangeCategory.RemoteBranch).Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new { Name = $"origin/{branch}", After = head });
            after.Branches.Single(b => b.Name == branch).TrackedBranch.Should().Be($"origin/{branch}");
        }
    }

    [Theory, AutoData]
    public async Task ReleaseWithoutChangesLeavesReferencesUntouched(
        Feature feature,
        Version version)
    {
        await _fixture.CreateFeatureConfig(feature, version);
        await _fixture.AddAndCommit(CommitMessage.New("feat"), feature.GetRoot(_fixture.RootDirectory));
        await _fixture.AddGitTag(feature.GetTag(version));
        await _fixture.OverrideOrigin();
        var before = _fixture.TakeSnapshot();

        await _fixture.RunReleaseFeature(feature);

        var diff = SnapshotDiff.Compare(before, _fixture.TakeSnapshot());
        diff.Changes.Should().BeEmpty();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _fixture.DisposeAsync();
}
