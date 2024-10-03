using TACBuilder.ILMeta;

namespace TACBuilder;

public class AppTacBuilder
{
    public AppTacBuilder(string rootAssemblyPath)
    {
        MetaBuilder.AddAssemblyFilter(assembly => assembly.Location == rootAssemblyPath);
        MetaBuilder.AddTypeFilter(type => type.Assembly.Location == rootAssemblyPath);
        MetaBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath);

        var rootAssemblyMeta = MetaBuilder.BuildFrom(rootAssemblyPath);

        foreach (var asm in MetaBuilder.GetAssemblies())
        {
            var tacAssembly = new AssemblyTacBuilder(asm).Build();
            tacAssembly.SerializeTo(Console.OpenStandardOutput());
        }
    }
}
