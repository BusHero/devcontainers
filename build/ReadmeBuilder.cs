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
                {{readme.Registry}}/{{readme.Namesapce}}/{{readme.Id}}:{{readme.Version}}: {}
            }
            """, "json")
            .AppendLine()
            ;
        AppendOptionsTable(stringBuilder, readme.Options);
        AppendVSCustomization(stringBuilder, readme.VSCodeExtenssions);
        AppendNotes(stringBuilder, readme.PathToAdditionalNotes);
        AppendFooter(
            stringBuilder,
            readme.PathToDevContainerDefinition);

        return stringBuilder.ToString();
    }

    public StringBuilder AppendNotes(
        StringBuilder stringBuilder,
        string? pathToAdditionalNotes)
    {
        if (pathToAdditionalNotes is null)
        {
            return stringBuilder;
        }

        if (!File.Exists(pathToAdditionalNotes))
        {
            return stringBuilder;
        }

        return File
            .ReadAllLines(pathToAdditionalNotes)
            .Aggregate(stringBuilder, (builder, line) => builder.AppendLine(line))
            .AppendLine();
    }
    public StringBuilder AppendVSCustomization(
        StringBuilder stringBuilder,
        IReadOnlyCollection<string>? customizations)
    {
        if (customizations is null || customizations.Count == 0)
        {
            return stringBuilder;
        }

        stringBuilder
            .AppendHeading2("Customizations")
            .AppendLine()
            .AppendHeading3("VS Code Extenssions")
            .AppendLine();

        customizations.Aggregate(
            stringBuilder,
            (builder, ext) => builder
                .AppendListItem(x => x.AppendCode(ext)))
            .AppendLine();

        return stringBuilder;
    }

    public StringBuilder AppendOptionsTable(
        StringBuilder stringBuilder,
        IReadOnlyCollection<Option>? options)
    {
        if (options is null || options.Count == 0)
        {
            return stringBuilder;
        }

        stringBuilder
            .AppendLine("## Options")
            .AppendLine()
            .AppendLine("| Options Id | Description | Type | Default Value |")
            .AppendLine("|-----|-----|-----|-----|");

        return options
            .Select(x => $"| {x.Id} | {x.Description} | {x.Type} | {x.DefaultValue} |")
            .Aggregate(stringBuilder, (builder, option) => builder.AppendLine(option))
            .AppendLine();
    }

    public StringBuilder AppendFooter(
        StringBuilder builder,
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

        return builder;
    }
}

public record Readme
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public required string Registry { get; init; }

    public required string Namesapce { get; init; }

    public required string Id { get; init; }

    public required string Version { get; init; }

    public IReadOnlyCollection<Option>? Options { get; init; }

    public IReadOnlyCollection<string>? VSCodeExtenssions { get; init; }

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
