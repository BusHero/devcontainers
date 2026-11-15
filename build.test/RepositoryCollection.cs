namespace build.test;

/// <summary>
/// Tests that change this repository and restore it with GitSnapshotter. They must not run in parallel:
/// each fixture restores the state from when it was created, so two at once would undo each other's work.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RepositoryCollection
{
    public const string Name = "Repository";
}
