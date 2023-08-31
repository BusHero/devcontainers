using System.Text.Json;
using CliWrap;
using Fare;
using Nuke.Common;
using Nuke.Common.IO;

namespace build.test;

internal sealed class CustomFixture : IAsyncDisposable
{
    private readonly List<string> tags = new();
    private readonly List<string> tempFiles = new();

    private int commitsCount = 0;

    public bool KeepFiles { get; init; }

    public bool KeepTags { get; init; }

    public bool KeepCommits { get; init; }

    public async ValueTask DisposeAsync()
    {
        if (!KeepFiles)
        {
            RemoveDirectory(tempFiles);
        }

        if (!KeepTags)
        {
            await DeleteGitTags(this.tags);
        }

        if (!KeepCommits)
        {
            await RevertCommits(commitsCount);
        }
    }

    public string GetTagForFeature(string feature) => $"feature_{feature}";

    public async Task RevertCommits(int numberOfCommits)
    {
        if (numberOfCommits <= 0)
        {
            return;
        }

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("reset")
                .Add("--no-refresh")
                .Add("--soft")
                .Add($"HEAD~{numberOfCommits}")
                .Add("--quiet"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
    }

    public void RemoveDirectory(IReadOnlyCollection<string> tempFiles)
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

        await File.WriteAllTextAsync(
            GetFeatureConfig(featureName),
            $$"""{ "version": "{{major}}.{{minor}}.{{build}}" }""");
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

    public async Task RunBuild(string feature)
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

    public async Task Commit(
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

        this.commitsCount++;
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
            .WithArguments(args =>
            {
                args
                    .Add("tag")
                    .Add("--delete");

                foreach (var tag in tags)
                {
                    args.Add(tag);
                }
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
    }
}
