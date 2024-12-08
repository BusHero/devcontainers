using System.Text;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendHeading1(
        this StringBuilder stringBuilder,
        string? heading) => stringBuilder
            .Append("# ")
            .AppendLine(heading);

    public static StringBuilder AppendHeading2(
        this StringBuilder stringBuilder,
        string? heading) => stringBuilder
            .Append("## ")
            .AppendLine(heading);

    public static StringBuilder AppendHeading3(
        this StringBuilder stringBuilder,
        string? heading) => stringBuilder
            .Append("### ")
            .AppendLine(heading);

    public static StringBuilder AppendListItem(
        this StringBuilder stringBuilder,
        Func<StringBuilder, StringBuilder> func) => stringBuilder
            .Append("- ")
            .Combine(func)
            .AppendLine();

    private static StringBuilder Combine(
        this StringBuilder stringBuilder,
        Func<StringBuilder, StringBuilder> next) => next(stringBuilder);

    public static StringBuilder AppendCode(
        this StringBuilder stringBuilder,
        string? code) => stringBuilder
            .Append('`')
            .Append(code)
            .Append('`');

    public static StringBuilder AppendCodeBlock(
        this StringBuilder stringBuilder,
        string? code,
        string? language = default) => stringBuilder
        .Append("```").AppendLine(language)
        .AppendLine(code)
        .AppendLine("```");

    public static StringBuilder AppendHorizontalLine(this StringBuilder stringBuilder) => stringBuilder
        .AppendLine("---");

    public static void AppendNote(this StringBuilder stringBuilder,
        Func<StringBuilder, StringBuilder> func)
    {
        stringBuilder
            .Append('_')
            .Combine(func)
            .AppendLine("_");
    }

    public static StringBuilder AppendUrl(
        this StringBuilder stringBuilder,
        string? label,
        string? url)
    {
        return stringBuilder
            .Append('[')
            .Append(label)
            .Append(']')
            .Append('(')
            .Append(url)
            .Append(')');
    }
}
