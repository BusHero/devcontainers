namespace build.test;

public sealed record Tag(string Name)
{
	public override string ToString()
	{
		return Name;
	}

	public static implicit operator string(Tag tag) => tag.ToString();
}
