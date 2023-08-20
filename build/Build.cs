using Nuke.Common;
using Nuke.Common.Tooling;

sealed partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.BuildTemplate);

    [PathExecutable("bash")] private readonly Tool Bash = null!;
}
