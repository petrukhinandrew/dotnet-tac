using System.Reflection;
using System.Runtime.Loader;
using TACBuilder.ILMeta;
using TACBuilder.ILMeta.ILBodyParser;
using Usvm.IL.Parser;

namespace Usvm.TACBuilder;

class Program
{
    static void Main(string[] args)
    {
        var settings = new ProgramSettings("TACBuilder.Tests/bin/Debug/net8.0/TACBuilder.Tests.dll",
            // ["Filter", "TernaryOp", "NestedTryCatch", "ArrayRef", "TupleRet", "NestedTryBlocks2"]
            ["IntMethod"]
        );
        var assemblyTacBuilder = new AssemblyTacBuilder.AssemblyTacBuilder(
            new AssemblyMeta(settings.DllPath)
        );
        var tacAssembly = assemblyTacBuilder.Build();
    }
}