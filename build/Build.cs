using Nuke.Common;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.Npm.NpmTasks;
using Nuke.Common.Tools.Npm;

class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.Compile);

	[PathExecutable("bash")] readonly Tool Bash;

	[Parameter("Template to build")] readonly string Template;

	Target InstallDevContainersCli => _ => _
		.Executes(() => NpmInstall(_ => _
			.SetPackages("@devcontainers/cli")
			.SetGlobal(true)));

	Target Compile => _ => _
		.Requires(() => Template)
		.DependsOn(InstallDevContainersCli)
		.Executes(() => Bash($"./scripts/build.sh {Template}"));
}
