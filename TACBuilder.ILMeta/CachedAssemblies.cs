using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

using AssemblyPath = string;

public partial class AssemblyMeta
{
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

        public new Assembly LoadFromAssemblyPath(AssemblyPath path)
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
        // TODO use in asmName resolver
        // public string ResolvePathFromName(AssemblyName name)
        // {
        //     foreach (var resolver in _resolvers.Values)
        //     {
        //         var path = resolver.ResolveAssemblyToPath(name);
        //         if (path != null)
        //         {
        //             return path;
        //         }
        //     }
        //
        //     throw new Exception("cannot resolve path for " + name.FullName);
        // }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }
    }

    public static class CachedAssemblies
    {
        private static readonly AsmLoadContext _context = new();

        private static readonly Dictionary<AssemblyPath, AssemblyMeta> _cache = new();

        public static AssemblyMeta Get(AssemblyPath path)
        {
            if (!_cache.ContainsKey(path))
            {
                _cache.Add(path, new AssemblyMeta(_context.LoadFromAssemblyPath(path)));
            }

            return _cache[path];
        }

        private static AssemblyMeta Get(Assembly assembly)
        {
            return GetOrInsert(assembly.Location, assembly);
        }

        public static AssemblyMeta Get(AssemblyName name)
        {
            var asm = _context.LoadFromAssemblyName(name);
            return GetOrInsert(asm.Location, asm);
        }

        public static List<AssemblyMeta> GetAll()
        {
            return _cache.Values.ToList();
        }

        private static AssemblyMeta GetOrInsert(AssemblyPath path, Assembly assembly)
        {
            if (!_cache.ContainsKey(path))
            {
                _cache.Add(path, new AssemblyMeta(assembly));
            }

            return _cache[path];
        }
    }
}
