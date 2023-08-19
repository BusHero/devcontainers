using Nuke.Common;
using Serilog;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.HelloWorld);

    Target HelloWorld => _ => _.Executes(() => Log.Information("{Message}", "Hello, World!"));

}
