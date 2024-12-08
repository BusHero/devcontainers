using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using Nuke.Common;
using Nuke.Common.IO;

[SuppressMessage("ReSharper", "AllUnderscoreLocalParameterName")]
public partial class Build
{
	[UsedImplicitly]
    private Target ListTemplatesAndFeatures => _ => _
		.Requires(() => GithubOutput)
		.Triggers(GetFeatures, GetTemplates, CheckChangesToNuke);

	private AbsolutePath Scripts => RootDirectory / "scripts";
}
