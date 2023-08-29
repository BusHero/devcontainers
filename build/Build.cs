using System.Text.Json.Nodes;

using Nuke.Common;
using Nuke.Common.Tooling;
using CliWrap;
using Serilog;

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

            var latestGitTag = await GetLatestTag();
            var latestGitMessage = await GetLatestCommitMessage();
            var commits = latestGitTag switch
            {
                null => new List<string>(),
                _ => await GetCommitsTillTag(latestGitTag)
            };
            Log.Information("latest git tag: {latestGitTag}", latestGitTag);
            Log.Information("commits {commits}", commits);
            Log.Information("latest git commit {commit}", latestGitMessage);

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

    private Target Foo => _ => _
        .Executes(async () =>
        {
            var message = await GetLatestCommitMessage();
            Log.Information("{tags}", message);
        });

    private async Task<string?> GetLatestTag()
    {
        var tags = new List<string>();

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("describe")
                .Add("--abbrev=0")
                .Add("--tags"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(tags.Add))
            .ExecuteAsync();

        return tags.FirstOrDefault();
    }

    private async Task<List<string>> GetCommitsTillTag(string tag)
    {
        Log.Information("{msg}", "Here is nice");
        var commits = new List<string>();

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("rev-list")
                .Add($"{tag}..HEAD")
                .Add("--pretty=%s")
                .Add("--no-commit-header"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(commits.Add))
            .ExecuteAsync();

        return commits;
    }

    private async Task<string> GetLatestCommitMessage()
    {
        var output = new List<string>();

        await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("rev-list")
                .Add("HEAD")
                .Add("--pretty=%s")
                .Add("--no-commit-header")
                .Add("-n1"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(output.Add))
            .ExecuteAsync();

        return output[0];
    }

    private async Task<List<string>> GetGitTags()
    {
        var tags = new List<string>();

        var foo = await Cli.Wrap("git")
            .WithArguments(args => args
                .Add("tag"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(tags.Add))
            .ExecuteAsync();

        return tags;
    }
}
