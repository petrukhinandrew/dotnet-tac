using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.Loader;

namespace TACBuilder.ILReflection;

internal class AsmLoadContext : AssemblyLoadContext, IDisposable
{
    private readonly Dictionary<string, Assembly> _assemblies = new();
    private readonly Dictionary<string, AssemblyDependencyResolver> _resolvers = new();

    public AsmLoadContext() : base("lolkek")
    {
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    public event Func<string, string?>? ExtraResolver;

    public IEnumerable<string> DependenciesDirs { get; set; } = new List<string>();

    private Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var existingInstance = Assemblies.FirstOrDefault(assembly => assembly.FullName == args.Name);
        if (existingInstance != null)
        {
            return existingInstance;
        }

        var extraResolverPath = ExtraResolver?.Invoke(args.Name);

        if (extraResolverPath is not null && File.Exists(extraResolverPath))
        {
            return LoadFromAssemblyPath(extraResolverPath);
        }

        foreach (var path in DependenciesDirs)
        {
            var assemblyPath = Path.Combine(path, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath))
                continue;
            var assembly = LoadFromAssemblyPath(assemblyPath);
            return assembly;
        }

        return null;
    }

    public new Assembly LoadFromAssemblyPath(System.String path)
    {
        if (_assemblies.TryGetValue(path, out var assembly))
        {
            return assembly;
        }

        if (!_resolvers.ContainsKey(path))
        {
            _resolvers[path] = new AssemblyDependencyResolver(path);
        }

        var asm = base.LoadFromAssemblyPath(path);
        _assemblies.Add(path, asm);
        return asm;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        foreach (var (_, resolver) in _resolvers)
        {
            var path = resolver.ResolveAssemblyToPath(assemblyName);
            if (path != null)
            {
                return LoadFromAssemblyPath(path);
            }
        }

        return null;
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
    }
}

class AssemblyMock
{
    public AssemblyBuilder Builder;
    public AsmLoadContext Context = new();
    public ModuleBuilder Module;
    public List<TypeBuilder> Types = new();

    public AssemblyMock()
    {
        var scope = Context.EnterContextualReflection();
        using (scope)
        {
            Builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.RunAndCollect);
            Module = Builder.DefineDynamicModule("MainModule");
        }
    }

    public Type AddType(TypeBuilder typeBuilder)
    {
        var dummy_method = typeBuilder.DefineMethod("Dummy", MethodAttributes.Public | MethodAttributes.Static);
        AddDummyMethod(dummy_method);
        var calli_test_method_builder = typeBuilder.DefineMethod("calli_test", MethodAttributes.Public | MethodAttributes.Static);
        AddMethod(calli_test_method_builder, dummy_method);
        return typeBuilder.CreateType();
    }

    public void AddDummyMethod(System.Reflection.Emit.MethodBuilder methodBuilder)
    {
        methodBuilder.SetReturnType(typeof(double));
        methodBuilder.SetParameters(typeof(int));
        var ilBuilder = methodBuilder.GetILGenerator();
        ilBuilder.Emit(OpCodes.Ldarg_0);
        ilBuilder.Emit(OpCodes.Ret);
    }

    public void AddMethod(System.Reflection.Emit.MethodBuilder methodBuilder, System.Reflection.Emit.MethodBuilder callTarget)
    {
        var callTargetSig = Module.GetSignatureMetadataToken(
            SignatureHelper.GetMethodSigHelper(Module, typeof(double), [typeof(int)])
        );
        var ilBuilder = methodBuilder.GetILGenerator();
        methodBuilder.SetReturnType(typeof(double));
        ilBuilder.Emit(OpCodes.Ldc_I4_4);
        ilBuilder.Emit(OpCodes.Ldftn, callTarget);
        ilBuilder.Emit(OpCodes.Calli, callTargetSig);
        ilBuilder.Emit(OpCodes.Ret);
    }

    public Assembly Build()
    {
        AddType(Module.DefineType("type1"));
        return Builder;
    }
}
