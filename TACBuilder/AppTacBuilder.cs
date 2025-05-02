using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using org.jacodb.api.net.generated.models;
using TACBuilder.ILReflection;
using TACBuilder.ReflectionUtils;


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

    
    public IlType? GetType(TypeId typeId)
    {
        var gt = MakeTypeFrom(typeId);
        if (gt == null) return null;
        
        var result = IlInstanceBuilder.GetType(gt);
        IlInstanceBuilder.Construct();
        return result;
    }

    private Type? MakeTypeFrom(TypeId typeId)
    {
        var typeIsGenericParam = typeId.TypeName.Contains('!');

        var topLevelType = typeIsGenericParam
            ? FindGenericParameter(typeId.AsmName, typeId.TypeName)
            : FindType(typeId.AsmName, typeId.TypeName);

        if (topLevelType == null)
        {
            return null;
        }

        if (typeId.TypeArgs.Count == 0) return topLevelType;

        var (groundType, qualifiers) = topLevelType.GroundAndQualifiers();

        var args = new List<Type>();
        foreach (var rawArg in typeId.TypeArgs)
        {
            if (rawArg is not TypeId argTypeId) throw new SerializationException("typeId expected");
            var arg = MakeTypeFrom(argTypeId);
            if (arg == null) return null;
            args.Add(arg);
        }

        try
        {
            var substitution = groundType.MakeGenericType(args.ToArray());
            var qualifiedSubst = substitution.AttachQualifiers(qualifiers);
            return qualifiedSubst;
        }
        catch
        {
            return null;
        }
    }

    private Type? FindType(string asmName, string typeName)
    {
        var asm = BuiltAssemblies.SingleOrDefault(asm => asm?.Name == asmName, defaultValue: null);
        var type = asm?._assembly.GetType(typeName);
        return type;
    }

    private Type? FindGenericParameter(string asmName, string typeName)
    {
        Debug.Assert(typeName.Contains('!'));

        var typeNameTokens = typeName.Split('!');
        Debug.Assert(typeNameTokens.Length == 2);
        var (declTypeName, paramName) = (typeNameTokens[0], typeNameTokens[1]);

        var asm = BuiltAssemblies.SingleOrDefault(asm => asm?.Name == asmName, defaultValue: null);
        var declType = asm?._assembly.GetType(declTypeName);

        if (declType == null) return null;

        Debug.Assert(declType.IsGenericTypeDefinition);
        return declType.GetGenericArguments().FirstOrDefault(p => p.Name == paramName);
    }
}