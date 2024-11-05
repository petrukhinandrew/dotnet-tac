// #define CONSOLE_SERIALIZER
#define RD_SERIALIZER

using System.Reflection.Emit;
using TACBuilder.Serialization;

namespace TACBuilder;

class Program
{
    static void Main(string[] args)
    {
        // TODO need logs on what asm is being resolved, how many types already resolved and total number of types
        AppTacBuilder builder = new();
        if (args.Contains("--rd"))
        {
            Console.WriteLine(Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll"));

            var connection = new RdConnection(req =>
            {
                Console.WriteLine(req.RootAsm);
                AppTacBuilder.FilterMethodsFromRootAsm(req.RootAsm);
                builder.Build(req.RootAsm);
                var instances = AppTacBuilder.GetFreshInstances();
                var serialized = RdSerializer.Serialize(instances);
                return serialized;
            });
            connection.Connect(8083);
        }
        else if (args.Contains("--console"))
        {
            var path = Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll");
            AppTacBuilder.FilterMethodsFromRootAsm(path);
            // AppTacBuilder.FilterSingleMethodFromRootAsm(path, "NestedFinally");
            builder.Build(path);
            var builtAsms = builder.BuiltAssemblies;
            // var writer = new StreamWriter(Console.OpenStandardOutput(), leaveOpen: true);
            // var serializer = new ConsoleTacSerializer(builtAsms, writer);
            // serializer.Serialize();
            // writer.Flush();
            // writer.Close();
        }
    }
}

// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "CounterGroup");
// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "AhoCorasick"); // net9.0

// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "Hashtable");
// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "FileSystemEntry");
// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "MulticastDelegate");
// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "Vector128");
