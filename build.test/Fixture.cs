using System.Text.Json;
using CliWrap;
using CliWrap.Builders;
using Nuke.Common;
using Nuke.Common.IO;

namespace build.test;

internal sealed class CustomFixture : IAsyncDisposable
{
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
            await DeleteGitTags();
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

    public string CreateTempFile()
    {
        var path = RootDirectory / $"tmpFile{Guid.NewGuid():N}";

        CreateTempFile(path);

        return path;
    }

    public void CreateTempFile(string path)
    {
        using var _ = File.Create(path);
        this.tempFiles.Add(path);
    }

    public async Task CreateFeatureConfig(
        Feature featureName,
        Version version)
    {
        this.CreateTempDirectory(featureName.GetRoot(RootDirectory));

        var featureConfig = featureName.GetConfig(RootDirectory);
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
        var featureConfig = feature.GetConfig(RootDirectory);
        using var fileStream = File.OpenRead(featureConfig);
        var document = await JsonDocument.ParseAsync(fileStream);

        return document.RootElement.GetProperty("version").GetString();
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

    public async Task RunCreateReleaseTagTarget(Feature feature)
    {
        await RunBuild(args => args
            .Add("CreateReleaseTag")
            .Add("--feature")
            .Add(feature));
    }

    public async Task RunGenerateDocumentationTarget(Feature feature)
    {
        await RunBuild(args => args
            .Add("GenerateDocumentationFeature")
            .Add("--feature")
            .Add(feature));
    }

    public async Task RunReleaseFeature(Feature feature)
    {
        await RunBuild(args => args
            .Add("ReleaseFeature")
            .Add("--feature")
            .Add(feature), false);
    }

    public async Task RunBuild(
        Func<ArgumentsBuilder, ArgumentsBuilder> configure,
        bool skip = true)
    {
        await Cli.Wrap("dotnet")
            .WithArguments(args =>
            {
                configure(args
                    .Add("run")
                    .Add("--project")
                    .Add("/workspaces/devcontainers/build"))
                .Add("--no-logo");

                if (skip)
                    args.Add("--skip");
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
    }

    public async Task AddAndCommit(
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
    }

    public async Task DeleteGitTags()
    {
        var tags = new List<string>();

        await Cli.Wrap("git")
            .WithArguments("tag")
            .WithStandardOutputPipe(PipeTarget.ToDelegate(tags.Add))
            .ExecuteAsync();

        var tagsToDelete = tags.Except(originalTags).ToList();
        if (tagsToDelete.Count == 0)
        {
            return;
        }

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("tag")
                .Add("--delete")
                .Add(tagsToDelete))
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

    private readonly List<string> originalTags = new();

    internal async Task SaveTags()
    {
        await Cli.Wrap("git")
            .WithArguments("tag")
            .WithStandardOutputPipe(PipeTarget.ToDelegate(originalTags.Add))
            .ExecuteAsync();
    }

    public async Task<string> GetGitHash(string latestGitTag)
    {
        var hash = string.Empty;

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("rev-parse")
                .Add(latestGitTag))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => hash = x))
            .ExecuteAsync();

        return hash;
    }
}
