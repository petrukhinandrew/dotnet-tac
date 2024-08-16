
using System.Reflection;
using System.Runtime.InteropServices;
using Usvm.IL.TypeSystem;
using Usvm.IL.Parser;

namespace Usvm.IL.TACBuilder;
abstract class ExceptionHandlingScope
{
    public ExceptionHandlingScope(int b, int e)
    {
        this.Begin = b;
        this.End = e;
    }
    public int Begin, End;
}
class TryScope(int b, int e) : ExceptionHandlingScope(b, e)
{
    public static TryScope FromClause(ehClause clause)
    {
        return new TryScope(clause.tryBegin.idx, clause.tryEnd.idx);
    }
    public override bool Equals(object? obj)
    {
        return obj != null && obj is TryScope ts && Begin == ts.Begin && End == ts.End;
    }
    public override int GetHashCode()
    {
        return (Begin, End).GetHashCode();
    }

}
class CatchScope(int b, int e, Type type) : ExceptionHandlingScope(b, e)
{
    public Type Type = type;
    public static CatchScope FromClause(ehClause clause)
    {
        return new CatchScope(clause.handlerBegin.idx, clause.handlerEnd.idx, (clause.ehcType as rewriterEhcType.CatchEH)!.type);
    }
    public override bool Equals(object? obj)
    {
        return obj != null && obj is TryScope ts && Begin == ts.Begin && End == ts.End;
    }
    public override int GetHashCode()
    {
        return (Begin, End).GetHashCode();
    }

}

class FilterScope(int b, int cb, int e) : ExceptionHandlingScope(b, e)
{
    public int CatchBegin = cb;

    public static FilterScope FromClause(ehClause clause)
    {
        return new FilterScope((clause.ehcType as rewriterEhcType.FilterEH)!.instr.idx, clause.handlerBegin.idx, clause.handlerEnd.idx);
    }
    public override bool Equals(object? obj)
    {
        return obj != null && obj is FilterScope ts && Begin == ts.Begin && End == ts.End && CatchBegin == ts.CatchBegin;
    }
    public override int GetHashCode()
    {
        return (Begin, End).GetHashCode();
    }
}

class FaultScope(int b, int e) : ExceptionHandlingScope(b, e)
{
    public static FaultScope FromClause(ehClause clause)
    {
        return new FaultScope(clause.handlerBegin.idx, clause.handlerEnd.idx);
    }
    public override bool Equals(object? obj)
    {
        return obj != null && obj is FaultScope ts && Begin == ts.Begin && End == ts.End;
    }
    public override int GetHashCode()
    {
        return (Begin, End).GetHashCode();
    }
}

class FinallyScope(int b, int e) : ExceptionHandlingScope(b, e)
{
    public static FinallyScope FromClause(ehClause clause)
    {
        return new FinallyScope(clause.handlerBegin.idx, clause.handlerEnd.idx);
    }
    public override bool Equals(object? obj)
    {
        return obj != null && obj is FinallyScope ts && Begin == ts.Begin && End == ts.End;
    }
    public override int GetHashCode()
    {
        return (Begin, End).GetHashCode();
    }
}

class StackMachine
{
    private Stack<ILExpr> _stack;
    private List<ILLocal> _locals;
    private List<ILLocal> _params;
    private List<ILExpr> _temps = new List<ILExpr>();
    private Dictionary<int, int?> _labels = new Dictionary<int, int?>();
    private int _nextTacLineIdx = 0;
    private List<ILStmt> _tac = new List<ILStmt>();
    private ILInstr _begin;
    private ehClause[] _ehs;
    private List<TryScope> _tryBlocks = [];
    private List<CatchScope> _catchBlocks = [];
    private List<FilterScope> _filterBlocks = [];
    private List<FinallyScope> _finallyBlocks = [];
    private List<FaultScope> _faultBlocks = [];
    private Module _declaringModule;
    private MethodInfo _methodInfo;

    public StackMachine(Module declaringModule, MethodInfo methodInfo, IList<LocalVariableInfo> locals, int maxDepth, ILInstr begin, ehClause[] ehs)
    {
        _begin = begin;
        _ehs = ehs;
        _declaringModule = declaringModule;
        _methodInfo = methodInfo;
        _params = _methodInfo.GetParameters().OrderBy(p => p.Position).Select(l => new ILLocal(TypeSolver.Resolve(l.ParameterType), Logger.ArgVarName(l.Position))).ToList();
        _locals = locals.OrderBy(l => l.LocalIndex).Select(l => new ILLocal(TypeSolver.Resolve(l.LocalType), Logger.LocalVarName(l.LocalIndex))).ToList();
        _stack = new Stack<ILExpr>(maxDepth);
        InitEHBlocs();
        IntroduceLabels();
        ProcessIL();
    }

