using System.Diagnostics;
using TACBuilder.ILReflection;


namespace TACBuilder;

public class AppTacBuilder
{
    private string _path;

    public AppTacBuilder(string rootAssemblyPath)
    {
        _path = rootAssemblyPath;
        Debug.Assert(File.Exists(rootAssemblyPath));
    }

    public void Build()
    {

        var rootAssemblyMeta = IlInstanceBuilder.BuildFrom(_path);
        BuiltAssemblies.AddRange(IlInstanceBuilder.GetAssemblies());
    }

    public List<IlAssembly> BuiltAssemblies { get; } = new();

    public static void FilterMethodsFromRootAsm(string rootAssemblyPath)
    {
        IlInstanceBuilder.AddAssemblyFilter(assembly => assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath);
    }

    public static void FilterSingleMethodFromRootAsm(string rootAssemblyPath, string methodName)
    {
        IlInstanceBuilder.AddAssemblyFilter(assembly => assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath && method.Name.StartsWith(methodName));
    }

    public static void FilterMethodsFromSingleMSCoreLibType(string rootAssemblyPath, string typeNamePart)
    {
        IlInstanceBuilder.AddAssemblyFilter(assembly =>
            assembly.GetName().FullName.StartsWith("System.Private.CoreLib") || assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.GetName().FullName.StartsWith("System.Private.CoreLib") ||
            type.Assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddTypeFilter(type => type.Name.StartsWith(typeNamePart));
        // IlInstanceBuilder.AddMethodFilter(call =>
        //     (call.ReflectedType ?? call.DeclaringType)!.Assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddMethodFilter(method => method.DeclaringType!.Name.StartsWith(typeNamePart));
    }

    public static List<IlCacheable> GetFreshInstances()
    {
        return IlInstanceBuilder.GetFreshInstances();
    }
}
