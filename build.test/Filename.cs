namespace build.test;

public class Filename
{
	public Filename(string name) => Name = name.Replace("-", string.Empty);

	public string Name { get; }

	public override string ToString() => Name;

	public static implicit operator string(Filename filename) => filename.ToString();
}
