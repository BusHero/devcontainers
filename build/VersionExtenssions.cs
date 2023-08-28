public static class VersionExtenssions
{
	public static Version IncrementMajor(this Version version)
	{
		var major = version.Major;

		return new Version(major + 1, 0, 0);
	}

	public static Version IncrementMinor(this Version version)
	{
		var major = version.Major;
		var minor = version.Minor;

		return new Version(major, minor + 1, 0);
	}

	public static Version IncrementBuild(this Version version)
	{
		var major = version.Major;
		var minor = version.Minor;
		var build = version.Build;

		return new Version(major, minor, build + 1);
	}
}
