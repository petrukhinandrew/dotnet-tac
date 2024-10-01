namespace Usvm.TACBuilder;

class Program
{
    static void Main(string[] args)
    {
        var path = Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll");
        Console.WriteLine(path);
        var appTacBuilder =
            new AppTacBuilder(path);
        appTacBuilder.Resolve();
    }
}
