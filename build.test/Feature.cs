namespace build.test;

public sealed record Feature
{
	private readonly string name;

	public Feature(string name) => this.name = name.Replace("-", string.Empty);

	public override string ToString() => name;

	
	public static implicit operator string(Feature feature) => feature.ToString();
}
