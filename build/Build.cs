using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

sealed class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.BuildTemplate);

	[PathExecutable("bash")] private readonly Tool Bash;

	[Parameter("Template to build")] private readonly string Template;

	private AbsolutePath Scripts => RootDirectory / "scripts";

	private Target BuildTemplate => _ => _
		.Requires(() => Template)
		.Executes(() => Bash($"{Scripts / "build.sh"} {Template}"));

	private Target TestTemplate => _ => _
		.Requires(() => Template)
		.Executes(() => Bash($"{Scripts / "test.sh"} {Template}"));
}
