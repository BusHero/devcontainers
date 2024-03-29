using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tooling;

partial class Build : NukeBuild
{
	private const string MAIN_BRANCH = "main";
	private const string REMOTE = "origin";
	private const string REMOTE_MAIN_BRANCH = $"remotes/{REMOTE}/{MAIN_BRANCH}";

	[PathExecutable("git")] readonly Tool Git = null!;

	[GitRepository] private readonly GitRepository Repository = null!;

	private string GitHubNamespace => $"{Repository.GetGitHubOwner()}/{Repository.GetGitHubName()}";

	[Parameter("Github Output")] private readonly string GithubOutput = null!;

	private List<string>? _changes;
	private List<string> Changes => _changes ??= GetChanges().ToList();

	public IEnumerable<string> GetChanges()
	{
		var changedFiles = Git(Arguments(
			"diff",
			REMOTE_MAIN_BRANCH,
			"--name-only"));
		var untrackedFiles = Git(Arguments(
			"ls-files",
			"--exclude-standard",
			"--others"));

		return changedFiles
			.Concat(untrackedFiles)
			.Select(x => x.Text);
	}

	private static string Arguments(params string[] args) => string.Join(" ", args);

	private async Task OutputToGithub(string name, object content)
	{
		await File.AppendAllTextAsync(GithubOutput, $"{name}={content}\n");
	}
}
