using Nuke.Common;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.Npm.NpmTasks;
using Nuke.Common.Tools.Npm;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;


sealed partial class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.BuildTemplate);

	[PathExecutable("bash")] private readonly Tool Bash;
	private Tool Devcontainer => ToolResolver.GetPathTool("devcontainer");

	private Target InstallDevcontainer => _ => _
		.Unlisted()
		.Executes(() => NpmInstall(_ => _
			.AddPackages("@devcontainers/cli")
			.SetGlobal(true)));

	[GitRepository] private readonly GitRepository Repository;

	private string GitHubNamespace => $"{Repository.GetGitHubOwner()}/{Repository.GetGitHubName()}";

}
