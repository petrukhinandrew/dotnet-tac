using Usvm.IL.Parser;

namespace Usvm.IL.Main;



class Program
{
    static void Main(string[] args)
    {
        // ["switchExample", "lambda", "ifExample"]
        // ["addOne", "calculations"]
        // ["ByValue"]
        // ["Literals"]
        ParserSettings settings = new ParserSettings("bin/Debug/net8.0/dotnet-tac.dll", ["ByValue"]);
        CodeBase codeBase = new CodeBase(settings);
        codeBase.Load();
    }
}