    private void InitEHBlocs()
    {
        foreach (var ehc in _ehs)
        {
            var tryBlock = TryScope.FromClause(ehc);
            if (!_tryBlocks.Contains(tryBlock))
            {
                _tryBlocks.Add(tryBlock);
            }
            if (ehc.ehcType is rewriterEhcType.CatchEH)
            {
                var catchBlock = CatchScope.FromClause(ehc);
                if (!_catchBlocks.Contains(catchBlock))
                {
                    _catchBlocks.Add(catchBlock);
                }
            }
            if (ehc.ehcType is rewriterEhcType.FilterEH)
            {
                var filterBlock = FilterScope.FromClause(ehc);
                if (!_filterBlocks.Contains(filterBlock))
                {
                    _filterBlocks.Add(filterBlock);
                    Console.WriteLine("filter {0} {1} {2}", filterBlock.Begin, filterBlock.CatchBegin, filterBlock.End);
                }
            }
            if (ehc.ehcType is rewriterEhcType.FinallyEH)
            {
                var finallyBlock = FinallyScope.FromClause(ehc);
                if (!_finallyBlocks.Contains(finallyBlock))
                {
                    _finallyBlocks.Add(finallyBlock);
                }
            }
            if (ehc.ehcType is rewriterEhcType.FaultEH)
            {
                var faultBlock = FaultScope.FromClause(ehc);
                if (!_faultBlocks.Contains(faultBlock))
                {
                    _faultBlocks.Add(faultBlock);
                }
            }
        }
    }
    private void PushLiteral<T>(T value)
    {
        ILLiteral lit = new ILLiteral(TypeSolver.Resolve(typeof(T)), value?.ToString() ?? "");
        _stack.Push(lit);
    }
    private void IntroduceLabels()
    {
        ILInstr curInstr = _begin;
        while (curInstr != _begin.prev)
        {
            if (curInstr.arg is ILInstrOperand.Target target)
            {
                _labels.TryAdd(target.value.idx, null);
            }
            curInstr = curInstr.next;
        }
    }

    private ILStmtTargetLocation ResolveTargetLocation(ILInstr instr, List<ILStmtTargetLocation> labelsPool)
    {
        ILInstrOperand.Target? target = instr.arg as ILInstrOperand.Target;
        if (target == null) throw new Exception("expected target");
        int targetILIdx = target.value.idx;
        int? targetTACIdx;
        _labels.TryGetValue(targetILIdx, out targetTACIdx);
        ILStmtTargetLocation to = new ILStmtTargetLocation(targetTACIdx.GetValueOrDefault(-1), targetILIdx);
        if (targetTACIdx == null)
        {
            labelsPool.Add(to);
        }
        return to;
    }
    private ILExpr PopSingleAddr()
    {
        return ToSingleAddr(_stack.Pop());
    }
    private ILExpr ToSingleAddr(ILExpr val)
    {
        if (val is not ILValue)
        {
            ILLocal tmp = GetNewTemp(val.Type, val);
            _tac.Add(new ILAssignStmt(GetNewStmtLoc(), tmp, val));
            val = tmp;
        }
        return val;
    }

