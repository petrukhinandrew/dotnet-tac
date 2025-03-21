using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.Utils;

namespace TACBuilder.Serialization;

public class ConsoleTacSerializer(IEnumerable<IlAssembly> assemblies, StreamWriter stream) : ITacSerializer
{
    private List<IlAssembly> _assemblies = assemblies.ToList();
    private StreamWriter _stream = stream;

    public void NewInstance(IlCacheable instance)
    {
        throw new NotImplementedException();
    }

    public void Serialize()
    {
        foreach (var asm in _assemblies)
        {
            SerializeAsm(asm);
        }
    }

    private void SerializeAsm(IlAssembly assembly)
    {
        _stream.WriteLine(assembly.Name);
        foreach (var type in assembly.Types)
        {
            SerializeType(type);
        }
    }

    private void SerializeAttrs(List<IlAttribute> attributes)
    {
    }

    private void SerializeType(IlType type)
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

    private void SerializeTypeField(IlField field)
    {
        _stream.WriteLine(field.ToString());
    }

    private void SerializeMethod(IlMethod method)
    {
        method.DumpAllTo(_stream);
    }
}

internal static class TACMethodPrinter
{
    private static List<string> FormatAnyVars(IEnumerable<IlExpr> vars, Func<int, string> nameGen)
    {
        List<string> res = new List<string>();
        Dictionary<IlType, List<int>> typeGroupping = new Dictionary<IlType, List<int>>();
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
            string buf = $"{mapping.Key} {string.Join(", ", mapping.Value.Select(nameGen))}";
            res.Add(buf);
        }

        return res;
    }

    private static List<string> FormatTempVars(this IlMethod method)
    {
        return FormatAnyVars(method.Temps.Values, NamingUtil.TempVar);
    }

    private static List<string> FormatLocalVars(this IlMethod method)
    {
        return FormatAnyVars(method.LocalVars.Select(v => (IlExpr)v).ToList(), NamingUtil.LocalVar);
    }

    private static List<string> FormatErrVars(this IlMethod method)
    {
        return FormatAnyVars(method.Errs, NamingUtil.ErrVar);
    }

    private static string FormatMethodSignature(this IlMethod method)
    {
        IlType retType = method.ReturnType ?? new IlType(typeof(void));
        return string.Format("{0} {1}({2})", retType, method.Name,
            string.Join(", ",
                method.Parameters.Select(mi => mi.ToString())));
    }

    private static void DumpMethodSignature(this IlMethod method, StreamWriter writer)
    {
        writer.WriteLine(method.FormatMethodSignature());
    }

    private static void DumpEHS(this IlMethod method, StreamWriter writer)
    {
        foreach (var scope in method.Scopes)
        {
            writer.WriteLine(scope.ToString());
        }
    }

    private static void DumpVars(this IlMethod method, StreamWriter writer)
    {
        foreach (var v in method.FormatLocalVars().Concat(method.FormatTempVars()).Concat(method.FormatErrVars()))
        {
            writer.WriteLine(v);
        }
    }

    private static void DumpTAC(this IlMethod method, StreamWriter writer)
    {
        if (method.Body is null) return;

        foreach (var line in method.Body.Lines)
        {
            writer.WriteLine(line.ToString());
        }
    }

    public static void DumpAllTo(this IlMethod method, StreamWriter writer)
    {
        writer.AutoFlush = true;
        method.DumpMethodSignature(writer);
        method.DumpEHS(writer);
        method.DumpVars(writer);
        method.DumpTAC(writer);
        writer.WriteLine();
    }
}
