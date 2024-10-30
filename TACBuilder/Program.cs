// #define CONSOLE_SERIALIZER

#define RD_SERIALIZER
using TACBuilder.Serialization;

namespace TACBuilder;

class Program
{
    static void Main(string[] args)
    {
        AppTacBuilder builder;
        if (args.Contains("--rd"))
        {
            Console.WriteLine(Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll"));
            var connection = new RdConnection(req =>
            {
                Console.WriteLine(req.RootAsm);
                builder = new AppTacBuilder(req.RootAsm);
                AppTacBuilder.FilterMethodsFromRootAsm(req.RootAsm);
                builder.Build();
                var instances = AppTacBuilder.GetFreshInstances();
                var serialized = RdSerializer.Serialize(instances);
                return serialized;
            });
            connection.Connect(8083);
        }
        else
        {
            var path = Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll");
            var appTacBuilder = new AppTacBuilder(path);
            AppTacBuilder.FilterMethodsFromRootAsm(path);
            // AppTacBuilder.FilterSingleMethodFromRootAsm(path, "NestedFinally");
            appTacBuilder.Build();
            var builtAsms = appTacBuilder.BuiltAssemblies;
            var writer = new StreamWriter(Console.OpenStandardOutput(), leaveOpen: true);
            var serializer = new ConsoleTacSerializer(builtAsms, writer);
            serializer.Serialize();
            writer.Flush();
            writer.Close();
        }
    }
}

// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "CounterGroup");
// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "AhoCorasick"); // net9.0

// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "Hashtable");
// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "FileSystemEntry");
// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "MulticastDelegate");
// AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "Vector128");
