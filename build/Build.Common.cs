using Nuke.Common;
using Nuke.Common.IO;

public partial class Build
{
	private Target ListTemplatesAndFeatures => _ => _
		.Requires(() => GithubOutput)
		.Triggers(GetFeatures, GetTemplates);

	private AbsolutePath Scripts => RootDirectory / "scripts";
}
