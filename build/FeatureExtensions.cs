using System.Text.Json;

public static class FeatureExtensions
{
	public static List<string>? GetVsCodeExtensions(this Feature feature)
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

		if (customization is not JsonElement element)
		{
			return null;
		}

        if (!element.TryGetProperty("extensions", out var jsonElementExtensions))
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
