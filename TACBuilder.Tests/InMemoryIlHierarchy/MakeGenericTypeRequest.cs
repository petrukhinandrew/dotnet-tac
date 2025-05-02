using org.jacodb.api.net.generated.models;

namespace TACBuilder.Tests.InMemoryIlHierarchy;

public class MakeGenericTypeRequest : IDisposable
{
    private AppTacBuilder builder = new();

    private string src =
        "/Users/petrukhinandrew/RiderProjects/dotnet-tac/TACBuilder.Tests/bin/Release/net8.0/osx-arm64/publish/TACBuilder.Tests.dll";

    public MakeGenericTypeRequest()
    {
        AppTacBuilder.IncludeRootAsm(src);
        builder.Build(src);
    }

    public void Dispose()
    {
    }

    [Fact]
    void DoesNotThrowForImproper()
    {
        var req = new TypeId(
            [
                new TypeId([],
                    AppTacBuilder.GetBuiltAssemblies().Single(asm => asm.Name.Contains("System.Private.CoreLib")).Name,
                    "System.Int32")
            ],
            AppTacBuilder.GetBuiltAssemblies().Single(asm => asm.Location == src).Name,
            "TACBuilder.Tests.InMemoryIlHierarchy.SingleParamBase`1");
        var actual = Record.Exception(() => builder.GetType(req));
        Assert.Null(actual);
    }
}