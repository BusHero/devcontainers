namespace build.test;

public sealed record Tag(string Name)
{
	public override string ToString() => Name;

	public static implicit operator string(Tag tag) => tag.ToString();
}
