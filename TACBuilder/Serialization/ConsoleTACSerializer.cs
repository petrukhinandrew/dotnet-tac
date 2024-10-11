using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder.Serialization;

public class ConsoleTACSerializer(IEnumerable<ILAssembly> assemblies, StreamWriter stream) : TACSerializer
{
    private List<ILAssembly> _assemblies = assemblies.ToList();
    private StreamWriter _stream = stream;

    public void Serialize()
    {
        foreach (var asm in _assemblies)
        {
            SerializeAsm(asm);
        }
    }

    private void SerializeAsm(ILAssembly assembly)
    {
        _stream.WriteLine(assembly.Name);
        foreach (var type in assembly.Types)
        {
            SerializeType(type);
        }
    }

    private void SerializeAttrs(List<ILAttribute> attributes)
    {
    }

    private void SerializeType(ILType type)
    {
        if (!type.IsConstructed) return;
        SerializeAttrs(type.Attributes);
        _stream.WriteLine(type.Name + " {");


        foreach (var field in type.Fields)
        {
            SerializeTypeField(field);
        }

        _stream.WriteLine();

        foreach (var method in type.Methods)
        {
            SerializeMethod(method);
        }

        _stream.WriteLine("}");
    }

    private void SerializeTypeField(ILField field)
    {
        _stream.WriteLine(field.ToString());
    }

    private void SerializeMethod(ILMethod method)
    {
        method.DumpAllTo(_stream);
    }
}

internal static class TACMethodPrinter
{
    private static List<string> FormatAnyVars(IEnumerable<ILExpr> vars, Func<int, string> nameGen)
    {
        List<string> res = new List<string>();
        Dictionary<ILType, List<int>> typeGroupping = new Dictionary<ILType, List<int>>();
        foreach (var (i, v) in vars.Select((x, i) => (i, x)))
        {
            if (v.Type is null) continue;
            if (!typeGroupping.ContainsKey(v.Type))
            {
                typeGroupping.Add(v.Type, []);
            }

            typeGroupping[v.Type].Add(i);
        }

        foreach (var mapping in typeGroupping)
        {
            string buf = string.Format("{0} {1}", mapping.Key.ToString(),
                string.Join(", ", mapping.Value.Select(nameGen)));
            res.Add(buf);
        }

        return res;
    }

    private static List<string> FormatTempVars(this ILMethod method)
    {
        return FormatAnyVars(method.Temps.Values, NamingUtil.TempVar);
    }

    private static List<string> FormatLocalVars(this ILMethod method)
    {
        return FormatAnyVars(method.Locals.Select(v => (ILExpr)v).ToList(), NamingUtil.LocalVar);
    }

    private static List<string> FormatErrVars(this ILMethod method)
    {
        return FormatAnyVars(method.Errs, NamingUtil.ErrVar);
    }

    private static string FormatMethodSignature(this ILMethod method)
    {
        ILType retType = method.ReturnType ?? new ILType(typeof(void));
        return string.Format("{0} {1}({2})", retType, method.Name,
            string.Join(", ",
                method.Parameters.Select(mi => mi.ToString())));
    }

    private static void DumpMethodSignature(this ILMethod method, StreamWriter writer)
    {
        writer.WriteLine(method.FormatMethodSignature());
    }

    private static void DumpEHS(this ILMethod method, StreamWriter writer)
    {
        foreach (var scope in method.Scopes)
        {
            writer.WriteLine(scope.ToString());
        }
    }

    private static void DumpVars(this ILMethod method, StreamWriter writer)
    {
        foreach (var v in method.FormatLocalVars().Concat(method.FormatTempVars()).Concat(method.FormatErrVars()))
        {
            writer.WriteLine(v);
        }
    }

    private static void DumpTAC(this ILMethod method, StreamWriter writer)
    {
        if (method.Body is null) return;

        foreach (var line in method.Body.Lines)
        {
            writer.WriteLine(line.ToString());
        }
    }

    public static void DumpAllTo(this ILMethod method, StreamWriter writer)
    {
        writer.AutoFlush = true;
        method.DumpMethodSignature(writer);
        // call.DumpEHS(writer);
        method.DumpVars(writer);
        method.DumpTAC(writer);
        writer.WriteLine();
    }
}
