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
            var latestGitMessage = await GetLatestCommitMessage();
            if (latestGitMessage.StartsWith("feat:"))
            {
                version = version.IncrementMajor();
            }
            else
            {
                version = version.IncrementMinor();
            }

            document["version"] = version.ToString();

            var outputJson = document.ToJsonString();
            await File.WriteAllTextAsync(PathToFeatureDefinition, outputJson);
        });

    private Target Foo => _ => _
        .Executes(async () =>
        {
            var message = await GetLatestCommitMessage();
            Log.Information("{tags}", message);
        });

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
