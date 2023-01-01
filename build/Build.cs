using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.Npm.NpmTasks;
using Nuke.Common.Tools.Npm;

sealed partial class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.BuildTemplate);

	[PathExecutable("bash")] private readonly Tool Bash;
	private Tool Devcontainer => ToolResolver.GetPathTool("devcontainer");

	[Parameter("Template to build")] private readonly string Template;

	private AbsolutePath Scripts => RootDirectory / "scripts";
	private AbsolutePath Source => RootDirectory / "src";

	private Target BuildTemplate => _ => _
		.Requires(() => Template)
		.Triggers(TestTemplate)
		.Executes(() => Bash($"{Scripts / "build.sh"} {Template}"));

	private Target TestTemplate => _ => _
		.Requires(() => Template)
		.Executes(() => Bash($"{Scripts / "test.sh"} {Template}"));

	private Target InstallDevcontainer => _ => _
		.Unlisted()
		.Executes(() => NpmInstall(_ => _
			.AddPackages("@devcontainers/cli")
			.SetGlobal(true)));

	private Target PublishTemplate => _ => _
		.DependsOn(InstallDevcontainer)
		.Executes(() =>
		{
			return Devcontainer($"templates publish {Source} --namespace BusHero/devcontainer-template-test/features");
		});
}
