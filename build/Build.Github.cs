using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;

sealed partial class Build
{
	[GitRepository] private readonly GitRepository Repository;

	private string GitHubNamespace => $"{Repository.GetGitHubOwner()}/{Repository.GetGitHubName()}";
}
