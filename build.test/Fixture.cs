using System.Text.Json;
using CliWrap;
using CliWrap.Builders;
using GitSnapshotter;
using Nuke.Common;
using Nuke.Common.IO;
using Xunit.Abstractions;

namespace build.test;

/// <summary>
/// Runs build targets against this repository and puts the repository back afterwards.
/// </summary>
/// <remarks>
/// The snapshot taken on construction records HEAD, branches and their upstreams, remotes and their URLs,
/// tags and the working tree (tracked and untracked, non-ignored files). Disposing the fixture restores all of it,
/// so commits, tags, upstream configuration, the overridden origin URL and any files a test created are undone,
/// whether the test passed or failed.
/// </remarks>
internal sealed class CustomFixture : IAsyncDisposable
{
    private static readonly SnapshotOptions WithWorkingTree = new() { IncludeWorkingTree = true };

    private static readonly RestoreOptions DiscardChanges = new() { Force = true };

    private readonly ITestOutputHelper _output;

    private readonly SnapshotScope _scope;

    private readonly List<string> _createdDirectories = [];

    public CustomFixture(ITestOutputHelper output)
    {
        _output = output;
        _scope = GitRepository.Preserve(RootDirectory, WithWorkingTree, DiscardChanges);
    }

    public AbsolutePath RootDirectory => NukeBuild.RootDirectory;

    /// <summary>Takes a snapshot of the repository's references, without the working tree.</summary>
    public GitRepositorySnapshot TakeSnapshot() => GitRepository.GetSnapshot(RootDirectory);

    public ValueTask DisposeAsync()
    {
        try
        {
            _scope.Dispose();
        }
        finally
        {
            // Restoring deletes the files a test created but not the folders they were in. An empty folder under
            // features/src would still be picked up as a feature, so remove the ones this fixture created.
            foreach (var directory in _createdDirectories)
            {
                DeleteIfEmpty(directory);
            }

            if (Directory.Exists(GitOriginPath))
            {
                Directory.Delete(GitOriginPath, true);
            }
        }

        return ValueTask.CompletedTask;
    }

    private static void DeleteIfEmpty(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var child in Directory.GetDirectories(directory))
        {
            DeleteIfEmpty(child);
        }

        if (!Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }

    public string CreateTempFile()
    {
        var path = RootDirectory / $"tmpFile{Guid.NewGuid():N}";

        using var _ = File.Create(path);

        return path;
    }

    public async Task CreateFeatureConfig(
        Feature featureName,
        Version version)
    {
        var featureRoot = featureName.GetRoot(RootDirectory);
        if (!Directory.Exists(featureRoot))
        {
            Directory.CreateDirectory(featureRoot);
            _createdDirectories.Add(featureRoot);
        }

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
                    .Add(RootDirectory / "build"))
                .Add("--no-logo");

                if (skip)
                    args.Add("--skip");
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
            .ExecuteAsync();

        return modifiedFiles;
    }

    public async Task<string> GetGitHash(string latestGitTag)
    {
        var hash = string.Empty;

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("rev-parse")
                .Add(latestGitTag))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => hash = x))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
            .ExecuteAsync();

        return email;
    }

    private const string REMOTE = "origin";

    public string GitOriginPath { get; } = Path.Combine(Path.GetTempPath(), $"repo_{Guid.NewGuid():N}");

    /// <summary>
    /// Points <c>origin</c> at a fresh bare clone of this repository, so the release can push without reaching GitHub.
    /// The original URL comes back when the fixture is disposed.
    /// </summary>
    public async Task OverrideOrigin()
    {
        await SetRemoteUrl(REMOTE, GitOriginPath);

        await CloneBareRepo(GitOriginPath);
    }

    private async Task CloneBareRepo(string path)
    {
        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("clone")
                .Add("--bare")
                .Add("--no-hardlinks")
                .Add("--single-branch")
                .Add(RootDirectory)
                .Add(path))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
            .ExecuteAsync();
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
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_output.WriteLine))
            .ExecuteAsync();
    }
}