    private ILStmtLocation GetNewStmtLoc()
    {
        return new ILStmtLocation(_nextTacLineIdx++);
    }
    private ILLocal GetNewTemp(ILType type, ILExpr value)
    {
        _temps.Add(value);
        return new ILLocal(type, Logger.TempVarName(_temps.Count - 1));
    }
    private void ProcessIL()
    {
        ILInstr curInstr = _begin;

        List<ILStmtTargetLocation> labelsPool = new List<ILStmtTargetLocation>();
        while (curInstr != _begin.prev)
        {
            ILInstr.Instr instr;
            if (curInstr is ILInstr.Instr ilinstr)
            {
                instr = ilinstr;
            }
            else
            {
                Console.WriteLine(curInstr.ToString());
                curInstr = curInstr.next;
                continue;
            }

            if (_labels.ContainsKey(curInstr.idx)) _labels[curInstr.idx] = _nextTacLineIdx;

            foreach (var catchBlock in _catchBlocks.Where(b => b.Begin == curInstr.idx))
            {
                _stack.Push(new ILLiteral(TypeSolver.Resolve(catchBlock.Type), "err"));
                _tac.Add(new ILEHStmt("catch"));
            }
            foreach (var catchBlock in _filterBlocks.Where(b => b.Begin == curInstr.idx))
            {
                _stack.Push(new ILLiteral(TypeSolver.Resolve(typeof(System.Exception)), "err"));
            }
            switch (instr.opCode.Name)
            {
                case "ckfinite":
                case "mkrefany":
                case "refanytype":
                case "refanyval":
                case "jmp":
                case "initblk":
                case "cpblk": throw new Exception("not implemented");

                case "nop":
                case "break": break;

                case "ldarg.0": _stack.Push(_params[0]); break;
                case "ldarg.1": _stack.Push(_params[1]); break;
                case "ldarg.2": _stack.Push(_params[2]); break;
                case "ldarg.3": _stack.Push(_params[3]); break;
                case "ldarg": _stack.Push(_params[((ILInstrOperand.Arg16)instr.arg).value]); break;
                case "ldarg.s": _stack.Push(_params[((ILInstrOperand.Arg8)instr.arg).value]); break;
                case "ldloc.0": _stack.Push(_locals[0]); break;
                case "ldloc.1": _stack.Push(_locals[1]); break;
                case "ldloc.2": _stack.Push(_locals[2]); break;
                case "ldloc.3": _stack.Push(_locals[3]); break;
                case "ldloc": _stack.Push(_locals[((ILInstrOperand.Arg16)instr.arg).value]); break;
                case "ldloc.s": _stack.Push(_locals[((ILInstrOperand.Arg8)instr.arg).value]); break;
                case "stloc.0":
                    _tac.Add(
                    new ILAssignStmt(GetNewStmtLoc(), _locals[0], _stack.Pop())
                    ); break;
                case "stloc.1":
                    _tac.Add(
                    new ILAssignStmt(GetNewStmtLoc(), _locals[1], _stack.Pop())
                    ); break;
                case "stloc.2":
                    _tac.Add(
                    new ILAssignStmt(GetNewStmtLoc(), _locals[2], _stack.Pop())
                    ); break;
                case "stloc.3":
                    _tac.Add(
                    new ILAssignStmt(GetNewStmtLoc(), _locals[3], _stack.Pop())
                    ); break;
                case "stloc.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _tac.Add(
                        new ILAssignStmt(GetNewStmtLoc(), _locals[idx], _stack.Pop())
                        ); break;
                    }
                case "starg":
                case "starg.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _tac.Add(
                        new ILAssignStmt(GetNewStmtLoc(), _params[idx], _stack.Pop())
                        ); break;
                    }
                case "arglist":
                    {
                        _stack.Push(new ILVarArgValue(_methodInfo.Name));
                        break;
                    }
                case "endfault":
                    {
                        _tac.Add(new ILEHStmt("endfault"));
                        break;
                    }
                case "endfinally":
                    {
                        _tac.Add(new ILEHStmt("endfinally"));
                        break;
                    }
                case "endfilter":
                    {
                        _stack.Pop();
                        _tac.Add(new ILEHStmt("endfilter"));
                        break;
                    }
                case "localloc":
                    {
                        ILExpr size = PopSingleAddr();
                        _stack.Push(new ILStackAlloc(size));
                        break;
                    }
                // obj model
                // cpobj
                // initobj
                // ldsfld
                // stsfld
                // stfld
                // ldsflda
                // ldvirtftn
                // throw
                // rethrow
                // stobj
                case "sizeof":
                    {
                        Type? mbType = safeTypeResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mbType == null) throw new Exception("type not resolved for sizeof");
                        _stack.Push(new ILSizeOfExpr(TypeSolver.Resolve(mbType)));
                        break;
                    }
                case "ldind.i1":
                case "ldind.i2":
                case "ldind.i4":
                case "ldind.u1":
                case "ldind.u2":
                case "ldind.u4":
                    {
                        ILExpr addr = _stack.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILInt32());
                        _stack.Push(deref);
                        break;
                    }
                case "ldind.u8":
                case "ldind.i8":
                    {
                        ILExpr addr = _stack.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILInt64());
                        _stack.Push(deref);
                        break;
                    }
                case "ldind.r4":
                    {
                        ILExpr addr = _stack.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeFloat());
                        _stack.Push(deref);
                        break;
                    }
                case "ldind.r8":
                    {
                        ILExpr addr = _stack.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeFloat());
                        _stack.Push(deref);
                        break;
                    }
                case "ldind.i":
                    {
                        ILExpr addr = _stack.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeInt());
                        _stack.Push(deref);
                        break;
                    }

                case "ldind.ref":
                case "ldobj":
                    {
                        ILExpr addr = _stack.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILObject());
                        _stack.Push(deref);
                        break;
                    }

                case "stind.i1":
                case "stind.i2":
                case "stind.i4":
                case "stind.i8":
                    {
                        ILExpr val = _stack.Pop();
                        ILLValue addr = (ILLValue)PopSingleAddr();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, new ILInt32()), val));
                        break;
                    }
                case "stind.r4":
                    {
                        ILExpr val = _stack.Pop();
                        ILLValue addr = (ILLValue)_stack.Pop();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, new ILFloat32()), val));
                        break;
                    }
                case "stind.r8":
                    {
                        ILExpr val = _stack.Pop();
                        ILLValue addr = (ILLValue)_stack.Pop();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, new ILFloat64()), val));
                        break;
                    }
                case "stind.i":
                    {
                        ILExpr val = _stack.Pop();
                        ILLValue addr = (ILLValue)_stack.Pop();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, new ILNativeInt()), val));
                        break;
                    }
                case "stind.ref":
                    {
                        ILExpr val = _stack.Pop();
                        ILLValue addr = (ILLValue)_stack.Pop();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, new ILObject()), val));
                        break;
                    }

                case "ldarga":
                    {
                        int idx = ((ILInstrOperand.Arg16)instr.arg).value;
                        _stack.Push(new ILManagedRef(_params[idx]));
                        break;
                    }
                case "ldarga.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _stack.Push(new ILManagedRef(_params[idx]));
                        break;
                    }
                case "ldloca":
                    {
                        int idx = ((ILInstrOperand.Arg16)instr.arg).value;
                        _stack.Push(new ILManagedRef(_locals[idx]));
                        break;
                    }
                case "ldloca.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _stack.Push(new ILManagedRef(_locals[idx]));
                        break;
                    }
                case "leave":
                case "leave.s":
                    {
                        ILStmtTargetLocation to = ResolveTargetLocation(instr, labelsPool);
                        _tac.Add(new ILGotoStmt(GetNewStmtLoc(), to));
                        _stack.Clear();
                        break;
                    }
                case "switch":
                    {
                        int branchCnt = ((ILInstrOperand.Arg32)instr.arg).value;
                        ILExpr compVal = PopSingleAddr();
                        for (int branch = 0; branch < branchCnt; branch++)
                        {
                            curInstr = curInstr.next;
                            ILInstr.SwitchArg target = (ILInstr.SwitchArg)curInstr;
                            ILStmtTargetLocation to = ResolveTargetLocation(curInstr, labelsPool);
                            _tac.Add(new ILIfStmt(
                                GetNewStmtLoc(),
                                new ILBinaryOperation(compVal, new ILLiteral(new ILInt32(), branch.ToString())),
                                to
                            ));
                        }
                        break;
                    }

                case "ldftn":
                    {
                        MethodBase? mb = safeMethodResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mb == null) throw new Exception("method not resolved at " + instr.idx);
                        _stack.Push(ILMethod.FromMethodBase(mb));
                        break;
                    }
                case "ldnull": _stack.Push(new ILNullValue()); break;
                case "ldc.i4.m1":
                case "ldc.i4.M1": PushLiteral(-1); break;
                case "ldc.i4.0": PushLiteral(0); break;
                case "ldc.i4.1": PushLiteral(1); break;
                case "ldc.i4.2": PushLiteral(2); break;
                case "ldc.i4.3": PushLiteral(3); break;
                case "ldc.i4.4": PushLiteral(4); break;
                case "ldc.i4.5": PushLiteral(5); break;
                case "ldc.i4.6": PushLiteral(6); break;
                case "ldc.i4.7": PushLiteral(7); break;
                case "ldc.i4.8": PushLiteral(8); break;
                case "ldc.i4.s": PushLiteral<int>(((ILInstrOperand.Arg8)instr.arg).value); break;
                case "ldc.i4": PushLiteral(((ILInstrOperand.Arg32)instr.arg).value); break;
                case "ldc.i8": PushLiteral(((ILInstrOperand.Arg64)instr.arg).value); break;
                case "ldc.r4": PushLiteral<float>(((ILInstrOperand.Arg32)instr.arg).value); break;
                case "ldc.r8": PushLiteral<double>(((ILInstrOperand.Arg64)instr.arg).value); break;
                case "ldstr": PushLiteral(safeStringResolve(((ILInstrOperand.Arg32)instr.arg).value)); break;
                case "dup":
                    {
                        ILExpr dup = ToSingleAddr(_stack.Pop());
                        _stack.Push(dup);
                        _stack.Push(dup);
                        break;
                    }
                case "pop": _stack.Pop(); break;
                case "ldtoken":
                    {
                        MethodBase? mbMethod = safeMethodResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        Type? mbType = safeTypeResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        FieldInfo? mbField = safeFieldResolve(((ILInstrOperand.Arg32)instr.arg).value, _methodInfo.DeclaringType!.GetGenericArguments(), _methodInfo.GetGenericArguments());

                        ILObjectLiteral token;
                        if (mbMethod != null)
                        {
                            token = new ILObjectLiteral(new ILHandleRef(), mbMethod.Name);
                        }
                        else if (mbField != null)
                        {
                            token = new ILObjectLiteral(new ILHandleRef(), mbField.GetValue(null));
                        }
                        else if (mbType != null)
                        {
                            token = new ILObjectLiteral(new ILHandleRef(), mbType.Name);
                        }
                        else
                            throw new Exception("cannot resolve token at " + instr.idx);
                        _stack.Push(token);
                        break;
                    }
                case "call":
                    {
                        MethodBase? method = safeMethodResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (method == null) throw new Exception("call not resolved at " + instr.idx);

                        ILMethod ilMethod = ILMethod.FromMethodBase(method);
                        ilMethod.LoadArgs(_stack);
                        if (ilMethod.IsInitializeArray())
                        {
                            InlineInitArray(ilMethod.Args);
                        }
                        else
                        {
                            var call = new ILCallExpr(ilMethod);
                            if (ilMethod.Returns())
                                _stack.Push(call);
                            else
                                _tac.Add(new ILCallStmt(GetNewStmtLoc(), call));
                        }
                        break;
                    }
                case "calli":
                    {
                        byte[]? sig = safeSignatureResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (sig == null) throw new Exception("signature not resolved at " + instr.idx);
                        ILMethod ilMethod = (ILMethod)_stack.Pop();
                        ilMethod.LoadArgs(_stack);
                        var call = new ILCallExpr(ilMethod);
                        if (ilMethod.Returns())
                            _stack.Push(call);
                        else
                            _tac.Add(new ILCallStmt(GetNewStmtLoc(), call));
                        break;
                    }
                case "callvirt":
                    {
                        MethodBase? method = safeMethodResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (method == null) throw new Exception("call not resolved at " + instr.idx);

                        ILMethod ilMethod = ILMethod.FromMethodBase(method);
                        ilMethod.LoadArgs(_stack);

                        var call = new ILInstanceCallExpr(_stack.Pop(), ilMethod);
                        if (ilMethod.Returns())
                            _stack.Push(call);
                        else
                            _tac.Add(new ILCallStmt(GetNewStmtLoc(), call));

                        break;
                    }
                case "ret":
                    {
                        ILExpr? retVal = _methodInfo.ReturnParameter.ParameterType != typeof(void) ? PopSingleAddr() : null;
                        _tac.Add(
                            new ILReturnStmt(GetNewStmtLoc(), retVal)
                        );

                        break;
                    }

                case "add":
                case "add.ovf":
                case "add.ovf.un":

                case "sub.ovf":
                case "sub.ovf.un":
                case "sub":

                case "mul.ovf":
                case "mul.ovf.un":
                case "mul":

                case "div.un":
                case "div":

                case "rem.un":
                case "rem":

                case "and":

                case "or":

                case "xor":

                case "shl":

                case "shr":
                case "shr.un":

                case "ceq":

                case "cgt.un":
                case "cgt":

                case "clt.un":
                case "clt":
                    {
                        ILExpr rhs = PopSingleAddr();
                        ILExpr lhs = PopSingleAddr();
                        ILBinaryOperation op = new ILBinaryOperation(lhs, rhs);
                        _stack.Push(op);
                        break;
                    }
                case "neg":
                case "not":
                    {
                        ILExpr operand = PopSingleAddr();
                        ILUnaryOperation op = new ILUnaryOperation(operand);
                        _stack.Push(op);
                        break;
                    }
                case "br.s":
                case "br":
                    {
                        ILStmtTargetLocation to = ResolveTargetLocation(instr, labelsPool);
                        _tac.Add(new ILGotoStmt(GetNewStmtLoc(), to));
                        break;
                    }
                case "beq.s":
                case "beq":

                case "bne.un":
                case "bne.un.s":

                case "bge.un":
                case "bge.un.s":
                case "bge.s":
                case "bge":

                case "bgt.un":
                case "bgt.un.s":
                case "bgt.s":
                case "bgt":

                case "ble.un":
                case "ble.un.s":
                case "ble.s":
                case "ble":

                case "blt.un":
                case "blt.un.s":
                case "blt.s":
                case "blt":

                    {
                        ILStmtTargetLocation to = ResolveTargetLocation(instr, labelsPool);
                        ILExpr lhs = PopSingleAddr();
                        ILExpr rhs = PopSingleAddr();
                        _tac.Add(new ILIfStmt(
                            GetNewStmtLoc(),
                            new ILBinaryOperation(lhs, rhs),
                            to
                        ));
                        break;
                    }
                case "brinst":
                case "brinst.s":
                case "brtrue.s":
                case "brtrue":
                    {

                        ILStmtTargetLocation to = ResolveTargetLocation(instr, labelsPool);
                        PushLiteral<bool>(true);
                        ILExpr rhs = _stack.Pop();
                        ILExpr lhs = _stack.Pop();
                        _tac.Add(new ILIfStmt(
                            GetNewStmtLoc(),
                            new ILBinaryOperation(lhs, rhs),
                            to
                        ));
                        break;
                    }
                case "brnull":
                case "brnull.s":
                case "brzero":
                case "brzero.s":
                case "brfalse.s":
                case "brfalse":
                    {
                        ILStmtTargetLocation to = ResolveTargetLocation(instr, labelsPool);
                        PushLiteral<bool>(false);
                        ILExpr rhs = _stack.Pop();
                        ILExpr lhs = _stack.Pop();
                        _tac.Add(new ILIfStmt(
                            GetNewStmtLoc(),
                            new ILBinaryOperation(lhs, rhs),
                            to
                        ));
                        break;
                    }
                case "newobj":
                    {
                        MethodBase? mb = safeMethodResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mb == null || !mb!.IsConstructor)
                        {
                            Console.WriteLine("error resolving method at " + instr.idx);
                            break;
                        }
                        int arity = mb.GetParameters().Where(p => !p.IsRetval).Count();
                        Type objType = mb.DeclaringType!;
                        ILExpr[] inParams = new ILExpr[arity];
                        for (int i = 0; i < arity; i++)
                        {
                            inParams[i] = _stack.Pop();
                        }
                        _stack.Push(new ILNewExpr(
                            TypeSolver.Resolve(objType),
                            inParams));
                        break;
                    }
                case "newarr":
                    {
                        Type? arrType = safeTypeResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (arrType == null)
                        {
                            Console.WriteLine("error resolving method at " + instr.idx);
                            break;
                        }
                        ILExpr sizeExpr = _stack.Pop();
                        // TODO check Int32 or Literal
                        ILArray resolvedType = new ILArray(TypeSolver.Resolve(arrType));
                        ILExpr arrExpr = new ILNewArrayExpr(
                            resolvedType,
                            sizeExpr);
                        ILLocal arrTemp = GetNewTemp(resolvedType, arrExpr);
                        _tac.Add(new ILAssignStmt(
                            GetNewStmtLoc(),
                            arrTemp,
                            arrExpr
                        ));
                        _stack.Push(arrTemp);
                        break;
                    }
                // TODO rework
                case "ldelem.i1":
                case "ldelem.i2":
                case "ldelem.i4":
                case "ldelem.i8":
                case "ldelem.u1":
                case "ldelem.u2":
                case "ldelem.u4":
                case "ldelem.u8":
                case "ldelem.r4":
                case "ldelem.r8":
                case "ldelem.i":
                case "ldelem.ref":
                case "ldelem":
                    {
                        // TODO separate cases depending on types
                        ILExpr index = _stack.Pop();
                        ILExpr arr = _stack.Pop();
                        _stack.Push(new ILArrayAccess(
                            arr, index));
                        break;
                    }
                case "ldelema":
                    {
                        ILExpr idx = _stack.Pop();
                        ILExpr arr = _stack.Pop();
                        _stack.Push(new ILManagedRef(new ILArrayAccess(arr, idx)));
                        break;
                    }
                case "ldlen":
                    {
                        ILExpr arr = _stack.Pop();
                        _stack.Push(new ILArrayLength(arr));
                        break;
                    }
                // TODO rework
                case "stelem.i1":
                case "stelem.i2":
                case "stelem.i4":
                case "stelem.i8":
                case "stelem.r4":
                case "stelem.r8":
                case "stelem.i":
                case "stelem.ref":
                case "stelem":
                    {
                        // TODO separate cases depending on types
                        // TODO add mbAddTemp?
                        ILExpr value = _stack.Pop();
                        ILExpr index = _stack.Pop();
                        ILExpr arr = _stack.Pop();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), new ILArrayAccess(arr, index), value));
                        break;
                    }

                case "conv.i1":
                case "conv.i2":
                case "conv.i4":
                    {
                        ILExpr value = PopSingleAddr();
                        ILConvExpr conv = new ILConvExpr(new ILInt32(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.i8":
                    {
                        ILExpr value = PopSingleAddr();
                        ILConvExpr conv = new ILConvExpr(new ILInt64(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.r4":
                    {
                        ILExpr value = PopSingleAddr();
                        ILConvExpr conv = new ILConvExpr(new ILFloat32(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.r8":
                    {
                        ILExpr value = PopSingleAddr();
                        ILConvExpr conv = new ILConvExpr(new ILFloat64(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.u1":
                case "conv.u2":
                case "conv.u4":
                    {
                        ILExpr value = PopSingleAddr();
                        ILConvExpr conv = new ILConvExpr(new ILInt32(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.u8":
                    {
                        ILExpr value = PopSingleAddr();
                        ILConvExpr conv = new ILConvExpr(new ILInt64(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.i":
                    {
                        ILExpr value = PopSingleAddr();
                        ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.u":
                    {
                        ILExpr value = PopSingleAddr();
                        ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.r.un":
                    {
                        ILExpr value = PopSingleAddr();
                        ILConvExpr conv = new ILConvExpr(new ILNativeFloat(), value);
                        _stack.Push(conv);
                        break;
                    }

                case "conv.ovf.i1":
                case "conv.ovf.i2":
                case "conv.ovf.i4":
                case "conv.ovf.i8":
                case "conv.ovf.u1":
                case "conv.ovf.u2":
                case "conv.ovf.u4":
                case "conv.ovf.u8":
                case "conv.ovf.i":
                case "conv.ovf.u":

                case "conv.ovf.i1.un":
                case "conv.ovf.i2.un":
                case "conv.ovf.i4.un":
                case "conv.ovf.i8.un":
                case "conv.ovf.u1.un":
                case "conv.ovf.u2.un":
                case "conv.ovf.u4.un":
                case "conv.ovf.u8.un":
                case "conv.ovf.i.un":
                case "conv.ovf.u.un":
                    {
                        ILExpr value = PopSingleAddr();
                        ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "isinst":
                    {
                        Type? mbType = safeTypeResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mbType == null)
                        {
                            Console.WriteLine("error resolving method at " + instr.idx);
                            break;
                        }
                        ILExpr obj = PopSingleAddr();
                        ILExpr res = new ILCondCastExpr(TypeSolver.Resolve(mbType), obj);
                        _stack.Push(res);
                        break;
                    }
                case "castclass":
                    {
                        Type? mbType = safeTypeResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mbType == null)
                        {
                            Console.WriteLine("error resolving method at " + instr.idx);
                            break;
                        }
                        ILExpr value = PopSingleAddr();
                        ILExpr casted = new ILCastClassExpr(TypeSolver.Resolve(mbType), value);
                        _stack.Push(casted);
                        break;
                    }
                case "box":
                    {
                        Type? mbType = safeTypeResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mbType == null)
                        {
                            Console.WriteLine("error resolving type at " + instr.idx);
                            break;
                        }
                        ILValue value = (ILValue)PopSingleAddr();
                        ILExpr boxed = new ILBoxExpr(value);
                        _stack.Push(boxed);
                        break;
                    }
                case "unbox":
                case "unbox.any":
                    {
                        Type? mbType = safeTypeResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mbType == null)
                        {
                            Console.WriteLine("error resolving type at " + instr.idx);
                            break;
                        }
                        ILValue obj = (ILValue)PopSingleAddr();
                        ILExpr unboxed = new ILUnboxExpr(TypeSolver.Resolve(mbType), obj);
                        _stack.Push(unboxed);
                        break;
                    }
                default: Console.WriteLine("unhandled instr " + instr.ToString()); break;
            }

            curInstr = curInstr.next;
        }
        foreach (var l in labelsPool)
        {
            l.Index = _labels[l.ILIndex]!.Value;
        }
        // TODO trailing gotos
    }
    private void InlineInitArray(ILExpr[] args)
    {
        if (args.First() is ILLocal newArr && args.Last() is ILObjectLiteral ilObj)
        {
            try
            {
                ILNewArrayExpr expr = (ILNewArrayExpr)_temps[Logger.NameToIndex(newArr.ToString())];
                int arrSize = int.Parse(expr.Size.ToString());
                Type arrType = ((ILPrimitiveType)expr.Type).BaseType;
                var tmp = Array.CreateInstance(arrType, arrSize);
                GCHandle handle = GCHandle.Alloc(tmp, GCHandleType.Pinned);

                try
                {
                    Marshal.StructureToPtr(ilObj.Object!, handle.AddrOfPinnedObject(), false);
                }
                finally
                {
                    handle.Free();
                }
                List<object> list = [.. tmp];
                ILLiteral arrLit = new ILLiteral(new ILArray(new ILInt32()), "[" + string.Join(", ", list.Select(v => v.ToString())) + "]");
                for (int i = 0; i < list.Count; i++)
                {
                    _tac.Add(new ILAssignStmt(
                    GetNewStmtLoc(),
                    new ILArrayAccess(newArr, new ILLiteral(new ILInt32(), i.ToString())),
                    new ILLiteral(expr.Type, list[i].ToString()!)
                ));
                }
            }
            catch (Exception e) { Console.WriteLine(e); }
            return;
        }
        throw new Exception("bad static array init");
    }
    private FieldInfo? safeFieldResolve(int target, Type[]? gta, Type[]? gpa)
    {
        try
        {
            return _declaringModule.ResolveField(target, gta, gpa);
        }
        catch (Exception)
        {
            return null;
        }
    }
    private Type? safeTypeResolve(int target)
    {
        try
        {
            return _declaringModule.ResolveType(target);
        }
        catch (Exception)
        {
            return null;
        }
    }
    private MethodBase? safeMethodResolve(int target)
    {
        try
        {
            return _declaringModule.ResolveMethod(target);
        }
        catch (Exception)
        {
            return null;
        }

    }
    private byte[]? safeSignatureResolve(int target)
    {
        try
        {
            return _declaringModule.ResolveSignature(target);
        }
        catch (Exception)
        {
            return null;
        }

    }
    private string safeStringResolve(int target)
    {
        try
        {
            return _declaringModule.ResolveString(target);
        }
        catch (Exception e)
        {
            Console.WriteLine("error resolving string " + e.Message);
            return "";
        }
    }

    public List<string> LocalVars()
    {
        List<string> res = new List<string>();

        Dictionary<ILType, List<int>> locals = new Dictionary<ILType, List<int>>();
        for (int i = 0; i < _locals.Count; i++)
        {
            if (!locals.ContainsKey(_locals[i].Type))
            {
                locals.Add(_locals[i].Type, []);
            }
            locals[_locals[i].Type].Add(i);
        }
        foreach (var mapping in locals)
        {
            string buf = string.Format("{0} {1}", mapping.Key.ToString(), string.Join(", ", mapping.Value.Select(v => Logger.LocalVarName(v))));
            res.Add(buf);
        }

        Dictionary<ILType, List<int>> temps = new Dictionary<ILType, List<int>>();
        for (int i = 0; i < _temps.Count; i++)
        {
            if (!temps.ContainsKey(_temps[i].Type))
            {
                temps.Add(_temps[i].Type, []);
            }
            temps[_temps[i].Type].Add(i);
        }
        foreach (var mapping in temps)
        {
            string buf = string.Format("{0} {1}", mapping.Key.ToString(), string.Join(", ", mapping.Value.Select(v => Logger.TempVarName(v))));
            res.Add(buf);
        }
        return res;
    }
    public string ListMethodSignature()
    {
        return string.Format("{0} {1}({2})", _methodInfo.ReturnType, _methodInfo.Name, string.Join(", ", _methodInfo.GetParameters().Select(mi => mi.ToString())));
    }
    public void DumpMethodSignature()
    {
        Console.WriteLine(ListMethodSignature());
    }
    public void DumpLocalVars()
    {
        foreach (var v in LocalVars())
        {
            Console.WriteLine(v);
        }
    }
    public void DumpTAC()
    {
        foreach (var line in _tac)
        {
            Console.WriteLine(line.ToString());
        }
    }
    public void DumpEHs()
    {
        foreach (var ehc in _ehs)
        {
            Console.WriteLine(ehc);
        }
    }
    public void DumpAll()
    {
        DumpEHs();
        DumpMethodSignature();
        DumpLocalVars();
        DumpTAC();
        Console.WriteLine("Left on stack: {0}", _stack.Count);
    }
    public void DumpLabels()
    {
        foreach (var (i, j) in _labels)
        {
            Console.WriteLine("{0} -> {1}", i, j);
        }
    }
}