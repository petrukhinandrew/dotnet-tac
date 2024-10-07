using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TACBuilder;

class Program
{
    static void Main(string[] args)
    {
        var path = Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll");
        var appTacBuilder =
            new AppTacBuilder(path);
        // appTacBuilder.Resolve();
    }
}


// /home/andrew/Documents/dotnet-tac/TACBuilder.Tests/bin/Release/net9.0/linux-x64/publish/System.Private.CoreLib.dll
// /home/andrew/Documents/dotnet-tac/TACBuilder.Tests/bin/Release/net9.0/linux-x64/publish/System.Private.CoreLib.dll
