using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;

sealed partial class Build
{
	private AbsolutePath TemplatesRoot => RootDirectory / "templates";

	[Parameter("Template to build")] private readonly string Template = null!;

	private Target GetTemplates => _ => _
		.Requires(() => GithubOutput)
		.Executes(async () =>
		{
			var templates = Directory
				.GetDirectories(TemplatesRoot / "src")
				.Select(x => RootDirectory.GetRelativePathTo(x))
				.Where(feature => Changes.Any(change => change.StartsWith(feature)))
				.Select(x => x.ToString())
				.Select(Path.GetFileName)
				.ToList();

			if (templates.Count is 0)
			{
				Log.Information("No updated templates");
				return;
			}


			var templatesStr = JsonSerializer.Serialize(templates);
			Log.Information("Updated templates: {Templates}", templatesStr);
			await OutputToGithub("templates", templatesStr);
		});

	private Target BuildTemplate => _ => _
		.Requires(() => Template)
		.Triggers(TestTemplate)
		.Executes(() => Bash($"{Scripts / "build.sh"} {Template}"));

	private Target TestTemplate => _ => _
		.Requires(() => Template)
		.Executes(() => Bash($"{Scripts / "test.sh"} {Template}"));

	private Target PublishTemplate => _ => _
		.Executes(() =>
		{
			return Devcontainer($"templates publish {TemplatesRoot} --namespace {GitHubNamespace}/templates");
		});
}
