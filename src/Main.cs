using Usvm.IL.Parser;

namespace Usvm.IL.Main;

class Program
{
    static void Main(string[] _)
    {
        ParserSettings settings = new ParserSettings("bin/Debug/net8.0/dotnet-tac.dll", ["CatchExceptionUsage"]);
        CodeBase codeBase = new CodeBase(settings);
        codeBase.Load();
    }
}