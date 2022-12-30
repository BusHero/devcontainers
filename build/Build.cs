using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

sealed class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.BuildTemplate);

	[PathExecutable("bash")] readonly Tool Bash;

	[Parameter("Template to build")] readonly string Template;

	private AbsolutePath Scripts => RootDirectory / "scripts";

	Target BuildTemplate => _ => _
		.Requires(() => Template)
		.Executes(() => Bash($"{Scripts / "build.sh"} {Template}"));
}
