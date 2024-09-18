using System.Reflection;
using System.Runtime.Loader;
using TACBuilder.ILMeta;
using TACBuilder.ILMeta.ILBodyParser;
using Usvm.IL.TACBuilder;


namespace Usvm.IL.Parser;

class CodeBase : AssemblyLoadContext
{
    private Dictionary<string, Assembly> _assemblies = new();
    private Dictionary<string, AssemblyDependencyResolver> _resolvers = new();
    private Dictionary<string, Type> _types = new();
    private ParserSettings _settings;

    public CodeBase(ParserSettings settings)
    {
        _settings = settings;
    }

    private const BindingFlags Flags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static string GetTypeName(Type t)
    {
        return t.AssemblyQualifiedName ?? t.FullName ?? t.Name;
    }

    private new Assembly LoadFromAssemblyPath(string path)
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

        foreach (var t in asm.GetTypesChecked())
        {
            var type = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
            if (type.FullName is null)
            {
                // Case for types, that contains open generic parameters
                continue;
            }

            _types[GetTypeName(type)] = type;
        }

        return asm;
    }

    public void Load()
    {
        Assembly asm = LoadFromAssemblyPath(_settings.DllPath);
        
        List<string> disabledTypes = ["AutoGeneratedProgram"];
        List<string> selectedTypes = [];
        bool selectedMethods(MethodInfo method) =>
            _settings.Methods.Contains(method.Name) && method.GetMethodBody() != null;

        bool allMethods(MethodInfo method) => method.GetMethodBody() != null;
        
        foreach (Module module in asm.GetLoadedModules())
        {
            foreach (Type type in module.GetTypes())
            {
                // foreach (MethodInfo method in type.GetMethods(Flags))
                foreach (MethodInfo method in type.GetMethods(Flags).Where(allMethods))
                {
                    MethodBody? methodBody;
                    if ((methodBody = method.GetMethodBody()) != null)
                    {
                        try
                        {
                            var r = new ILBodyParser(methodBody);
    
                            r.Parse();
                            MethodTacBuilder mp = new MethodTacBuilder(module, method, methodBody.LocalVariables,
                                r.Instructions, r.EhClauses);
                            // mp.DumpAll();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(method.DeclaringType?.Name +" " + method.Name);
                            Console.WriteLine(e);
                            // Console.WriteLine(e.StackTrace);
                        }
                    }
                }
            }
        }
    }
}