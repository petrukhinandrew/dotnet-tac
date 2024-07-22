using Usvm.IL.Parser;

namespace Usvm.IL.Main;



class Program
{
    static void Main(string[] args)
    {
        // ["switchExample", "lambda", "ifExample"]
        // ["addOne", "calculations"]
        // ["ByValue"]
        ParserSettings settings = new ParserSettings("/home/andrew/Documents/dotnet-lib-parser/test/resources/dotnet-test.dll", ["testEHC"]);
        CodeBase codeBase = new CodeBase(settings);
        codeBase.Load();
    }
}