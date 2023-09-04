using System.Text.Json;
using Nuke.Common.IO;

namespace build.test;

public static class FeatureExtenssions
{
	public static Tag GetTag(
		this Feature feature,
		Version version) => new($"feature_{feature}_{version}");

	public static AbsolutePath GetRoot(
		this Feature feature,
		AbsolutePath projectRoot) => projectRoot
			/ "features"
			/ "src"
			/ feature;

	public static AbsolutePath GetConfig(
		this Feature featureName,
		AbsolutePath projectRoot)
		=> featureName.GetRoot(projectRoot)
			/ "devcontainer-feature.json";

	public static AbsolutePath GetDocumentation(
		this Feature featureName,
		AbsolutePath projectRoot)
		=> featureName.GetRoot(projectRoot)
			/ "README.md";

	public static string GetRelativePathToConfig(this Feature feature)
		=> Path.Combine("features", "src", feature, "devcontainer-feature.json");

	public static string GetRelativePathToDocumentation(this Feature feature)
		=> Path.Combine("features", "src", feature, "README.md");

	public static async Task<string?> GetVersion(
		this Feature feature,
		AbsolutePath projectRoot)
	{
		var featureConfig = feature.GetConfig(projectRoot);
		using var fileStream = File.OpenRead(featureConfig);
		var document = await JsonDocument.ParseAsync(fileStream);

		return document.RootElement.GetProperty("version").GetString();
	}

	public static AbsolutePath CreateTempFile(
		this Feature feature,
		AbsolutePath root)
	{
		var path = feature.GetRoot(root) / $"tmp_{Guid.NewGuid():N}";

		using var _ = File.Create(path);

		return path;
	}
}
