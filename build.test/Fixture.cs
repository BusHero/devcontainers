using System.Text.Json;
using CliWrap;
using CliWrap.Builders;
using Nuke.Common;
using Nuke.Common.IO;
using Xunit.Abstractions;

namespace build.test;

internal sealed class CustomFixture(ITestOutputHelper output) : IAsyncDisposable
{
    private readonly List<string> _tempFiles = [];

    private string? _commitToRestore;

    public async Task SaveCommit(string commit)
    {
        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("rev-parse")
                .Add(commit))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => _commitToRestore = x))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
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
            await RevertCommits(_commitToRestore);
        }

        if (!KeepFiles)
        {
            RemoveTempFiles(_tempFiles);
            await RestoreFiles(_tempFiles);
        }

        if (_originalOrigin is not null)
        {
            await ResetOrigin(_originalOrigin);
        }
    }

    private async Task RestoreFiles(IReadOnlyCollection<string> files)
    {
        var repoFiles = files
            .Where(x => x.StartsWith(RootDirectory))
            .ToList();
        if (repoFiles.Count == 0)
        {
            return;
        }

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("restore")
                .Add("--staged")
                .Add("--progress")
                .Add(repoFiles))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
    }

    private async Task RevertCommits(string? commit)
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();
    }

    private void RemoveTempFiles(IReadOnlyCollection<string> tempFiles)
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

    private void CreateTempDirectory(string path)
    {
        Directory.CreateDirectory(path);
        _tempFiles.Add(path);
    }

    public string CreateTempFile()
    {
        var path = RootDirectory / $"tmpFile{Guid.NewGuid():N}";

        CreateTempFile(path);

        return path;
    }

    private void CreateTempFile(string path)
    {
        using var _ = File.Create(path);
        _tempFiles.Add(path);
    }

    public async Task CreateFeatureConfig(
        Feature featureName,
        Version version)
    {
        CreateTempDirectory(featureName.GetRoot(RootDirectory));

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
        await using var fileStream = File.OpenRead(featureConfig);
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

    private async Task RunBuild(
        Func<ArgumentsBuilder, ArgumentsBuilder> configure,
        bool skip = true)
    {
        await Cli.Wrap("dotnet")
            .WithArguments(args =>
            {
                configure(args
                    .Add("run")
                    .Add("--project")
                    .Add((RootDirectory / "build").ToString()))
                .Add("--no-logo");

                if (skip)
                    args.Add("--skip");
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("commit")
                .Add("--include")
                .Add(path)
                .Add("--message")
                .Add(message))
            .WithEnvironmentVariables(env =>
            {
                env.Set("GIT_COMMITTER_NAME", "Bot");
                env.Set("GIT_COMMITTER_EMAIL", "noreply@github.com");
                env.Set("GIT_AUTHOR_NAME", "Bot");
                env.Set("GIT_AUTHOR_EMAIL", "noreply@github.com");
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();
    }

    private async Task DeleteGitTags()
    {
        var tags = new List<string>();

        await Cli.Wrap("git")
            .WithArguments("tag")
            .WithStandardOutputPipe(PipeTarget.ToDelegate(tags.Add))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();

        var tagsToDelete = tags.Except(_originalTags).ToList();
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();
    }

    public async Task<string> GetLatestCommitMessage(
        string? path = null)
    {
        var commitMessage = string.Empty;

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("rev-list")
                .Add("--pretty=%s")
                .Add("--no-commit-header")
                .Add("-n1")
                .Add("HEAD"))
            .WithEnvironmentVariables(env =>
            {
                if (path is not null)
                {
                    env.Set("GIT_DIR", path);
                }
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => commitMessage = x))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();

        return commitMessage;
    }

    public async Task<string> GetLatestTag(
        string feature,
        string? path = null)
    {
        var tag = string.Empty;

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("describe")
                .Add("--abbrev=0")
                .Add("--tags")
                .Add("--always")
                .Add("--match")
                .Add($"feature_{feature}*"))
            .WithEnvironmentVariables(env =>
            {
                if (path is not null)
                {
                    env.Set("GIT_DIR", path);
                }
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => tag = x))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();

        return modifiedFiles;
    }

    private readonly List<string> _originalTags = [];

    internal async Task SaveTags()
    {
        await Cli.Wrap("git")
            .WithArguments("tag")
            .WithStandardOutputPipe(PipeTarget.ToDelegate(_originalTags.Add))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();

        return hash;
    }

    public async Task<string> GetCommitterName(string commit = "HEAD")
    {
        var name = string.Empty;

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("show")
                .Add("-s")
                .Add("--format=%aN")
                .Add(commit))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => name = x))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();

        return name;
    }

    public async Task<string> GetCommiterEmail(string commit = "HEAD")
    {
        var email = string.Empty;

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("show")
                .Add("-s")
                .Add("--format=%aE")
                .Add(commit))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => email = x))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();

        return email;
    }

    private const string REMOTE = "origin";

    private string? _originalOrigin;

    public string GitOriginPath { get; } = Path.Combine("/tmp", $"repo_{Guid.NewGuid():N}");

    public async Task OverrideOrigin()
    {
        CreateTempDirectory(GitOriginPath);

        _originalOrigin = await GetRemoteUrl(REMOTE);

        await SetRemoteUrl(REMOTE, GitOriginPath);

        await CloneBareRepo(GitOriginPath);
    }

    private async Task ResetOrigin(string? url)
    {
        if (url is null)
        {
            return;
        }

        await SetRemoteUrl(REMOTE, url);
    }

    private async Task CloneBareRepo(string path)
    {
        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("clone")
                .Add("--bare")
                .Add("--no-hardlinks")
                .Add("--single-branch")
                .Add(RootDirectory.ToString())
                .Add(path))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();
    }

    private async Task<string> GetRemoteUrl(string remote)
    {
        var remoteUrl = string.Empty;

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("remote")
                .Add("get-url")
                .Add(remote))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => remoteUrl = x))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();

        return remoteUrl;
    }

    private async Task SetRemoteUrl(
        string remote,
        string url)
    {
        ArgumentNullException.ThrowIfNull(remote);
        ArgumentNullException.ThrowIfNull(url);

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("remote")
                .Add("set-url")
                .Add(remote)
                .Add(url))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(output.WriteLine))
            .ExecuteAsync();
    }
}
