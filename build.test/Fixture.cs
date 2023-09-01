using System.Text.Json;
using CliWrap;
using CliWrap.Builders;
using Nuke.Common;
using Nuke.Common.IO;

namespace build.test;

internal sealed class CustomFixture : IAsyncDisposable
{
    private readonly List<Tag> tags = new();
    private readonly List<string> tempFiles = new();
    private string? commitToRestore;

    public async Task SaveCommit(string commit)
    {
        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("rev-parse")
                .Add(commit))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => commitToRestore = x))
            .ExecuteAsync();
    }

    public bool KeepFiles { get; init; }

    public bool KeepTags { get; init; }

    public bool KeepCommits { get; init; }

    public AbsolutePath RootDirectory => NukeBuild.RootDirectory;

    public async ValueTask DisposeAsync()
    {
        if (!KeepTags)
        {
            await DeleteGitTags(this.tags);
        }

        if (!KeepCommits)
        {
            await RevertCommits(commitToRestore);
        }

        if (!KeepFiles)
        {
            RemoveTempFiles(tempFiles);
            await RestoreFiles(tempFiles);
        }
    }

    private async Task RestoreFiles(IReadOnlyCollection<string> files)
    {
        if (files.Count == 0)
        {
            return;
        }

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("restore")
                .Add("--staged")
                .Add("--progress")
                .Add(files))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
    }

    public async Task RevertCommits(string? commit)
    {
        if (string.IsNullOrEmpty(commit))
        {
            return;
        }

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("reset")
                .Add("--no-refresh")
                .Add("--soft")
                .Add(commit))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
    }

    public void RemoveTempFiles(IReadOnlyCollection<string> tempFiles)
    {
        if (tempFiles.Count == 0)
        {
            return;
        }

        foreach (var file in tempFiles)
        {
            if (Directory.Exists(file))
            {
                Directory.Delete(file, true);
            }
            else if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    public void CreateTempDirectory(string path)
    {
        Directory.CreateDirectory(path);
        this.tempFiles.Add(path);
    }

    public void CreateTempFile(string path)
    {
        File.Create(path);
        this.tempFiles.Add(path);
    }

    public async Task CreateFeatureConfig(
        Feature featureName,
        Version version)
    {
        this.CreateTempDirectory(featureName.GetFeatureRoot(RootDirectory));

        var featureConfig = featureName.GetFeatureConfig(RootDirectory);
        var json = $$"""
            {
                "version": "{{version}}",
                "id": "{{featureName}}",
                "name": "{{featureName}}"
            }
            """;

        await File.WriteAllTextAsync(featureConfig, json);
    }

    public async Task<string?> GetVersion(Feature feature)
    {
        var featureConfig = feature.GetFeatureConfig(RootDirectory);
        using var fileStream = File.OpenRead(featureConfig);
        var document = await JsonDocument.ParseAsync(fileStream);

        return document.RootElement.GetProperty("version").GetString();
    }

    public async Task RunBuild(Func<ArgumentsBuilder, ArgumentsBuilder> configure)
    {
        await Cli.Wrap("dotnet")
            .WithArguments(args => configure(args
                    .Add("run")
                    .Add("--project")
                    .Add("/workspaces/devcontainers/build"))
                .Add("--no-logo"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
    }

    public async Task RunCreateReleaseCommitTarget(Feature feature)
    {
        await RunBuild(args => args
            .Add("--target")
            .Add("CreateVersionChangeCommit")
            .Add("--feature")
            .Add(feature));
    }

    public async Task RunVersionTarget(Feature feature)
    {
        await RunBuild(args => args
            .Add("Version")
            .Add("--feature")
            .Add(feature));
    }

    public async Task Commit(
        CommitMessage message,
        string path)
    {
        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("add")
                .Add(path))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("commit")
                .Add("--include")
                .Add(path)
                .Add("--message")
                .Add(message))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
    }

    public async Task AddGitTag(
        Tag tag,
        string commit = "HEAD")
    {
        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("tag")
                .Add(tag)
                .Add(commit))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
        this.tags.Add(tag);
    }

    public async Task DeleteGitTags(IReadOnlyCollection<Tag> tags)
    {
        if (tags.Count is 0)
        {
            return;
        }

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("tag")
                .Add("--delete")
                .Add(tags.Select(x => x.ToString())))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
    }

    public async Task<string> GetLatestCommitMessage()
    {
        var commitMessage = string.Empty;

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("rev-list")
                .Add("HEAD")
                .Add("--pretty=%s")
                .Add("--no-commit-header")
                .Add("-n1"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => commitMessage = x))
            .ExecuteAsync();

        return commitMessage;
    }

    public async Task<string> GetLatestTag(string feature)
    {
        var tag = string.Empty;

        var result = await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("describe")
                .Add("--abbrev=0")
                .Add("--tags")
                .Add("--match")
                .Add($"feature_{feature}*"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => tag = x))
            .ExecuteAsync();

        return tag;
    }

    public async Task<List<string>> GetModifiedFilesLatestCommit(
        string commit = "HEAD")
    {
        var modifiedFiles = new List<string>();

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("diff-tree")
                .Add("--no-commit-id")
                .Add("--name-only")
                .Add("-r")
                .Add(commit))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(modifiedFiles.Add))
            .ExecuteAsync();

        return modifiedFiles;
    }
}

public static class FeatureExtenssions
{
    public static Tag GetTag(this Feature feature) => new($"feature_{feature}");

    public static AbsolutePath GetFeatureRoot(
        this Feature feature,
        AbsolutePath projectRoot) => projectRoot
            / "features"
            / "src"
            / feature;

    public static AbsolutePath GetFeatureConfig(
        this Feature featureName,
        AbsolutePath projectRoot)
        => featureName.GetFeatureRoot(projectRoot)
            / "devcontainer-feature.json";

    public static string GetRelativePathToConfig(this Feature feature)
        => Path.Combine("features", "src", feature, "devcontainer-feature.json");

    public static async Task<string?> GetVersion(
        this Feature feature,
        AbsolutePath projectRoot)
    {
        var featureConfig = feature.GetFeatureConfig(projectRoot);
        using var fileStream = File.OpenRead(featureConfig);
        var document = await JsonDocument.ParseAsync(fileStream);

        return document.RootElement.GetProperty("version").GetString();
    }

    public static void CreateTempFile(
        this Feature feature,
        AbsolutePath root)
    {
        using var _ = File.Create(feature.GetFeatureRoot(root) / $"tmp_{Guid.NewGuid():N}");
    }
}
