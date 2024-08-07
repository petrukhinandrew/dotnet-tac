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
        ParserSettings settings = new ParserSettings("test/resources/dotnet-test.dll", ["Boxing"]);
        CodeBase codeBase = new CodeBase(settings);
        codeBase.Load();
    }
}