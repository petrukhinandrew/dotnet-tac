using System.Reflection;

namespace Usvm.IL.Utils;

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
            return e.Types.Where(t => t is not null)!;
        }
    }
}