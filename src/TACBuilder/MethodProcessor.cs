using System.Reflection;
using Usvm.IL.TypeSystem;
using Usvm.IL.Parser;

namespace Usvm.IL.TACBuilder;

class MethodProcessor
{
    public Module DeclaringModule;
    public MethodInfo MethodInfo;
    private ehClause[] _ehs;
    public List<ILLocal> Locals;
    public List<ILLocal> Params;
    public List<ILExpr> Temps = new();
    public List<ILExpr> Errs = new();
    public List<ILIndexedStmt> Tac = new();
    public List<ILInstr> Leaders;
    public Dictionary<int, List<int>> Successors = new();
    public Dictionary<int, SMFrame> TacBlocks;
    private ILInstr _begin;
    public List<EHScope> Scopes = [];
    public Dictionary<int, int?> ilToTacMapping = new();

    public MethodProcessor(Module declaringModule, MethodInfo methodInfo, IList<LocalVariableInfo> locals,
        ILInstr begin, ehClause[] ehs)
    {
        _begin = begin;
        TacBlocks = new Dictionary<int, SMFrame>();
        _ehs = ehs;
        DeclaringModule = declaringModule;
        MethodInfo = methodInfo;
        Params = methodInfo.GetParameters().OrderBy(p => p.Position).Select(l =>
            new ILLocal(TypeSolver.Resolve(l.ParameterType), Logger.ArgVarName(l.Position))).ToList();
        Locals = locals.OrderBy(l => l.LocalIndex)
            .Select(l => new ILLocal(TypeSolver.Resolve(l.LocalType), Logger.LocalVarName(l.LocalIndex))).ToList();
        InitEHScopes();
        Leaders = CollectLeaders();
        ProcessNonExceptionalIL();
        ProcessEHScopesIL();
        MergeStacks();
        foreach (var bb in TacBlocks)
        {
            bb.Value.InsertExtraAssignments();
        }

        ComposeTac();
    }

    public ILInstr GetBeginInstr()
    {
        return _begin;
    }

    private List<ILInstr> CollectLeaders()
    {
        ILInstr cur = _begin;
        HashSet<ILInstr> leaders = [cur];
        while (cur is not ILInstr.Back)
        {
            if (cur.isJump())
            {
                leaders.Add(((ILInstrOperand.Target)cur.arg).value);
                leaders.Add(cur.next);
            }

            cur = cur.next;
        }

        return [.. leaders.OrderBy(l => l.idx)];
    }

    private void ProcessNonExceptionalIL()
    {
        TacBlocks[0] = SMFrame.CreateInitial(this);
        Successors.Add(0, []);
        TacBlocks[0].Branch();
    }

    private void MergeStacks()
    {
        Queue<KeyValuePair<int, SMFrame>> q = new();
        foreach (var bb in TacBlocks.OrderByDescending(b1 => b1.Key))
        {
            if (GetPredecessorsOf(bb.Key).Count > 1)
            {
                q.Enqueue(bb);
            }
        }

        while (q.Count > 0)
        {
            var bb = q.Dequeue();
            if (bb.Value.MergeStacksFrom(GetPredecessorsOf(bb.Key)))
            {
                foreach (var sb in TacBlocks.Where(p => Successors[bb.Key].Contains(p.Key)))
                {
                    q.Enqueue(sb);
                }
            }
        }
    }

    private void ProcessEHScopesIL()
    {
        foreach (var scope in Scopes)
        {
            int hbIndex = scope.ilLoc.hb.idx;
            Leaders.Add(scope.ilLoc.hb);
            if (scope is CatchScope catchScope)
            {
                TacBlocks[hbIndex] = new SMFrame(this, null, new Stack<ILExpr>([Errs[catchScope.ErrIdx]]),
                    (ILInstr.Instr)catchScope.ilLoc.hb);
            }
            else if (scope is FilterScope filterScope)
            {
                int fbIndex = filterScope.fb.idx;
                Leaders.Add(filterScope.fb);
                TacBlocks[fbIndex] = new SMFrame(this, null, new Stack<ILExpr>([Errs[filterScope.ErrIdx]]),
                    (ILInstr.Instr)filterScope.fb);
                TacBlocks[hbIndex] = new SMFrame(this, null, new Stack<ILExpr>([Errs[filterScope.ErrIdx]]),
                    (ILInstr.Instr)filterScope.ilLoc.hb);
                TacBlocks[fbIndex].Branch();
            }
            else
            {
                TacBlocks[hbIndex] = new SMFrame(this, null, new Stack<ILExpr>(), (ILInstr.Instr)scope.ilLoc.hb);
            }

            TacBlocks[hbIndex].Branch();
        }
    }

    private void ComposeTac()
    {
        int lineNum = 0;
        foreach (var m in Successors)
        {
            ilToTacMapping.Add(m.Key, null);
        }

        var tacBlocksIndexed = TacBlocks.OrderBy(b => b.Key).ToList();
        foreach (var (i, bb) in tacBlocksIndexed.Select((e, i) => (i, e.Value)))
        {
            ilToTacMapping[bb.ILFirst] = lineNum;
            foreach (var line in bb.TacLines)
            {
                Tac.Add(new ILIndexedStmt(lineNum++, line));
            }

            if (tacBlocksIndexed.Count > i + 1 && Successors[bb.ILFirst][0] != tacBlocksIndexed[i + 1].Key)
            {
                Tac.Add(new ILIndexedStmt(lineNum++, new ILGotoStmt(Successors[bb.ILFirst][0])));
            }
        }

        foreach (var stmt in Tac)
        {
            if (stmt.Stmt is ILBranchStmt branch)
            {
                branch.Target = (int)ilToTacMapping[branch.Target]!;
            }
        }
    }

    private void InitEHScopes()
    {
        foreach (var ehc in _ehs)
        {
            EHScope scope = EHScope.FromClause(ehc);
            if (!Scopes.Contains(scope))
            {
                if (scope is EHScopeWithVarIdx s)
                {
                    s.ErrIdx = Errs.Count;
                    Errs.Add(new ILLocal(TypeSolver.Resolve(s.Type), Logger.ErrVarName(s.ErrIdx)));
                }

                Scopes.Add(scope);
            }
        }
    }

    public List<SMFrame> GetPredecessorsOf(int idx)
    {
        List<int> indices = Successors.Where((p) => p.Value.Contains(idx)).Select(p => p.Key).ToList();
        return TacBlocks.Where(p => indices.Contains(p.Key)).Select(p => p.Value).ToList();
    }

    public FieldInfo ResolveField(int target)
    {
        return DeclaringModule.ResolveField(target, MethodInfo.DeclaringType!.GetGenericArguments(),
            MethodInfo.GetGenericArguments()) ?? throw new Exception("cannot resolve field");
    }

    public Type ResolveType(int target)
    {
        return DeclaringModule.ResolveType(target) ?? throw new Exception("cannot resolve type");
    }

    public MethodBase ResolveMethod(int target)
    {
        return DeclaringModule.ResolveMethod(target) ?? throw new Exception("cannot resolve method");
    }

    public byte[] ResolveSignature(int target)
    {
        return DeclaringModule.ResolveSignature(target);
    }

    public string ResolveString(int target)
    {
        return DeclaringModule.ResolveString(target);
    }
}