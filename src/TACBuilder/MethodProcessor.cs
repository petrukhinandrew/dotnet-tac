
using System.Reflection;
using Usvm.IL.TypeSystem;
using Usvm.IL.Parser;
using Microsoft.VisualBasic;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;
using System.ComponentModel.Design.Serialization;

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
    public List<ILInstr> Leaders;
    public Dictionary<int, List<int>> Successors = new Dictionary<int, List<int>>();
    public Dictionary<int, SMFrame> TacBlocks;
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

    public MethodProcessor(Module declaringModule, MethodInfo methodInfo, IList<LocalVariableInfo> locals, ILInstr begin, ehClause[] ehs)
    {
        _begin = begin;
        TacBlocks = new Dictionary<int, SMFrame>();
        _ehs = ehs;
        DeclaringModule = declaringModule;
        MethodInfo = methodInfo;
        Params = methodInfo.GetParameters().OrderBy(p => p.Position).Select(l => new ILLocal(TypeSolver.Resolve(l.ParameterType), Logger.ArgVarName(l.Position))).ToList();
        Locals = locals.OrderBy(l => l.LocalIndex).Select(l => new ILLocal(TypeSolver.Resolve(l.LocalType), Logger.LocalVarName(l.LocalIndex))).ToList();
        InitEHScopes();
        Leaders = CollectLeaders();
        ProcessIL();
        ComposeTac(0);
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
            if (!cur.isJump())
            {
                cur = cur.next;
                continue;
            }
            if (cur is ILInstr.SwitchArg)
            {
                // TODO handle switch tables 
            }
            else
            {
                leaders.Add(((ILInstrOperand.Target)cur.arg).value);
                leaders.Add(cur.next);
            }
            cur = cur.next;
        }
        return [.. leaders.OrderBy(l => l.idx)];
    }

    private void ProcessIL()
    {
        TacBlocks[0] = SMFrame.CreateInitial(this);
        Successors.Add(0, []);
        TacBlocks[0].Branch();
    }
    private void ComposeTac(int idx)
    {
        Tac.AddRange(TacBlocks[idx].TacLines);
        foreach (var t in Successors.GetValueOrDefault(idx, []).Order())
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