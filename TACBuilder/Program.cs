using Usvm.IL.Parser;

namespace Usvm.IL;

class Program
{
    static void Main(string[] args)
    {
        var settings = new ParserSettings("TACBuilder.Tests/bin/Debug/net8.0/TACBuilder.Tests.dll",
            // ["Filter", "TernaryOp", "NestedTryCatch", "ArrayRef"]
            ["TupleRet"]
        );
        var codeBase = new CodeBase(settings);
        codeBase.Load();
    }
}