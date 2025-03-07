namespace build.test;

public static class VersionExtensions
{
    public static Version IncrementMajor(this Version version) => new(
        Major: version.Major + 1,
        Minor: 0,
        Build: 0);

    public static Version IncrementMinor(this Version version) => version with
    {
        Minor = version.Minor + 1,
        Build = 0
    };

    public static Version IncrementBuild(this Version version) => version with
    {
        Build = version.Build + 1
    };
}