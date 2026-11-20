using Xunit;

namespace JXReborn.Workbench.ParityTests.Collections;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class NonParallelParityCollection
{
    public const string Name = "WorkbenchNonParallelParity";
}
