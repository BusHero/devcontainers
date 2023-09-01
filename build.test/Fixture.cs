using System.Text.Json;
using CliWrap;
using CliWrap.Builders;
using Nuke.Common;
using Nuke.Common.IO;

namespace build.test;

internal sealed class CustomFixture : IAsyncDisposable
{
    private readonly List<string> tags = new();
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

    public string GetTagForFeature(string feature) => $"feature_{feature}";

    public async Task RevertCommits(string commit)
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
        string featureName,
        int major,
        int minor,
        int build)
    {
        this.CreateTempDirectory(GetFeatureRoot(featureName));

        var featureConfig = GetFeatureConfig(featureName);
        var json = $$"""
            {
                "version": "{{major}}.{{minor}}.{{build}}",
                "id": "{{featureName}}",
                "name": "{{featureName}}"
            }
            """;

        await File.WriteAllTextAsync(featureConfig, json);
    }

    public AbsolutePath GetFeatureRoot(string featureName)
        => NukeBuild.RootDirectory
            / "features"
            / "src"
            / featureName;

    public AbsolutePath GetFeatureConfig(string featureName)
        => GetFeatureRoot(featureName)
            / "devcontainer-feature.json";

    public async Task<string?> GetVersion(string feature)
    {
        var featureConfig = this.GetFeatureConfig(feature);
        using var fileStream = File.OpenRead(featureConfig);
        var document = await JsonDocument.ParseAsync(fileStream);

        return document.RootElement.GetProperty("version").GetString();
    }

    public async Task RunBuild(Action<ArgumentsBuilder> configure)
    {
        await Cli.Wrap("dotnet")
            .WithArguments(args => configure(args
                .Add("run")
                .Add("--project")
                .Add("/workspaces/devcontainers/build")
                .Add("--no-logo")))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
    }

    public async Task RunVersionTarget(string feature)
    {
        await RunBuild(args => args
            .Add("Version")
            .Add("--feature")
            .Add(feature));
    }

    public async Task Commit(
        string path,
        string message)
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
        string tag,
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

    public async Task DeleteGitTags(IReadOnlyCollection<string> tags)
    {
        if (tags.Count is 0)
        {
            return;
        }

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("tag")
                .Add("--delete")
                .Add(tags))
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

public static class EnumerableExtenssions
{
    public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
    {
        foreach (var item in items)
        {
            action(item);
        }
    }

    public static void ForEach<T, TReturn>(
        this IEnumerable<T> items,
        Func<T, TReturn> action)
        => items.ForEach(x => { action(x); });
}
