using System.Text.Json;
using CliWrap;

namespace build.test;

public class VersioningTests : IAsyncLifetime
{
    private const bool DELETE_FILES = true;
    private const bool ROLLBACK_COMMITS = true;
    private const bool DELETE_TAG = true;

    private string? DirectoryToCleanUp;

    private string? TagToRemove;

    private int commitsCount = 0;

    [Theory]
    [AutoData]
    public async Task TwoCommits_FeatAndChore_UpdatesMajor(
        string feature,
        int major,
        int minor,
        int build,
        string gitTag,
        string message)
    {
        feature = feature.Replace("-", "");
        gitTag = gitTag.Replace("-", "");
        (major, minor, build) = (Math.Abs(major), Math.Abs(minor), Math.Abs(build));
        var featureRoot = Nuke.Common.NukeBuild.RootDirectory
            / "features"
            / "src"
            / feature;
        var featureFile = featureRoot
            / "devcontainer-feature.json";

        await AddGitTag(gitTag);

        await CreateFeatureFile(featureFile, major, minor, build);
        await Commit(featureRoot, $"feat: {message}");
        File.WriteAllText(featureRoot / "foo", string.Empty);
        await Commit(featureRoot, $"chore: {message}");

        await RunBuild(feature);

        var version = await GetVersion(featureFile);

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
        string gitTag,
        string message)
    {
        feature = feature.Replace("-", "");
        gitTag = gitTag.Replace("-", "");
        (major, minor, build) = (Math.Abs(major), Math.Abs(minor), Math.Abs(build));
        var featureRoot = Nuke.Common.NukeBuild.RootDirectory
            / "features"
            / "src"
            / feature;
        var featureFile = featureRoot
            / "devcontainer-feature.json";

        await AddGitTag(gitTag);

        await CreateFeatureFile(featureFile, major, minor, build);
        await Commit(featureRoot, $"chore: {message}");
        File.WriteAllText(featureRoot / "foo", string.Empty);
        await Commit(featureRoot, $"chore: {message}");

        await RunBuild(feature);

        var version = await GetVersion(featureFile);

        version
            .Should()
            .Be($"{major}.{minor + 1}.0");
    }

    private async Task CreateFeatureFile(
        string pathToFeature,
        int major,
        int minor,
        int build)
    {
        var directoryName = Path.GetDirectoryName(pathToFeature);

        Directory.CreateDirectory(directoryName!);

        await File.WriteAllTextAsync(
            pathToFeature,
            $$"""{ "version": "{{major}}.{{minor}}.{{build}}" }""");

        DirectoryToCleanUp = directoryName;
    }

    private async Task<string?> GetVersion(string path)
    {
        using var fileStream = File.OpenRead(path);
        var document = await JsonDocument.ParseAsync(fileStream);

        return document.RootElement.GetProperty("version").GetString();
    }

    private async Task RunBuild(string feature)
    {
        await Cli.Wrap("dotnet")
            .WithArguments(args => args
                .Add("run")
                .Add("--project")
                .Add("/workspaces/devcontainers/build")
                .Add("Version")
                .Add("--feature")
                .Add(feature)
                .Add("--no-logo")
                .Add("--verbosity")
                .Add("Quiet"))
            .ExecuteAsync();
    }

    private async Task Commit(
        string path,
        string message)
    {
        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("add")
                .Add(path))
            .ExecuteAsync();

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("commit")
                .Add("--include")
                .Add(path)
                .Add("--message")
                .Add(message)
                .Add("--quiet"))
            .ExecuteAsync();

        commitsCount++;
    }

    private async Task AddGitTag(string tag)
    {
        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("tag")
                .Add(tag)
                .Add("HEAD"))
            .ExecuteAsync();
        TagToRemove = tag;
    }

    private async Task RemoveTag(string tag)
    {
        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("tag")
                .Add("--delete")
                .Add(tag))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
    }

    public async Task InitializeAsync() => await Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (DELETE_TAG && TagToRemove is not null)
            await RemoveTag(TagToRemove);

        if (ROLLBACK_COMMITS && commitsCount > 0)
        {
            await Cli.Wrap("git")
                .WithArguments(args => args
                    .Add("reset")
                    .Add("--no-refresh")
                    .Add("--soft")
                    .Add($"HEAD~{commitsCount}")
                    .Add("--quiet"))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
                .ExecuteAsync();
        }

        if (DELETE_FILES)
            RemoveTempDirectory(DirectoryToCleanUp);
    }

    private void RemoveTempDirectory(string? directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
    }
}
