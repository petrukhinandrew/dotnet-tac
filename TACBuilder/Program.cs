using TACBuilder.ILMeta;
using Usvm.IL.Parser;

namespace Usvm.TACBuilder;

class Program
{
    static void Main(string[] args)
    {
        var settings = new ProgramSettings("TACBuilder.Tests.dll",
            // ["Filter", "TernaryOp", "NestedTryCatch", "ArrayRef", "TupleRet", "NestedTryBlocks2"]
            ["Filter"]
        );
        var assemblyTacBuilder = new AssemblyTacBuilder(
            new AssemblyMeta(settings.DllPath)
        );
        var tacAssembly = assemblyTacBuilder.Build();

        tacAssembly.SerializeTo(Console.OpenStandardOutput());
    }
}