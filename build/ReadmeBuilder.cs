using System.Text;

public class ReadmeBuilder
{
    public string Build(Readme readme)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder
            .AppendHeading1(readme.Name)
            .AppendLine()
            .AppendHeading1(readme.Description)
            .AppendLine()
            .AppendCodeBlock($$"""
            "features": {
                "{{readme.Registry}}/{{readme.Namespace}}/{{readme.Id}}:{{readme.Version}}": {}
            }
            """, "json")
            .AppendLine()
            ;
        AppendOptionsTable(stringBuilder, readme.Options);
        AppendVsCustomization(stringBuilder, readme.VsCodeExtensions);
        AppendNotes(stringBuilder, readme.PathToAdditionalNotes);
        AppendFooter(
            stringBuilder,
            readme.PathToDevContainerDefinition);

        return stringBuilder.ToString();
    }

    private void AppendNotes(StringBuilder stringBuilder,
        string? pathToAdditionalNotes)
    {
        if (pathToAdditionalNotes is null)
        {
            return;
        }

        if (!File.Exists(pathToAdditionalNotes))
        {
            return;
        }

        File
            .ReadAllLines(pathToAdditionalNotes)
            .Aggregate(stringBuilder, (builder, line) => builder.AppendLine(line))
            .AppendLine();
    }

    private void AppendVsCustomization(StringBuilder stringBuilder,
        IReadOnlyCollection<string>? customizations)
    {
        if (customizations is null || customizations.Count == 0)
        {
            return;
        }

        stringBuilder
            .AppendHeading2("Customizations")
            .AppendLine()
            .AppendHeading3("VS Code Extensions")
            .AppendLine();

        customizations.Aggregate(
            stringBuilder,
            (builder, ext) => builder
                .AppendListItem(x => x.AppendCode(ext)))
            .AppendLine();
    }

    private void AppendOptionsTable(StringBuilder stringBuilder,
        IReadOnlyCollection<Option>? options)
    {
        if (options is null || options.Count == 0)
        {
            return;
        }

        stringBuilder
            .AppendLine("## Options")
            .AppendLine()
            .AppendLine("| Options Id | Description | Type | Default Value |")
            .AppendLine("|-----|-----|-----|-----|");

        options
            .Select(x => $"| {x.Id} | {x.Description} | {x.Type} | {x.DefaultValue} |")
            .Aggregate(stringBuilder, (builder, option) => builder.AppendLine(option))
            .AppendLine();
    }

    private void AppendFooter(StringBuilder builder,
        string url)
    {
        builder
            .AppendHorizontalLine()
            .AppendLine()
            .AppendNote(x => x
                .Append("Note: This file was auto-generated from the ")
                .AppendUrl("devcontainer-feature.json", url)
                .Append(". Add additional notes to a ")
                .AppendCode("Notes.md")
                .Append('.'));
    }
}

public record Readme
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public required string Registry { get; init; }

    public required string Namespace { get; init; }

    public required string Id { get; init; }

    public required string Version { get; init; }

    public IReadOnlyCollection<Option>? Options { get; init; }

    public IReadOnlyCollection<string>? VsCodeExtensions { get; init; }

    public string? PathToAdditionalNotes { get; init; }

    public required string PathToDevContainerDefinition { get; init; }
}

public record Option
{
    public required string Id { get; init; }

    public string? Description { get; init; }

    public required string Type { get; init; }

    public required string DefaultValue { get; init; }
}
