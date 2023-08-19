using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;

sealed partial class Build
{
	[Parameter] private readonly bool SkipAutoGenerated;

	[Parameter("Feature to build")] private readonly string Feature;

	private AbsolutePath PathToFeatures => RootDirectory / "features";

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
				PathToFeatures
			};

			var files = Directory
				.GetFiles(PathToFeatures / "test" / Feature)
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
			return Devcontainer($"features publish {PathToFeatures / "src"} --namespace {GitHubNamespace}/features");
		});
}
