using System.Text.Json;

using Nuke.Common;
using Nuke.Common.IO;

using Serilog;

sealed partial class Build
{
	private AbsolutePath FeaturesRoot => RootDirectory / "features";

	private RelativePath RelativeFeatureRoot => RootDirectory.GetRelativePathTo(FeaturesRoot) / "src" / Feature;

	[Parameter] private readonly bool SkipAutoGenerated;

	[Parameter] private readonly bool All = false;

	[Parameter("Feature to build")] private readonly string Feature = null!;

	private Target TestFeature => _ => _
		.Requires(() => Feature)
		.Executes(() =>
		{
			var commands = new List<string>(10)
			{
				"features",
				"test",
				"--features",
				Feature,
				"--project-folder",
				FeaturesRoot
			};

			var files = Directory
				.GetFiles(FeaturesRoot / "test" / Feature)
				.Select(Path.GetFileName)
				.ToList();
			Log.Information("{files}", JsonSerializer.Serialize(files));

			if (SkipAutoGenerated || !files.Contains("test.sh"))
			{
				commands.Add("--skip-autogenerated");
			}
			else
			{
				commands.Add("-i");
				commands.Add("alpine");
			}

			if (!files.Contains("scenarios.json"))
			{
				commands.Add("--skip-scenarios");
			}
			if (!files.Contains("duplicate.sh"))
			{
				commands.Add("--skip-duplicated");
			}
			var command = string.Join(" ", commands);
			Log.Information("{command}", command);

			Devcontainer(command, customLogger: DevcontainerLog);
		});


	private Target PublishFeatures => _ => _
		.Executes(() =>
		{
			return Devcontainer($"features publish {FeaturesRoot / "src"} --namespace {GitHubNamespace}/features");
		});

	private Target GetFeatures => _ => _
		.Requires(() => GithubOutput)
		.Executes(async () =>
		{
			var features = Directory
				.GetDirectories(FeaturesRoot / "src")
				.Select(x => RootDirectory.GetRelativePathTo(x))
				.Where(feature => All || Changes.Any(change => change.StartsWith(feature)))
				.Select(x => x.ToString())
				.Select(Path.GetFileName)
				.ToList();

			if (features.Count is 0)
			{
				Log.Information("No changes to features");
				return;
			}

			var featuresStr = JsonSerializer.Serialize(features);
			Log.Information("Updated Features: {Features}", features);
			await OutputToGithub("features", featuresStr);
		});
}
