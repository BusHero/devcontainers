using Nuke.Common;
using Nuke.Common.IO;
using System;
using System.IO;

sealed partial class Build
{
	private AbsolutePath Scripts => RootDirectory / "scripts";
	private AbsolutePath Source => RootDirectory / "templates" / "src";

	[Parameter("Template to build")] private readonly string Template;

	private Target ListTemplates => _ => _
		.Executes(() =>
		{
			var directories = Directory.GetDirectories(Source);
			foreach (var d in directories)
			{
				Console.Write(Path.GetFileName(d) + " ");
			}
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
