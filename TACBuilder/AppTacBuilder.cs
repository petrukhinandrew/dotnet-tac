using System.Diagnostics;
using TACBuilder.ILReflection;


namespace TACBuilder;

public class AppTacBuilder
{
    public AppTacBuilder(string rootAssemblyPath, Stream? serializationStream = null)
    {
        Debug.Assert(File.Exists(rootAssemblyPath));

        var rootAssemblyMeta = ILInstanceBuilder.BuildFrom(rootAssemblyPath);
        BuiltAssemblies.AddRange(ILInstanceBuilder.GetAssemblies());
    }

    public List<ILAssembly> BuiltAssemblies { get; } = new();

    public static void FilterMethodsFromRootAsm(string rootAssemblyPath)
    {
        ILInstanceBuilder.AddAssemblyFilter(assembly => assembly.Location == rootAssemblyPath);
        ILInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.Location == rootAssemblyPath);
        ILInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath);
    }

    public static void FilterSingleMethodFromRootAsm(string rootAssemblyPath, string methodName)
    {
        ILInstanceBuilder.AddAssemblyFilter(assembly => assembly.Location == rootAssemblyPath);
        ILInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.Location == rootAssemblyPath);
        ILInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath);
        ILInstanceBuilder.AddTypeFilter(method => method.Name.StartsWith(methodName));
    }

    public static void FilterMethodsFromSingleMSCoreLibType(string rootAssemblyPath, string typeNamePart)
    {
        ILInstanceBuilder.AddAssemblyFilter(assembly =>
            assembly.GetName().FullName.StartsWith("System.Private.CoreLib") || assembly.Location == rootAssemblyPath);
        ILInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.GetName().FullName.StartsWith("System.Private.CoreLib") ||
            type.Assembly.Location == rootAssemblyPath);
        ILInstanceBuilder.AddTypeFilter(type => type.Name.StartsWith(typeNamePart));
        // ILInstanceBuilder.AddMethodFilter(call =>
        //     (call.ReflectedType ?? call.DeclaringType)!.Assembly.Location == rootAssemblyPath);
        ILInstanceBuilder.AddMethodFilter(method => method.DeclaringType!.Name.StartsWith(typeNamePart));
    }
}
