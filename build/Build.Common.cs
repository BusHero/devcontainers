using Nuke.Common;
using Nuke.Common.IO;

public partial class Build
{
	private Target ListTemplatesAndFeatures => _ => _
		.Requires(() => GithubOutput)
		.Triggers(GetFeatures, GetTemplates, CheckChangesToNuke);

	private AbsolutePath Scripts => RootDirectory / "scripts";
}
