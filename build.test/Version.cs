namespace build.test;

public sealed record Version(int Major, int Minor, int Build)
{
	public override string ToString() => $"{Major}.{Minor}.{Build}";

	public static implicit operator string(Version version) => version.ToString();
}
