using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TACBuilder.ILReflection;

using AssemblyPath = string;

internal class AssemblyCache
{
    protected static readonly ILogger Logger = LoggerFactory
        .Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.None))
        .CreateLogger("Meta");

    private readonly AsmLoadContext _context = new();

    private readonly Dictionary<AssemblyPath, Assembly> _cache = new();

    private Assembly Get(Assembly assembly)
    {
        return GetOrInsert(assembly.Location, assembly);
    }

    private Assembly GetOrInsert(AssemblyPath path, Assembly assembly)
    {
        if (_cache.TryAdd(path, assembly))
        {
            DumpSingleAsmMeta(assembly);
        }

        return _cache[path];
    }

    private static void DumpSingleAsmMeta(Assembly assembly)
    {

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
