using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;

sealed partial class Build
{
	private AbsolutePath Scripts => RootDirectory / "scripts";
	private AbsolutePath Source => RootDirectory / "templates" / "src";

	[Parameter("Github Output")] private readonly string GithubOutput = null!;

	[Parameter("Template to build")] private readonly string Template = null!;

	private List<string?> Templates => Directory
		.GetDirectories(Source)
		.Select(Path.GetFileName)
		.ToList();

	private List<string?> Features => Directory
		.GetDirectories(RootDirectory / "features" / "src")
		.Select(Path.GetFileName)
		.ToList();

	private Target ListTemplates => _ => _
	 	.Requires(() => GithubOutput)
		.Executes(() =>
		{
			var templates = JsonSerializer.Serialize(Templates);
			Log.Information("templates={Templates}", templates);
			var features = JsonSerializer.Serialize(Features);
			Log.Information("features={Features}", features);

			File.WriteAllLines(
				GithubOutput,
				new[]
				{
					$"templates={templates}",
					$"features={features}"
				});
		});

	private Target BuildTemplate => _ => _
		.Requires(() => Template)
		.Triggers(TestTemplate)
		.Executes(() => Bash($"{Scripts / "build.sh"} {Template}"));

	private Target TestTemplate => _ => _
		.Requires(() => Template)
		.Executes(() => Bash($"{Scripts / "test.sh"} {Template}"));

	private Target PublishTemplate => _ => _
		.DependsOn(InstallDevcontainer)
		.Executes(() =>
		{
			return Devcontainer($"templates publish {Source} --namespace {GitHubNamespace}/templates");
		});
}
