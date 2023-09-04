using System.Text.Json;

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
