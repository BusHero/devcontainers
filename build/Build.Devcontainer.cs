using Nuke.Common;
using Nuke.Common.Tooling;
using Serilog;

sealed partial class Build : NukeBuild
{
	private Tool Devcontainer => ToolResolver.GetPathTool("devcontainer");

	private void DevcontainerLog(OutputType type, string message) => Log.Debug("{Message}", message);
}
