using System.Diagnostics;
using TACBuilder.Serialization;

namespace TACBuilder;

class Program
{
    static void Main(string[] args)
    {
        var path = Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll");
        // AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "FileSystemEntry");
        // AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "AhoCorasick");
        // AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "MulticastDelegate");
        // AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "Vector128");
        // AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "Int32");
        AppTacBuilder.FilterMethodsFromRootAsm(path);
        // ILInstanceBuilder.AddMethodFilter(call => call.Name == "NestedFinally");
        // ILInstanceBuilder.AddTypeFilter(type => type.Name == "CustomAttrUsage");
        // tacAssembly.SerializeTo(serializationStream);
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
