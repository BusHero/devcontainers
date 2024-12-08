using JetBrains.Annotations;

namespace build.test;

[UsedImplicitly]
public sealed record Feature
{
	private readonly string _name;

	public Feature(string name) => _name = name.Replace("-", string.Empty);

	public override string ToString() => _name;
	
	public static implicit operator string(Feature feature) => feature.ToString();
}
