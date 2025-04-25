using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using CommandLine;
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
    public static void IncludeMsCorLib()
    {
        IlInstanceBuilder.AddTypeFilter(type =>
            type.Assembly.GetName().ToString().StartsWith("System.Private.CoreLib"));
        IlInstanceBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.GetName().ToString().StartsWith("System.Private.CoreLib"));
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

    
    public IlType? MakeGenericType(TypeId typeId)
    {
        Console.WriteLine("HUI 981291821");
        var gt = MakeGenericTypeFrom(typeId);
        Console.WriteLine("HUI 575757");
        if (gt == null) return null;
        
        var result = IlInstanceBuilder.GetType(gt);
        IlInstanceBuilder.Construct();
        Console.WriteLine("HUI 82828228");
        return result;
    }

    private Type? MakeGenericTypeFrom(TypeId typeId)
    {
        try
        {
            var topLevelType = FindTypeUnsafe(typeId.AsmName, typeId.TypeName);
            if (topLevelType == null) return null;
            if (typeId.TypeArgs.Count == 0) return topLevelType;
            var args = new List<Type>();
            foreach (var rawArg in typeId.TypeArgs)
            {
                if (rawArg is not TypeId argTypeId) throw new SerializationException("typeId expected");
                var arg = MakeGenericTypeFrom(argTypeId);
                if (arg == null) return null;
                args.Add(arg);
            }

            return topLevelType.MakeGenericType(args.ToArray());
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return null;
        }
    }

    private Type? FindTypeUnsafe(string asmName, string typeName)
    {
        var asm = BuiltAssemblies.Single(asm => asm.Name == asmName);
        var possibleTypes = asm.Types.Where(t => t.FullName == typeName && (!t.IsGenericType || t.IsGenericDefinition))
            .Select(ilt => ilt.Type).ToList();
        return possibleTypes.SingleOrDefault(defaultValue: null);
    }
}