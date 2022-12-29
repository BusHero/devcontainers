using Nuke.Common;
using Nuke.Common.Tooling;

class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.Compile);

	[PathExecutable("/bin/bash")] readonly Tool Bash;

	[Parameter("Template to build")] readonly string Template;

	Target Compile => _ => _
		.Requires(() => Template)
		.Executes(() => Bash($"./scripts/build.sh {Template}"));

}
