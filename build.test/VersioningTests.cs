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
        await fixture.Commit(CommitMessage.New("feat"), feature.GetFeatureRoot(fixture.RootDirectory));
        feature.CreateTempFile(fixture.RootDirectory);
        await fixture.Commit(CommitMessage.New("chore"), feature.GetFeatureRoot(fixture.RootDirectory));

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
        await fixture.Commit(CommitMessage.New("chore"), feature.GetFeatureRoot(fixture.RootDirectory));
        feature.CreateTempFile(fixture.RootDirectory);
        await fixture.Commit(CommitMessage.New("chore"), feature.GetFeatureRoot(fixture.RootDirectory));

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
        await fixture.Commit(CommitMessage.New("feat"), feature.GetFeatureRoot(fixture.RootDirectory));
        await fixture.AddGitTag(wrongTag);
        feature.CreateTempFile(fixture.RootDirectory);
        await fixture.Commit(CommitMessage.New("chore"), feature.GetFeatureRoot(fixture.RootDirectory));

        await fixture.RunVersionTarget(feature);

        var newVersion = await fixture.GetVersion(feature);

        newVersion
            .Should()
            .Be(version.IncrementMajor());
    }

    [Theory, AutoData]
    public async Task UseTheRightCommit(
        Feature feature,
        string tempFileName,
        Version version)
    {
        await fixture.AddGitTag(feature.GetTag());
        await fixture.CreateFeatureConfig(feature, version);
        await fixture.Commit(CommitMessage.New("chore"), feature.GetFeatureRoot(fixture.RootDirectory));
        fixture.CreateTempFile(fixture.RootDirectory / tempFileName);
        await fixture.Commit(CommitMessage.New("feat"), fixture.RootDirectory / tempFileName);

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


public sealed class Feature
{
    public Feature(string name) => Name = name.Replace("-", string.Empty);

    public string Name { get; }

    public override string ToString() => Name;

    public static implicit operator string(Feature feature) => feature.ToString();
}

public sealed record Version(int Major, int Minor, int Build)
{
    public override string ToString() => $"{Major}.{Minor}.{Build}";

    public static implicit operator string(Version version) => version.ToString();
}

public static class VersionExtenssions
{
    public static Version IncrementMajor(this Version version) => version with
    {
        Major = version.Major + 1,
        Minor = 0,
        Build = 0
    };

    public static Version IncrementMinor(this Version version) => version with
    {
        Minor = version.Minor + 1,
        Build = 0
    };
}

public class CommitMessage
{
    public static CommitMessage New(string type) => new()
    {
        Type = type,
        Message = Guid.NewGuid().ToString("N")
    };

    public required string Type { get; init; }

    public required string Message { get; init; }

    public override string ToString() => $"{Type}: {Message}";

    public static implicit operator string(CommitMessage commitMessage) => commitMessage.ToString();
}

public sealed record Tag(string Name)
{
    public override string ToString()
    {
        return Name;
    }

    public static implicit operator string(Tag tag) => tag.ToString();
}
