using TACBuilder.ILMeta;
using TACBuilder.ILMeta.ILBodyParser;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder.ILTAC;

public class TACMethodInfo
{
    // TODO pass signature here instead
    public MethodMeta Meta;
    public List<ILLocal> Locals = new();
    public List<ILLocal> Params = new();
    public List<ILExpr> Temps = new();
    public List<ILExpr> Errs = new();
    public List<EHScope> Scopes = new();
}

public class TACMethod(TACMethodInfo info, List<ILIndexedStmt> statements)
{
    public TACMethodInfo Info => info;
    public List<ILIndexedStmt> Statements => statements;

    public void SerializeTo(Stream to)
    {
        this.DumpAllTo(to);
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

    private static List<string> FormatTempVars(this TACMethod method)
    {
        return FormatAnyVars(method.Info.Temps, NamingUtil.TempVar);
    }

    private static List<string> FormatLocalVars(this TACMethod method)
    {
        return FormatAnyVars(method.Info.Locals.Select(v => (ILExpr)v).ToList(), NamingUtil.LocalVar);
    }

    private static List<string> FormatErrVars(this TACMethod method)
    {
        return FormatAnyVars(method.Info.Errs, NamingUtil.ErrVar);
    }

    private static string FormatMethodSignature(this TACMethod method)
    {
        var meta = method.Info.Meta;
        ILType retType = TypingUtil.ILTypeFrom(meta.ReturnType.BaseType);
        return string.Format("{0} {1}({2})", retType, meta.Name,
            string.Join(", ",
                meta.MethodBase.GetParameters().Select(mi => TypingUtil.ILTypeFrom(mi.ParameterType).ToString())));
    }

    private static void DumpMethodSignature(this TACMethod method, StreamWriter writer)
    {
        writer.WriteLine(method.FormatMethodSignature());
    }

    private static void DumpEHS(this TACMethod method, StreamWriter writer)
    {
        foreach (var scope in method.Info.Scopes)
        {
            writer.WriteLine(scope.ToString());
        }
    }

    private static void DumpVars(this TACMethod method, StreamWriter writer)
    {
        foreach (var v in method.FormatLocalVars().Concat(method.FormatTempVars()).Concat(method.FormatErrVars()))
        {
            writer.WriteLine(v);
        }
    }

    private static void DumpTAC(this TACMethod method, StreamWriter writer)
    {
        foreach (var line in method.Statements)
        {
            writer.WriteLine(line.ToString());
        }
    }

    public static void DumpIL(this TACMethod method, StreamWriter writer)
    {
        int index = 0;
        var instr = method.Info.Meta.FirstInstruction;
        while (instr is not ILInstr.Back)
        {
            var arg = instr.arg is ILInstrOperand.NoArg ? "" : instr.arg.ToString();
            writer.WriteLine($"IL_{++index} {instr} {arg}");
            instr = instr.next;
        }
    }

    public static void DumpAllTo(this TACMethod method, Stream to)
    {
        var writer = new StreamWriter(to, leaveOpen: true);
        writer.AutoFlush = true;
        // method.DumpIL(writer);
        method.DumpMethodSignature(writer);
        // method.DumpEHS(writer);
        method.DumpVars(writer);
        method.DumpTAC(writer);
        writer.WriteLine();
        writer.Close();
    }
}
