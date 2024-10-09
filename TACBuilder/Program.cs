using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TACBuilder;

class Program
{
    static void Main(string[] args)
    {
        var path = Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll");
        var appTacBuilder =
            new AppTacBuilder(path, Console.OpenStandardOutput());
        // appTacBuilder.Resolve();
    }
}
