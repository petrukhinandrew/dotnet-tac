using System.Reflection;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.Parser;
enum SMValueSource
{
    Local = 0,
    Arg = 1,
    Temp = 2
}
interface SMValue
{
    public ILExpr AsILExpr { get; }
    public ILType Type { get; }
}
class SMLiteral<T>(T value, int index) : SMValue
{
    ILType _type = TypeSolver.Resolve(typeof(T));
    T Value = value;
    int Index = index;

    public ILExpr AsILExpr => new ILLocal(_type, Value == null ? "" : ToString());

    ILType SMValue.Type => _type;
}
class SMNullLit { }
class SMVar(ILType type, SMValueSource source, int index) : SMValue
{
    ILType SMValue.Type => type;
    SMValueSource Source = source;
    int Index = index;

    public ILLValue AsILLValue => new ILLocal(type, Name);
    public ILExpr AsILExpr => new ILLocal(type, Name);
    public string Name => Source switch
    {
        SMValueSource.Local => Logger.LocalVarName(Index),
        SMValueSource.Arg => Logger.ArgVarName(Index),
        SMValueSource.Temp => Logger.TempVarName(Index),
        _ => throw new NotImplementedException()
    };
}
class StackMachine
{
    private Stack<SMValue> _stack;
    private List<SMVar> _locals;
    private List<SMVar> _params;
    private List<SMVar> _temps = new List<SMVar>();
    private List<SMValue> _lits = new List<SMValue>();
    private int _nextLitIdx = 0;
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
        _params = _methodInfo.GetParameters().OrderBy(p => p.Position).Select(l => new SMVar(TypeSolver.Resolve(l.ParameterType), SMValueSource.Arg, l.Position)).ToList();
        _locals = locals.OrderBy(l => l.LocalIndex).Select(l => new SMVar(TypeSolver.Resolve(l.LocalType), SMValueSource.Local, l.LocalIndex)).ToList();
        _stack = new Stack<SMValue>(maxDepth);
        ProcessIL();
    }
    private void PushLocal(int idx)
    {
        _stack.Push(_locals[idx]);
    }
    private void PushArg(int idx)
    {
        _stack.Push(_params[idx]);
    }
    private void Dup()
    {
        _stack.Push(_stack.Peek());
    }
    private void Pop()
    {
        _stack.Pop();
    }
    private void PushLiteral<T>(T value)
    {
        SMLiteral<T> lit = new SMLiteral<T>(value, _nextLitIdx++);
        _lits.Add(lit);
        _stack.Push(lit);
    }
    private void ProcessIL()
    {
        ILInstr curInstr = _begin;
        while (curInstr != _begin.prev)
        {
            ILInstr.Instr instr;
            if (curInstr is ILInstr.Instr ilinstr)
            {
                instr = ilinstr;
            }
            else
            {
                curInstr = curInstr.next;
                continue;
            }
            switch (instr.opCode.Name)
            {
                case "nop":
                case "break": break;
                case "ldarg.0": PushArg(0); break;
                case "ldarg.1": PushArg(1); break;
                case "ldarg.2": PushArg(2); break;
                case "ldarg.3": PushArg(3); break;
                case "ldarg.s": PushArg(((ILInstrOperand.Arg8)instr.arg).value); break;
                case "ldloc.0": PushLocal(0); break;
                case "ldloc.1": PushLocal(1); break;
                case "ldloc.2": PushLocal(2); break;
                case "ldloc.3": PushLocal(3); break;
                case "ldloc.s": PushLocal(((ILInstrOperand.Arg8)instr.arg).value); break;
                case "stloc.0":
                    _tac.Add(
                    new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _locals[0].AsILLValue, _stack.Pop().AsILExpr)
                    ); break;
                case "stloc.1":
                    _tac.Add(
                    new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _locals[1].AsILLValue, _stack.Pop().AsILExpr)
                    ); break;
                case "stloc.2":
                    _tac.Add(
                    new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _locals[2].AsILLValue, _stack.Pop().AsILExpr)
                    ); break;
                case "stloc.3":
                    _tac.Add(
                    new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _locals[3].AsILLValue, _stack.Pop().AsILExpr)
                    ); break;
                case "stloc.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _tac.Add(
                        new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _locals[idx].AsILLValue, _stack.Pop().AsILExpr)
                        ); break;
                    }
                case "starg.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _tac.Add(
                        new ILAssignStmt(new ILStmtLocation(_nextTacLineIdx++), _params[idx].AsILLValue, _stack.Pop().AsILExpr)
                        ); break;
                    }
                // TODO byref
                // case "ldarga.s":
                //     _stack.Push(new SMValue.Arg(((ILInstrOperand.Arg8)instr.arg).value, AsAddr: true)); break;
                // case "ldloca.s":
                //     _stack.Push(new SMValue.Local(((ILInstrOperand.Arg8)instr.arg).value, AsAddr: true)); break;
                case "ldnull": PushLiteral<SMNullLit>(new SMNullLit()); break;
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
                case "dup": Dup(); break;
                case "pop": Pop(); break;
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
                        ILExpr? retVal = _methodInfo.ReturnParameter.ParameterType != typeof(void) ? _stack.Pop().AsILExpr : null;

                        _tac.Add(
                            new ILReturnStmt(new ILStmtLocation(_nextTacLineIdx++), retVal)
                        );

                        break;
                    }
                case "add":
                    {
                        SMValue add2 = _stack.Pop();
                        SMValue add1 = _stack.Pop();
                        _temps.Add(new SMVar(add1.Type, SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILAddOp(add1.AsILExpr, add2.AsILExpr)
                        ));
                        break;
                    }
                case "sub":
                    {
                        SMValue sub2 = _stack.Pop();
                        SMValue sub1 = _stack.Pop();
                        _temps.Add(new SMVar(sub1.Type, SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILSubOp(sub1.AsILExpr, sub2.AsILExpr)
                        ));
                        break;
                    }
                case "mul":
                    {
                        SMValue mul2 = _stack.Pop();
                        SMValue mul1 = _stack.Pop();
                        _temps.Add(new SMVar(mul1.Type, SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILMulOp(mul1.AsILExpr, mul2.AsILExpr)
                        ));
                        break;
                    }
                case "div.un":
                case "div":
                    {
                        SMValue div2 = _stack.Pop();
                        SMValue div1 = _stack.Pop();
                        _temps.Add(new SMVar(div1.Type, SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILSubOp(div1.AsILExpr, div2.AsILExpr)
                        ));
                        break;
                    }
                case "rem.un":
                case "rem":
                    {
                        SMValue rem2 = _stack.Pop();
                        SMValue rem1 = _stack.Pop();
                        _temps.Add(new SMVar(rem1.Type, SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILRemOp(rem1.AsILExpr, rem2.AsILExpr)
                        ));
                        break;
                    }
                case "and":
                    {
                        SMValue and2 = _stack.Pop();
                        SMValue and1 = _stack.Pop();
                        _temps.Add(new SMVar(and1.Type, SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILAndOp(and1.AsILExpr, and2.AsILExpr)
                        ));
                        break;
                    }
                case "or":
                    {
                        SMValue or2 = _stack.Pop();
                        SMValue or1 = _stack.Pop();
                        _temps.Add(new SMVar(or1.Type, SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILOrOp(or1.AsILExpr, or2.AsILExpr)
                        ));
                        break;
                    }
                case "xor":
                    {
                        SMValue xor2 = _stack.Pop();
                        SMValue xor1 = _stack.Pop();
                        _temps.Add(new SMVar(xor1.Type, SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILXorOp(xor1.AsILExpr, xor2.AsILExpr)
                        ));
                        break;
                    }
                case "shl":
                    {
                        SMValue shl2 = _stack.Pop();
                        SMValue shl1 = _stack.Pop();
                        _temps.Add(new SMVar(shl1.Type, SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILShlOp(shl1.AsILExpr, shl2.AsILExpr)
                        ));
                        break;
                    }
                case "shr.un":
                case "shr":
                    {
                        SMValue shr2 = _stack.Pop();
                        SMValue shr1 = _stack.Pop();
                        _temps.Add(new SMVar(shr1.Type, SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILShrOp(shr1.AsILExpr, shr2.AsILExpr)
                        ));
                        break;
                    }
                case "ceq":
                    {
                        SMValue ceq2 = _stack.Pop();
                        SMValue ceq1 = _stack.Pop();
                        _temps.Add(new SMVar(new ILInt32(), SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILCeqOp(ceq1.AsILExpr, ceq2.AsILExpr)
                        ));
                        break;
                    }
                case "cgt.un":
                case "cgt":
                    {
                        SMValue cgt2 = _stack.Pop();
                        SMValue cgt1 = _stack.Pop();
                        _temps.Add(new SMVar(new ILInt32(), SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILCgtOp(cgt1.AsILExpr, cgt2.AsILExpr)
                        ));
                        break;
                    }
                case "clt.un":
                case "clt":
                    {
                        SMValue clt2 = _stack.Pop();
                        SMValue clt1 = _stack.Pop();
                        _temps.Add(new SMVar(new ILInt32(), SMValueSource.Temp, _temps.Count));
                        _stack.Push(_temps.Last());
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILShrOp(clt1.AsILExpr, clt2.AsILExpr)
                        ));
                        break;
                    }
                case "neg":
                    {
                        SMValue notVal = _stack.Pop();
                        _temps.Add(new SMVar(notVal.Type, SMValueSource.Temp, _temps.Count));
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILUnaryMinus(notVal.AsILExpr)
                        ));
                        break;
                    }
                case "not":
                    {
                        SMValue notVal = _stack.Pop();
                        _temps.Add(new SMVar(notVal.Type, SMValueSource.Temp, _temps.Count));
                        _tac.Add(new ILAssignStmt(
                            new ILStmtLocation(_nextTacLineIdx++),
                            _temps.Last().AsILLValue,
                            new ILUnaryNot(notVal.AsILExpr)
                        ));
                        break;
                    }
                default: Console.WriteLine("unhandled instr " + instr.ToString()); break;
            }
            curInstr = curInstr.next;
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
        foreach (var mapping in _locals.ToDictionary(l => (l as SMValue).Type, l => l))
        {
            string buf = string.Format("{0} {1};", mapping.Key.ToString(), string.Join(", ", mapping.Value.Name));
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
            Console.WriteLine(line);
        }
    }
    public void DumpAll()
    {
        DumpMethodSignature();
        DumpLocalVars();
        DumpTAC();
    }
}