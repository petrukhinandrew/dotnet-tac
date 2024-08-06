using System.Reflection;

namespace Usvm.IL.Parser;

public static class ReflectionUtils
{
    public static IEnumerable<Type> GetTypesChecked(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            // TODO: pass logger here and show warnings
            return e.Types.Where(t => t is not null)!;
        }
    }
}
