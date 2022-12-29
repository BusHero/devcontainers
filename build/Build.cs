using Nuke.Common;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.Npm.NpmTasks;
using Nuke.Common.Tools.Npm;
using static Nuke.Common.IO.FileSystemTasks;
using Nuke.Common.IO;
using System.IO;

class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.Compile);

	[PathExecutable("bash")] readonly Tool Bash;
	[PathExecutable("devcontainer")] readonly Tool DevContainer;

	[Parameter("Template to build")] readonly string Template;

	Target InstallDevContainersCli => _ => _
		.Executes(() => NpmInstall(_ => _
			.SetPackages("@devcontainers/cli")
			.SetGlobal(true)));

	Target Compile => _ => _
		.Requires(() => Template)
		.DependsOn(InstallDevContainersCli)
		.Executes(() => Bash($"./scripts/build.sh {Template}"));

	Target UpTemplate => _ => _
		.Requires(() => Template)
		.DependsOn(InstallDevContainersCli)
		.OnlyWhenStatic(() => Directory.Exists(RootDirectory / "src" / Template))
		.Executes(() =>
		{
			var sourceDir = $"/tmp/{Template}";
			var idLabel = $"test-container={Template}";
			EnsureCleanDirectory(sourceDir);
			CopyDirectoryRecursively($"./src/{Template}", sourceDir,
				directoryPolicy: DirectoryExistsPolicy.Merge,
				filePolicy: FileExistsPolicy.Overwrite);
			DevContainer($"up --id-label {idLabel} --workspace-folder {sourceDir}");
		});
}
