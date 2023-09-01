using System.Text.Json;

using Nuke.Common;
using Nuke.Common.IO;

sealed partial class Build
{
    private readonly ReadmeBuilder _readmeBuilder = new();

    private RelativePath PathToAdditionalNotes => FeaturesRoot.GetRelativePathTo(FeaturesRoot) / "src" / Feature / "Notes.md";

    private AbsolutePath PathToFeatureDefinition => FeaturesRoot / "src" / Feature / "devcontainer-feature.json";

    private RelativePath RelativePathToFeatureDefinition => RootDirectory.GetRelativePathTo(FeaturesRoot) / "src" / Feature / "devcontainer-feature.json";

    private AbsolutePath PathToReadme => FeaturesRoot / "src" / Feature / "README.md";

    Target GenerateDocumentationFeature => _ => _
        .Requires(() => Feature)
        .Executes(async () =>
        {
            var feature = await ReadFeatureSpecification(PathToFeatureDefinition);

            var vsCodeExtenssions = feature.GetVSCodeExtenssions();

            var content = _readmeBuilder.Build(new Readme
            {
                Name = feature.Name,
                Description = feature.Description,
                Registry = "ghcr.io",
                Id = feature.Id,
                Version = feature.Version,
                Namesapce = $"bushero/devcontainers/features",
                Options = feature
                    .Options
                    ?.Select(tuple => MapOption(tuple.Key, tuple.Value))
                    .ToList(),
                VSCodeExtenssions = vsCodeExtenssions,
                PathToAdditionalNotes = PathToAdditionalNotes,
                PathToDevContainerDefinition = "/" + RelativePathToFeatureDefinition
            });

            await File.WriteAllTextAsync(PathToReadme, content);
        });

    private static Option MapOption(string key, FeatureOption featureOption)
    {
        var defaultValue = featureOption.Default.GetValue();
        var type = Enum.GetName(featureOption.Type) ?? "-";

        var option = new Option
        {
            Id = key,
            Type = type,
            Description = featureOption.Description,
            DefaultValue = defaultValue,
        };

        return option;
    }

    private async Task<Feature> ReadFeatureSpecification(string path)
    {
        using var stream = File.OpenRead(path);
        var feature = await JsonSerializer.DeserializeAsync<Feature>(stream, Converter.Settings);

        return feature ?? throw new InvalidOperationException("It's not supposed to be null");
    }
}

public static class FeatureExtenssions
{
    public static List<string>? GetVSCodeExtenssions(this Feature feature)
    {
        var customizations = feature.Customizations;
        if (customizations is null)
        {
            return null;
        }

        if (!customizations.TryGetValue("vscode", out var customization))
        {
            return null;
        }

        if (customization is not JsonElement)
        {
            return null;
        }

        var jsonElement = (JsonElement)customization;

        if (!jsonElement.TryGetProperty("extensions", out var jsonElementExtensions))
        {
            return null;
        }

        if (jsonElementExtensions.ValueKind is not JsonValueKind.Array)
        {
            return null;
        }

        var extensions = jsonElementExtensions
            .EnumerateArray()
            .Where(x => x.ValueKind is JsonValueKind.String)
            .Select(x => x.GetString())
            .Cast<string>()
            .ToList();

        return extensions.Count == 0 ? null : extensions;
    }
}
