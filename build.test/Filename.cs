using JetBrains.Annotations;

namespace build.test;

[UsedImplicitly]
public class Filename(string name)
{
	private string Name { get; } = name.Replace("-", string.Empty);

	public override string ToString() => Name;

	public static implicit operator string(Filename filename) => filename.ToString();
}
