using org.jacodb.api.net.generated.models;
using TACBuilder.ILReflection;
using TACBuilder.Serialization;
using TACBuilder.Utils;

namespace TACBuilder;

class Program
{
    static void Main(string[] args)
    {

        AppTacBuilder builder = new();
        if (args.Contains("--rd"))
        {
            Console.WriteLine(Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll"));

            var connection = new RdConnection(req =>
            {
                Console.WriteLine(req.RootAsm);
                AppTacBuilder.IncludeRootAsm(req.RootAsm);
                builder.Build(req.RootAsm);
                var instances = AppTacBuilder.GetFreshInstances();
                Console.WriteLine($".net built {instances.Count} instances with total of {instances.Select(it => (it as IlType).Methods.Count).Sum()}");
                var serialized = RdSerializer.Serialize(instances);
                
                return serialized;
            });
            connection.Connect(8083);
        }
        else if (args.Contains("--dynamic-tests"))
        {
            var asm = new CalliDynamicAsmBuilder().Build();
            var name = asm.GetName();
            AppTacBuilder.IncludeRootAsm(name);
            builder.Build(asm);
            var builtAsms = builder.BuiltAssemblies;
        }
        else if (args.Contains("--console"))
        {
            var path = Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll");
            Console.WriteLine(path);
            AppTacBuilder.IncludeTACBuilder();
            // AppTacBuilder.IncludeRootAsm(path);
            // AppTacBuilder.IncludeMsCoreLib();
            builder.Build(path);
            var builtAsms = builder.BuiltAssemblies;
            var serialized = RdSerializer.Serialize(AppTacBuilder.GetFreshInstances()); // .Where(i => i is IlType t && t.Name.Contains("AnotherStruct")).ToList()
        }
    }
}