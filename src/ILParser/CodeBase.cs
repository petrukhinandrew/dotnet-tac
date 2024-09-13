using System.ComponentModel.Design;
using System.Reflection;
using System.Runtime.Loader;
using Usvm.IL.TACBuilder;
using Usvm.IL.TypeSystem;
using Usvm.IL.Utils;


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

        // Console.WriteLine("asm: {0}", asm.GetName().ToString());
        // NamingUtil.PrintSeparator();
        // foreach (var mod in asm.GetModules().Where(m => m != null))
        // {
        //     Console.WriteLine("module {0}", mod.ScopeName);
        // }
        // foreach (var mod in asm.GetLoadedModules().Where(m => m != null))
        // {
        //     Console.WriteLine("loaded {0}", mod.Name);
        // }

        foreach (Module module in asm.GetLoadedModules())
        {
            foreach (Type type in module.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(Flags).Where(tm => _settings.Methods.Contains(tm.Name)))
                {
                    MethodBody? methodBody;
                    if ((methodBody = method.GetMethodBody()) != null)
                    {
                        ILType ilType = TypeSolver.Resolve(type);
                        Console.WriteLine(ilType.ToString());
                        try
                        {
                            ILRewriter r = new ILRewriter(module, ILRewriterDumpMode.ILAndEHS);

                            r.ImportIL(methodBody);
                            r.ImportEH(methodBody);
                            MethodProcessor mp = new MethodProcessor(module, method, methodBody.LocalVariables, r.GetBeginning(), r.GetEHs());
                            mp.DumpAll();

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.TargetSite);
                            Console.WriteLine(e.StackTrace);
                            throw new Exception("Caught on {0}: " + method.Name);
                        }
                    }

                }
            }
        }
    }

    public List<string> ListAssemblies()
    {
        return _assemblies.Select(asm => asm.Value.FullName ?? asm.Value.GetName().FullName).ToList();
    }

    public List<string> ListTypes()
    {
        return _types.Select(t => t.Value.FullName ?? t.Value.Name).ToList();
    }

    public List<string> ListMethods()
    {
        return _types.SelectMany(t => t.Value.GetMethods()).ToList().Select(mi => mi.Name).ToList();
    }
    public List<string> ListModules()
    {
        return [];
    }
}