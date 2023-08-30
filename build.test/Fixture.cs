using System.Text.Json;
using CliWrap;

namespace build.test;

internal sealed class CustomFixture : IAsyncDisposable
{
	private readonly List<string> tags = new();

	private int commitsCount = 0;

	private string? directoryToRemove;

	public bool KeepFiles { get; init; }

	public bool KeepTags { get; init; }

	public bool KeepCommits { get; init; }

	public async ValueTask DisposeAsync()
	{
		if (!KeepFiles)
		{
			RemoveDirectory(directoryToRemove);
		}

		if (!KeepTags)
		{
			await DeleteGitTags(this.tags);
		}

		if (!KeepCommits)
		{
			await RevertCommits(commitsCount);
		}
	}

	public string GetRightTag(string feature) => $"feature_{feature}";

	public async Task RevertCommits(int numberOfCommits)
	{
		if (numberOfCommits <= 0)
		{
			return;
		}

		await Cli.Wrap("git")
			.WithArguments(args => args
				.Add("reset")
				.Add("--no-refresh")
				.Add("--soft")
				.Add($"HEAD~{numberOfCommits}")
				.Add("--quiet"))
			.WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
			.ExecuteAsync();
	}

	public void RemoveDirectory(string? directory)
	{
		if (Directory.Exists(directory))
			Directory.Delete(directory, true);
	}

	public async Task CreateFeatureFile(
		string pathToFeature,
		int major,
		int minor,
		int build)
	{
		var directoryName = Path.GetDirectoryName(pathToFeature);

		Directory.CreateDirectory(directoryName!);

		await File.WriteAllTextAsync(
			pathToFeature,
			$$"""{ "version": "{{major}}.{{minor}}.{{build}}" }""");

		this.directoryToRemove = directoryName;
	}

	public async Task<string?> GetVersion(string path)
	{
		using var fileStream = File.OpenRead(path);
		var document = await JsonDocument.ParseAsync(fileStream);

		return document.RootElement.GetProperty("version").GetString();
	}

	public async Task RunBuild(string feature)
	{
		await Cli.Wrap("dotnet")
			.WithArguments(args => args
				.Add("run")
				.Add("--project")
				.Add("/workspaces/devcontainers/build")
				.Add("Version")
				.Add("--feature")
				.Add(feature)
				.Add("--no-logo")
				.Add("--verbosity")
				.Add("Quiet"))
			.ExecuteAsync();
	}

	public async Task Commit(
		string path,
		string message)
	{
		await Cli.Wrap("git")
			.WithArguments(args => args
				.Add("add")
				.Add(path))
			.ExecuteAsync();

		await Cli.Wrap("git")
			.WithArguments(args => args
				.Add("commit")
				.Add("--include")
				.Add(path)
				.Add("--message")
				.Add(message)
				.Add("--quiet"))
			.ExecuteAsync();

		this.commitsCount++;
	}

	public async Task AddGitTag(
		string tag,
		string commit = "HEAD")
	{
		await Cli.Wrap("git")
			.WithArguments(args => args
				.Add("tag")
				.Add(tag)
				.Add(commit))
			.ExecuteAsync();
		this.tags.Add(tag);
	}

	public async Task DeleteGitTags(IReadOnlyCollection<string> tags)
	{
		if (tags.Count is 0)
		{
			return;
		}

		await Cli.Wrap("git")
			.WithArguments(args =>
			{
				args
					.Add("tag")
					.Add("--delete");

				foreach (var tag in tags)
				{
					args.Add(tag);
				}
			})
			.WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
			.ExecuteAsync();
	}
}
