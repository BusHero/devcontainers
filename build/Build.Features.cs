using Nuke.Common;
using Nuke.Common.IO;

sealed partial class Build
{
	[Parameter("Feature to build")] private readonly string Feature;
	private AbsolutePath PathToFeatures => RootDirectory / "features";

	private Target TestFeature => _ => _
		.Requires(() => Feature)
		.Executes(() =>
		{
			return Devcontainer($"features test --features {Feature} --project-folder {PathToFeatures} -i alpine");
		});

	private Target PublishFeatures => _ => _
		.Executes(() =>
		{
			return Devcontainer($"features publish {PathToFeatures / "src"} --namespace {GitHubNamespace}/features");
		});
}
