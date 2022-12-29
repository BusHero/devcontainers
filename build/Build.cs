using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.Compile);

	[PathExecutable("/bin/bash")] readonly Tool Bash;

	[Parameter("Template to build")] readonly string Template;

	Target Compile => _ => _
		.Requires(() => Template)
		.Executes(() => Bash($"./scripts/build.sh {Template}"));

}
