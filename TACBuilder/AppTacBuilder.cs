using System.Diagnostics;
using System.Reflection;
using TACBuilder.ILReflection;


namespace TACBuilder;

public class AppTacBuilder
{
    public void Build(Assembly assembly)
    {
        IlInstanceBuilder.BuildFrom(assembly);
        BuiltAssemblies.AddRange(IlInstanceBuilder.GetAssemblies());
    }

    public void Build(string asmPath)
    {
        Debug.Assert(File.Exists(asmPath));
        IlInstanceBuilder.BuildFrom(asmPath);
        BuiltAssemblies.AddRange(IlInstanceBuilder.GetAssemblies());
    }

    public void Build(AssemblyName asmName)
    {
        IlInstanceBuilder.BuildFrom(asmName);
        BuiltAssemblies.AddRange(IlInstanceBuilder.GetAssemblies());
    }

    public List<IlAssembly> BuiltAssemblies { get; } = new();

    public static void IncludeTACBuilder()
    {
        IlInstanceBuilder.AddAssemblyFilter(assembly => assembly.GetName().ToString().StartsWith("TACBuilder"));
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.GetName().ToString().StartsWith("TACBuilder"));
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.GetName().ToString().StartsWith("TACBuilder"));
    }

    public static void IncludeRootAsm(string rootAssemblyPath)
    {
        IlInstanceBuilder.AddAssemblyFilter(assembly => assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath);
    }

    public static void IncludeRootAsm(AssemblyName asmName)
    {
        IlInstanceBuilder.AddAssemblyFilter(assembly => assembly.GetName().FullName == asmName.FullName);
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.GetName().FullName == asmName.FullName);
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.GetName().FullName == asmName.FullName);
    }

    public static void IncludeMsCoreLib()
    {
        IlInstanceBuilder.AddAssemblyFilter(assembly =>
            assembly.GetName().FullName.StartsWith("System.Private.CoreLib"));
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.GetName().FullName.StartsWith("System.Private.CoreLib"));
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.FullName.StartsWith("System.Private.CoreLib"));
    }

    public static void FilterSingleMethodFromRootAsm(string rootAssemblyPath, string methodName)
    {
        IlInstanceBuilder.AddAssemblyFilter(assembly => assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath &&
            method.Name.StartsWith(methodName));
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

    public static List<IlType> GetFreshInstances()
    {
        return IlInstanceBuilder.GetFreshTypes();
    }

    public static Dictionary<string, List<string>> GetBuiltAssemblies()
    {
        return IlInstanceBuilder.GetAsmDependencyGraph();
    }
}