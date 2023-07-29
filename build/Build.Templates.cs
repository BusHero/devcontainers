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

	private Target ListTemplates => _ => _
	 	.Requires(() => GithubOutput)
		.Executes(() =>
		{
			var templates = string.Join(" ", Templates);
			File.WriteAllText(GithubOutput, $"templates={templates}\n");
			Log.Information("templates={Templates}, GithubOutput={GithubOutput}", templates, GithubOutput);
			Log.Information("GithubOutput content: {Output}", File.ReadAllText(GithubOutput));
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
