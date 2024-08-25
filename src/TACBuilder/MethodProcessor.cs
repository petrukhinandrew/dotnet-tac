
using System.Reflection;
using Usvm.IL.TypeSystem;
using Usvm.IL.Parser;
using Microsoft.VisualBasic;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace Usvm.IL.TACBuilder;

class MethodProcessor
{
    public Module DeclaringModule;
    public MethodInfo MethodInfo;
    private ehClause[] _ehs;
    public List<ILLocal> Locals;
    public List<ILLocal> Params;
    public List<ILExpr> Temps = new List<ILExpr>();
    public List<ILExpr> Errs = new List<ILExpr>();
    public List<ILStmt> Tac = new List<ILStmt>();
    public Dictionary<SMFrame, List<int>> Successors = new Dictionary<SMFrame, List<int>>();
    private Dictionary<int, SMFrame?> _blocks;
    private ILInstr _begin;
    public List<EHScope> Scopes = [];

    private int _stmtIndex = 0;
    public int StmtIndex
    {
        get
        {
            return _stmtIndex++;
        }
    }
    private Dictionary<int, ILInstr> _ilBlocks;
    public ILInstr GetILInstr(int idx)
    {
        return _ilBlocks[idx];
    }
    public MethodProcessor(Module declaringModule, MethodInfo methodInfo, IList<LocalVariableInfo> locals, Dictionary<int, ILInstr> blocks, ILInstr begin, ehClause[] ehs)
    {
        _begin = begin;
        _blocks = new Dictionary<int, SMFrame?>();
        _ilBlocks = blocks;
        foreach (var b in blocks)
        {
            _blocks.Add(b.Key, null);
        }
        _ehs = ehs;
        DeclaringModule = declaringModule;
        MethodInfo = methodInfo;
        Params = methodInfo.GetParameters().OrderBy(p => p.Position).Select(l => new ILLocal(TypeSolver.Resolve(l.ParameterType), Logger.ArgVarName(l.Position))).ToList();
        Locals = locals.OrderBy(l => l.LocalIndex).Select(l => new ILLocal(TypeSolver.Resolve(l.LocalType), Logger.LocalVarName(l.LocalIndex))).ToList();
        InitEHScopes();
        ProcessIL();
        ComposeTac(0);
    }
    public ILInstr GetBeginInstr()
    {
        return _begin;
    }
    private void ProcessIL()
    {
        _blocks[0] = SMFrame.CreateInitial(this);
        List<int> queue = [0];
        while (queue.Count > 0)
        {
            var cur = queue.First();
            queue.RemoveAt(0);
            var branches = _blocks[cur]!.Branch();
            Successors.TryAdd(_blocks[cur]!, []);
            Successors.GetValueOrDefault(_blocks[cur]!, []).AddRange(branches);
            foreach (var b in branches)
            {
                if (_blocks[b] == null)
                {
                    _blocks[b] = SMFrame.ContinueFrom(_blocks[cur]!, b);
                    queue.Add(b);
                }
            }
        }
    }
    private void ComposeTac(int idx)
    {
        Tac.AddRange(_blocks[idx]!.TacLines);
        foreach (var t in Successors[_blocks[idx]!])
        {
            ComposeTac(t);
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

    public FieldInfo ResolveField(int target)
    {
        return DeclaringModule.ResolveField(target, MethodInfo.DeclaringType!.GetGenericArguments(), MethodInfo.GetGenericArguments()) ?? throw new Exception("cannot resolve field");

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