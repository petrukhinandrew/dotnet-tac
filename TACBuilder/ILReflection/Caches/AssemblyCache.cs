using System.Reflection;

namespace TACBuilder.ILReflection;

using AssemblyPath = string;

internal class AssemblyCache
{
    private readonly AsmLoadContext _context = new();

    private readonly Dictionary<AssemblyPath, Assembly> _cache = new();

    private Assembly Get(Assembly assembly)
    {
        return GetOrInsert(assembly.Location, assembly);
    }

    private Assembly GetOrInsert(AssemblyPath path, Assembly assembly)
    {
        _cache.TryAdd(path, assembly);

        return _cache[path];
    }

    public Assembly Get(AssemblyPath path)
    {
        if (!_cache.ContainsKey(path))
        {
            _cache.Add(path, _context.LoadFromAssemblyPath(path));
        }

        return _cache[path];
    }

    public Assembly Get(AssemblyName name)
    {
        var asm = _context.LoadFromAssemblyName(name);
        return GetOrInsert(asm.Location, asm);
    }
}
