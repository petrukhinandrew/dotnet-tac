using System.ComponentModel;
using System.Net.Http.Headers;
using System.Reflection;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.Parser;
class StackMachine
{
    private Stack<ILExpr> _stack;
    private List<ILLocal> _locals;
    private List<ILLocal> _params;
    private int _temps = 0;
    private Dictionary<int, int?> _labels = new Dictionary<int, int?>();
    private int _nextTacLineIdx = 0;
    private List<ILStmt> _tac = new List<ILStmt>();
    private ILInstr _begin;
    private Module _declaringModule;
    private MethodInfo _methodInfo;

    public StackMachine(Module declaringModule, MethodInfo methodInfo, IList<LocalVariableInfo> locals, int maxDepth, ILInstr begin)
    {
        _begin = begin;
        _declaringModule = declaringModule;
        _methodInfo = methodInfo;
        _params = _methodInfo.GetParameters().OrderBy(p => p.Position).Select(l => new ILLocal(TypeSolver.Resolve(l.ParameterType), Logger.ArgVarName(l.Position))).ToList();
        _locals = locals.OrderBy(l => l.LocalIndex).Select(l => new ILLocal(TypeSolver.Resolve(l.LocalType), Logger.LocalVarName(l.LocalIndex))).ToList();
        _stack = new Stack<ILExpr>(maxDepth);
        IntroduceLabels();
        ProcessIL();
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
        if (target == null) throw new Exception("expected non null value for br.s");
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
    private (ILExpr, ILExpr) MbIntroduceTemp(ILExpr lhs, ILExpr rhs)
    {
        // TODO recursive call?
        if (lhs is ILValue && rhs is ILValue)
        {
            return (lhs, rhs);
        }
        if (lhs is ILValue)
        {
            ILLocal tmp = new ILLocal(rhs.Type, Logger.TempVarName(_temps++));
            _tac.Add(new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), tmp, rhs));
            _stack.Push(tmp);
            return (lhs, tmp);
        }
        else
        {
            ILLocal tmp = new ILLocal(lhs.Type, Logger.TempVarName(_temps++));
            _tac.Add(new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), tmp, lhs));
            _stack.Push(tmp);
            return (tmp, rhs);
        }
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
            switch (instr.opCode.Name)
            {
                case "nop":
                case "break": break;
                case "ldarg.0": _stack.Push(_params[0]); break;
                case "ldarg.1": _stack.Push(_params[1]); break;
                case "ldarg.2": _stack.Push(_params[2]); break;
                case "ldarg.3": _stack.Push(_params[3]); break;
                case "ldarg.s": _stack.Push(_params[((ILInstrOperand.Arg8)instr.arg).value]); break;
                case "ldloc.0": _stack.Push(_locals[0]); break;
                case "ldloc.1": _stack.Push(_locals[1]); break;
                case "ldloc.2": _stack.Push(_locals[2]); break;
                case "ldloc.3": _stack.Push(_locals[3]); break;
                case "ldloc.s": _stack.Push(_locals[((ILInstrOperand.Arg8)instr.arg).value]); break;
                case "stloc.0":
                    _tac.Add(
                    new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _locals[0], _stack.Pop())
                    ); break;
                case "stloc.1":
                    _tac.Add(
                    new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _locals[1], _stack.Pop())
                    ); break;
                case "stloc.2":
                    _tac.Add(
                    new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _locals[2], _stack.Pop())
                    ); break;
                case "stloc.3":
                    _tac.Add(
                    new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _locals[3], _stack.Pop())
                    ); break;
                case "stloc.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _tac.Add(
                        new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _locals[idx], _stack.Pop())
                        ); break;
                    }
                case "starg.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _tac.Add(
                        new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _params[idx], _stack.Pop())
                        ); break;
                    }
                // TODO byref
                // case "ldarga.s":
                //     _stack.Push(new SMValue.Arg(((ILInstrOperand.Arg8)instr.arg).value, AsAddr: true)); break;
                // case "ldloca.s":
                //     _stack.Push(new SMValue.Local(((ILInstrOperand.Arg8)instr.arg).value, AsAddr: true)); break;
                case "ldnull": _stack.Push((ILExpr)new ILNullValue()); break;
                case "ldc.i4.m1":
                case "ldc.i4.M1": PushLiteral<int>(-1); break;
                case "ldc.i4.0": PushLiteral<int>(0); break;
                case "ldc.i4.1": PushLiteral<int>(1); break;
                case "ldc.i4.2": PushLiteral<int>(2); break;
                case "ldc.i4.3": PushLiteral<int>(3); break;
                case "ldc.i4.4": PushLiteral<int>(4); break;
                case "ldc.i4.5": PushLiteral<int>(5); break;
                case "ldc.i4.6": PushLiteral<int>(6); break;
                case "ldc.i4.7": PushLiteral<int>(7); break;
                case "ldc.i4.8": PushLiteral<int>(8); break;
                case "ldc.i4.s": PushLiteral<int>(((ILInstrOperand.Arg8)instr.arg).value); break;
                case "ldc.i4": PushLiteral<int>(((ILInstrOperand.Arg32)instr.arg).value); break;
                case "ldc.i8": PushLiteral<long>(((ILInstrOperand.Arg64)instr.arg).value); break;
                case "ldc.r4": PushLiteral<float>(((ILInstrOperand.Arg32)instr.arg).value); break;
                case "ldc.r8": PushLiteral<double>(((ILInstrOperand.Arg64)instr.arg).value); break;
                case "ldstr": PushLiteral<string>(safeStringResolve(((ILInstrOperand.Arg32)instr.arg).value)); break;
                case "dup": _stack.Push(_stack.Peek()); break;
                case "pop": _stack.Pop(); break;
                case "jmp": throw new Exception("jmp occured");
                // case "call":
                //     MethodBase? callResolvedMethod = safeMethodResolve(((ILInstrOperand.Arg32)instr.arg).value);
                //     if (callResolvedMethod == null) break;
                //     string callResolvedMethodRetVal = "";
                //     string callResolvedMethodArgs = " ";
                //     // TODO handle in out args 
                //     foreach (var p in callResolvedMethod.GetParameters().Where(p => p.IsRetval))
                //     {
                //         callResolvedMethodRetVal += p.ToString() + " ";
                //     }
                //     for (int i = 0; i < callResolvedMethod.GetParameters().Where(p => !p.IsRetval).Count(); i++)
                //     {
                //         callResolvedMethodArgs += _stack.Pop().Name + " ";
                //     }
                //     string callResolvedMethodRetValPref = "";
                //     if (callResolvedMethodRetVal != "")
                //     {
                //         _stack.Push(new SMValue.Temp(_nextTempIdx++));
                //         callResolvedMethodRetValPref = _stack.Peek().Name + " = ";
                //     }
                //     _tac.Add(callResolvedMethodRetValPref + callResolvedMethodRetVal + callResolvedMethod.DeclaringType + " " + callResolvedMethod.Name + callResolvedMethodArgs + ";");
                //     break;
                case "ret":
                    {
                        ILExpr? retVal = _methodInfo.ReturnParameter.ParameterType != typeof(void) ? _stack.Pop() : null;

                        _tac.Add(
                            new ILReturnStmt(new ILStmtLocation(_nextTacLineIdx++), retVal)
                        );

                        break;
                    }
                case "add":
                case "sub":
                case "mul":
                case "div.un":
                case "div":
                case "rem.un":
                case "rem":
                case "and":
                case "or":
                case "xor":
                case "shl":
                case "shr.un":
                case "shr":
                case "ceq":
                case "cgt.un":
                case "cgt":
                case "clt.un":
                case "clt":
                    {
                        ILExpr rhs = _stack.Pop();
                        ILExpr lhs = _stack.Pop();
                        (lhs, rhs) = MbIntroduceTemp(lhs, rhs);
                        ILBinaryOperation op = new ILBinaryOperation(lhs, rhs);
                        _stack.Push(op);
                        break;
                    }
                case "neg":
                case "not":
                    {
                        ILExpr operand = _stack.Pop();
                        ILUnaryOperation op = new ILUnaryOperation(operand);
                        _stack.Push(op);
                        break;
                    }
                case "br.s":
                case "br":
                    {
                        ILStmtTargetLocation to = ResolveTargetLocation(instr, labelsPool);
                        _tac.Add(new ILGotoStmt(new ILStmtLocation(_nextTacLineIdx++), to));
                        break;
                    }
                case "beq.s":
                case "beq":
                case "bgt.s":
                case "bgt":
                    {
                        ILStmtTargetLocation to = ResolveTargetLocation(instr, labelsPool);
                        ILExpr lhs = _stack.Pop();
                        ILExpr rhs = _stack.Pop();
                        _tac.Add(new ILIfStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
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
                            new ILStmtLocation(_nextTacLineIdx++),
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
                            new ILStmtLocation(_nextTacLineIdx++),
                            new ILBinaryOperation(lhs, rhs),
                            to
                        ));
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
    }
    private MethodBase? safeMethodResolve(int target)
    {
        try
        {
            return _declaringModule.ResolveMethod(target);
        }
        catch (Exception e)
        {
            Console.WriteLine("error resolving method " + e.Message);
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
    public List<string> ListLocalVars()
    {
        List<string> res = new List<string>();
        foreach (var mapping in _locals)
        {
            string buf = string.Format("{0} {1};", mapping.Type.ToString(), string.Join(", ", mapping.Name));
            res.Add(buf);
        }
        return res;
    }
    public string ListMethodSignature()
    {
        return string.Format("{0} {1}({2})", _methodInfo.ReturnType, _methodInfo.Name, string.Join(",", _methodInfo.GetParameters().Select(mi => mi.ToString())));
    }
    public void DumpMethodSignature()
    {
        Console.WriteLine(ListMethodSignature());
    }
    public void DumpLocalVars()
    {
        foreach (var v in ListLocalVars())
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
    public void DumpAll()
    {
        DumpMethodSignature();
        DumpLocalVars();
        DumpTAC();
    }
}