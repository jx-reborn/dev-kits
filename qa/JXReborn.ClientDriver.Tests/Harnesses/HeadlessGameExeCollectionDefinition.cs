using Xunit;

namespace JXReborn.ClientDriver.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class HeadlessGameExeCollectionDefinition
{
    public const string Name = "HeadlessGameExeSerial";
}
