using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

using CliWrap;

using JetBrains.Annotations;

using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

using Serilog;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "AllUnderscoreLocalParameterName")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public sealed partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Version);

    private bool ShouldUpdateVersion = true;

    [PathExecutable("bash")] private readonly Tool Bash = null!;

    private Version GetFeatureVersion(string pathToFeatureDefinition)
    {
        var json = File.ReadAllText(pathToFeatureDefinition);
        var feature = JsonSerializer.Deserialize<Feature>(json);
        var version = feature?.Version!;

        return new Version(version);
    }

    private Target CheckChangesToNuke => _ => _
        .Requires(() => GithubOutput)
        .Executes(async () =>
        {
            if (Changes.Any(x => x.StartsWith("build")))
            {
                Log.Information("Changes to nuke: {status}", true);
                await OutputToGithub("changesToNuke", "true");
            }
            else
            {
                Log.Information("Changes to nuke: {status}", false);
                await OutputToGithub("changesToNuke", "false");
            }
        });

    [UsedImplicitly]
    public Target ReleaseFeature => _ => _
        .Triggers(Version)
        .Requires(() => Feature);

    public Target CreateReleaseTag => _ => _
        .Requires(() => Feature)
        .Triggers(PushToMain)
        .OnlyWhenDynamic(() => ShouldUpdateVersion)
        .Executes(async () =>
        {
            var version = GetFeatureVersion(PathToFeatureDefinition);
            var tag = $"feature_{Feature}_{version}";
            Log.Information("New tag: {tag}", tag);

            await Cli.Wrap("git")
                .WithArguments(args => args
                    .Add("tag")
                    .Add(tag))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(x => Log.Information("{msg}", x)))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(x => Log.Error("{msg}", x)))
                .ExecuteAsync();
        });

    private Target PushToMain => _ => _
        .OnlyWhenDynamic(() => ShouldUpdateVersion)
        .Executes(async () =>
        {
            var branch = string.Empty;
            await Cli.Wrap("git")
                .WithArguments(args => args
                    .Add("branch")
                    .Add("--show-current"))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(x => branch = x))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(x => Log.Information("{msg}", x)))
                .ExecuteAsync();

            await Cli.Wrap("git")
                .WithArguments(args => args
                    .Add("push")
                    .Add("--set-upstream")
                    .Add("origin")
                    .Add(branch))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(x => Log.Information("{msg}", x)))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(x => Log.Information("{msg}", x)))
                .ExecuteAsync();

            await Cli.Wrap("git")
                .WithArguments(args => args
                    .Add("push")
                    .Add("--tags"))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(x => Log.Information("{msg}", x)))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(x => Log.Information("{msg}", x)))
                .ExecuteAsync();
        });

    public Target CreateVersionChangeCommit => _ => _
        .Requires(() => Feature)
        .Triggers(CreateReleaseTag)
        .OnlyWhenDynamic(() => ShouldUpdateVersion)
        .Executes(async () =>
        {
            var version = GetFeatureVersion(PathToFeatureDefinition);

            await Cli.Wrap("git")
                .WithArguments(args => args
                    .Add("add")
                    .Add(FeatureRoot))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(x => Log.Information("{git_msg}", x)))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(x => Log.Error("{git_msg}", x)))
                .ExecuteAsync();

            await Cli.Wrap("git")
                .WithArguments(args => args
                    .Add("commit")
                    .Add("--include")
                    .Add(FeatureRoot)
                    .Add("--message")
                    .Add($"Release: feature {Feature} {version}"))
                .WithEnvironmentVariables(dict =>
                {
                    dict.Set("GIT_COMMITTER_NAME", "Release Bot");
                    dict.Set("GIT_COMMITTER_EMAIL", "noreply@github.com");
                    dict.Set("GIT_AUTHOR_NAME", "Release Bot");
                    dict.Set("GIT_AUTHOR_EMAIL", "noreply@github.com");
                })
                .WithStandardOutputPipe(PipeTarget.ToDelegate(x => Log.Information("{git_msg}", x)))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(x => Log.Error("{git_msg}", x)))
                .ExecuteAsync();
        });

    public Target Version => _ => _
        .Requires(() => Feature)
        .Triggers(GenerateDocumentationFeature)
        .Executes(async () =>
        {
            var json = await File.ReadAllTextAsync(PathToFeatureDefinition);
            var document = JsonNode.Parse(json)
                           ?? throw new InvalidOperationException(
                               $"{PathToFeatureDefinition} is not a valid json document");

            var versionJsonElement = document.Root["version"];

            var version = new Version(versionJsonElement!.GetValue<string>());
            Log.Information("old version: {version}", version);

            var latestGitTag = await GetLatestTag(Feature);
            if (latestGitTag is null)
            {
                Log.Warning("No previous tag found. Abort the Target");
                return;
            }

            var commits = await GetCommitsTillTag(latestGitTag, RelativeFeatureRoot);
            if (commits is { Count: 0 })
            {
                ShouldUpdateVersion = false;
                return;
            }

            version = commits.Any(x => x.StartsWith("feat:")) 
                ? version.IncrementMinor() 
                : version.IncrementBuild();

            document["version"] = version.ToString();
            Log.Information("new version: {version}", version);

            var outputJson = document.ToJsonString();
            await File.WriteAllTextAsync(PathToFeatureDefinition, outputJson);
        });

    private async Task<string?> GetLatestTag(string feature)
    {
        var tag = default(string);

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("describe")
                .Add("--abbrev=0")
                .Add("--tags")
                .Add("--match")
                .Add($"feature_{feature}*"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => tag = x))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        return tag;
    }

    private async Task<List<string>> GetCommitsTillTag(
        string tag,
        RelativePath path)
    {
        var commits = new List<string>();

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("rev-list")
                .Add($"{tag}..HEAD")
                .Add("--no-commit-header"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(commits.Add))
            .ExecuteAsync();

        var commitMessages = new List<string>();
        foreach (var commit in commits)
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

            if (!modifiedFiles.Any(x => x.StartsWith(path)))
            {
                continue;
            }

            await Cli.Wrap("git")
                .WithArguments(args => args
                    .Add("rev-list")
                    .Add("--max-count=1")
                    .Add("--no-commit-header")
                    .Add("--format=%s")
                    .Add(commit))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(commitMessages.Add))
                .ExecuteAsync();
        }

        return commitMessages;
    }
}