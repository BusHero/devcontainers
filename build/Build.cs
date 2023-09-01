using System.Text.Json.Nodes;

using Nuke.Common;
using Nuke.Common.Tooling;
using CliWrap;
using Serilog;
using Nuke.Common.IO;

public sealed partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Version);

    [PathExecutable("bash")] private readonly Tool Bash = null!;

    public Target Version => _ => _
        .Requires(() => Feature)
        .Executes(async () =>
        {
            var json = await File.ReadAllTextAsync(PathToFeatureDefinition);
            var document = JsonNode.Parse(json)
                ?? throw new InvalidOperationException($"{PathToFeatureDefinition} is not a valid json document");

            var versionJsonElement = document.Root["version"];

            var version = new Version(versionJsonElement!.GetValue<string>());
            Log.Information("old version: {version}", version);

            var latestGitTag = await GetLatestTag(Feature);

            var commits = latestGitTag switch
            {
                null => new List<string>(),
                _ => await GetCommitsTillTag(latestGitTag, RelativeFeatureRoot)
            };
            if (commits.Any(x => x.StartsWith("feat:")))
            {
                version = version.IncrementMajor();
            }
            else
            {
                version = version.IncrementMinor();
            }

            document["version"] = version.ToString();
            Log.Information("new version: {version}", version);

            var outputJson = document.ToJsonString();
            await File.WriteAllTextAsync(PathToFeatureDefinition, outputJson);
        });

    private async Task<string?> GetLatestTag(string feature)
    {
        var tags = new List<string>();

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("describe")
                .Add("--abbrev=0")
                .Add("--tags")
                .Add("--match")
                .Add($"feature_{feature}*"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(tags.Add))
            .ExecuteAsync();

        return tags.FirstOrDefault();
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
