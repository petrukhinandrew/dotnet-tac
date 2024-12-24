using System.Diagnostics;
using System.Reflection;
using JetBrains.Util.Util;
using org.jacodb.api.net.generated.models;
using TACBuilder.ILReflection;


namespace TACBuilder;

public class AppTacBuilder
{
    public void Build(Assembly assembly)
    {
        IlInstanceBuilder.BuildFrom(assembly);
        foreach (var asm in IlInstanceBuilder.GetAssemblies())
            BuiltAssemblies.Add(asm);
    }

    public void Build(string asmPath)
    {
        Debug.Assert(File.Exists(asmPath));
        IlInstanceBuilder.BuildFrom(asmPath);
        foreach (var asm in IlInstanceBuilder.GetAssemblies())
            BuiltAssemblies.Add(asm);
    }

    public void Build(AssemblyName asmName)
    {
        IlInstanceBuilder.BuildFrom(asmName);
        foreach (var asm in IlInstanceBuilder.GetAssemblies())
            BuiltAssemblies.Add(asm);
    }

    public HashSet<IlAssembly> BuiltAssemblies { get; } = [];

    public static void IncludeTACBuilder()
    {
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.GetName().ToString().StartsWith("TACBuilder"));
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.GetName().ToString().StartsWith("TACBuilder"));
    }

    public static void IncludeRootAsm(string rootAssemblyPath)
    {
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.Location == rootAssemblyPath);
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath);
    }

    public static void IncludeRootAsm(AssemblyName asmName)
    {
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.GetName().FullName == asmName.FullName);
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.GetName().FullName == asmName.FullName);
    }

    public static List<IlType> GetFreshInstances()
    {
        return IlInstanceBuilder.GetFreshTypes();
    }

    public static List<IlAssembly> GetBuiltAssemblies()
    {
        return IlInstanceBuilder.GetAssemblies();
    }

    public IlType MakeGenericType(TypeId typeId)
    {
        return IlInstanceBuilder.GetType(MakeGenericTypeFrom(typeId));
    }

    private Type MakeGenericTypeFrom(TypeId typeId)
    {
        var topLevelType = FindTypeUnsafe(typeId.AsmName, typeId.TypeName);
        Debug.Assert(topLevelType.IsGenericTypeDefinition || !topLevelType.IsGenericType, topLevelType.ToString());
        return typeId.TypeArgs.Count == 0
            ? topLevelType
            : topLevelType.MakeGenericType(typeId.TypeArgs.Select(t => MakeGenericTypeFrom((TypeId)t)).ToArray());
    }

    private Type FindTypeUnsafe(string asmName, string typeName)
    {
        var asm = BuiltAssemblies.Single(asm => asm.Name == asmName);
        var possibleTypes = asm.Types.Where(t => t.FullName == typeName).Select(ilt => ilt.Type).ToList();
        return possibleTypes.Single();
    }
}