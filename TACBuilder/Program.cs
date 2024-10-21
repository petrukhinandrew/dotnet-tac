using System.Diagnostics;
using System.Runtime.Intrinsics;
using TACBuilder.Serialization;

namespace TACBuilder;

class Program
{
    static void Main(string[] args)
    {
        var path = Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll");
        // AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "CounterGroup");
        // AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "AhoCorasick"); // net9.0

        // AppTacBuilder.FilterMethodsFromRootAsm(path);
        // AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "FileSystemEntry");
        // AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "MulticastDelegate");
        // AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "Vector128");
        AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "Hashtable");

        // ILInstanceBuilder.AddMethodFilter(call => calfel.Name == "NestedFinally");
        // ILInstanceBuilder.AddTypeFilter(type => type.Name == "CustomAttrUsage");
        var appTacBuilder =
            new AppTacBuilder(path, Console.OpenStandardOutput());
        var builtAsms = appTacBuilder.BuiltAssemblies;

        var writer = new StreamWriter(Console.OpenStandardOutput(), leaveOpen: true);
        var serializer = new ConsoleTACSerializer(builtAsms, writer);
        serializer.Serialize();
        writer.Flush();
        writer.Close();
    }
}
