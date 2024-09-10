using Usvm.IL.Parser;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

static class MethodFormatter
{
    private static List<string> FormatAnyVars(List<ILExpr> vars, Func<int, string> nameGen)
    {
        List<string> res = new List<string>();
        Dictionary<ILType, List<int>> typeGroupping = new Dictionary<ILType, List<int>>();
        for (int i = 0; i < vars.Count; i++)
        {
            if (!typeGroupping.ContainsKey(vars[i].Type))
            {
                typeGroupping.Add(vars[i].Type, []);
            }

            typeGroupping[vars[i].Type].Add(i);
        }

        foreach (var mapping in typeGroupping)
        {
            string buf = string.Format("{0} {1}", mapping.Key.ToString(),
                string.Join(", ", mapping.Value.Select(v => nameGen(v))));
            res.Add(buf);
        }

        return res;
    }

    private static List<string> FormatTempVars(this MethodProcessor mp)
    {
        return FormatAnyVars(mp.Temps, Logger.TempVarName);
    }

    private static List<string> FormatLocalVars(this MethodProcessor mp)
    {
        return FormatAnyVars(mp.Locals.Select(v => (ILExpr)v).ToList(), Logger.LocalVarName);
    }

    private static List<string> FormatErrVars(this MethodProcessor mp)
    {
        return FormatAnyVars(mp.Errs, Logger.ErrVarName);
    }

    private static string FormatMethodSignature(this MethodProcessor mp)
    {
        ILType retType = TypeSolver.Resolve(mp.MethodInfo.ReturnType);
        return string.Format("{0} {1}({2})", retType.ToString(), mp.MethodInfo.Name,
            string.Join(", ",
                mp.MethodInfo.GetParameters().Select(mi => TypeSolver.Resolve(mi.ParameterType).ToString())));
    }

    public static void DumpMethodSignature(this MethodProcessor mp)
    {
        Console.WriteLine(mp.FormatMethodSignature());
    }

    public static void DumpEHS(this MethodProcessor mp)
    {
        foreach (var scope in mp.Scopes)
        {
            Console.WriteLine(scope.ToString());
        }
    }

    public static void DumpVars(this MethodProcessor mp)
    {
        foreach (var v in mp.FormatLocalVars().Concat(mp.FormatTempVars()).Concat(mp.FormatErrVars()))
        {
            Console.WriteLine(v);
        }
    }

    public static void DumpBBs(this MethodProcessor mp)
    {
        foreach (var e in mp.TacBlocks.OrderBy(b => b.Key))
        {
            Console.WriteLine("BB#" + e.Key);
            foreach (var l in e.Value.TacLines)
            {
                Console.WriteLine(l.ToString());
            }

            Console.WriteLine();
        }
    }

    public static void DumpSuccessors(this MethodProcessor mp)
    {
        foreach (var s in mp.Successors.OrderBy(e => e.Key))
        {
            Console.WriteLine("[{0} -> {2}]: {1}", s.Key,
                string.Join(" ", s.Value.Select(il => string.Format("[{0} -> {1}]", il, mp.ilToTacMapping[il]))),
                mp.ilToTacMapping[s.Key]);
        }
    }

    public static void DumpTAC(this MethodProcessor mp)
    {
        foreach (var line in mp.Tac)
        {
            Console.WriteLine(line.ToString());
        }
    }

    public static void DumpAll(this MethodProcessor mp)
    {
        mp.DumpSuccessors();
        mp.DumpMethodSignature();
        // mp.DumpEHS();
        mp.DumpVars();
        // mp.DumpBBs();
        mp.DumpTAC();
    }
}