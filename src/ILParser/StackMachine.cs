using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.Parser;
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
        if (rhs is not ILValue)
        {
            ILLocal tmp = GetNewTemp(rhs.Type, rhs);
            _tac.Add(new ILAssignStmt(GetNewStmtLoc(), tmp, rhs));
            rhs = tmp;
        }
        if (lhs is not ILValue)
        {
            ILLocal tmp = GetNewTemp(lhs.Type, lhs);
            _tac.Add(new ILAssignStmt(GetNewStmtLoc(), tmp, lhs));
            lhs = tmp;
        }
        return (lhs, rhs);
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
                case "starg.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _tac.Add(
                        new ILAssignStmt(GetNewStmtLoc(), _params[idx], _stack.Pop())
                        ); break;
                    }
                // TODO byref
                // case "ldarga.s":
                //     _stack.Push(new SMValue.Arg(((ILInstrOperand.Arg8)instr.arg).value, AsAddr: true)); break;
                // case "ldloca.s":
                //     _stack.Push(new SMValue.Local(((ILInstrOperand.Arg8)instr.arg).value, AsAddr: true)); break;
                case "ldftn":
                    {
                        MethodBase? mb = safeMethodResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mb == null) throw new Exception("method not resolved at " + instr.idx);
                        _stack.Push(ILMethod.FromMethodBase(mb));
                        break;
                    }
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
                        break; // vsharp interpreter fs ldtoken
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
                            _stack.Push(new ILCallExpr(ilMethod));
                        }
                        break;
                    }
                case "calli":
                    {
                        byte[]? sig = safeSignatureResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (sig == null) throw new Exception("signature not resolved at " + instr.idx);
                        ILMethod ilMethod = (ILMethod)_stack.Pop();
                        ilMethod.LoadArgs(_stack);
                        _stack.Push(new ILCallExpr(ilMethod));
                        break;
                    }
                case "ret":
                    {
                        ILExpr? retVal = _methodInfo.ReturnParameter.ParameterType != typeof(void) ? _stack.Pop() : null;
                        _tac.Add(
                            new ILReturnStmt(GetNewStmtLoc(), retVal)
                        );

                        break;
                    }
                case "add":

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
                        ILExpr lhs = _stack.Pop();
                        ILExpr rhs = _stack.Pop();
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
                        Type? objType = mb.DeclaringType ?? typeof(void);
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
                        ILArrayRef resolvedType = new ILArrayRef(TypeSolver.Resolve(arrType));
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
                // case "ldelema":
                case "ldlen":
                    {
                        ILExpr arr = _stack.Pop();
                        _stack.Push(new ILArrayLength(arr));
                        break;
                    }
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
                        ILExpr value = _stack.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILInt32(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.i8":
                    {
                        ILExpr value = _stack.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILInt64(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.r4":
                    {
                        ILExpr value = _stack.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILFloat32(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.r8":
                    {
                        ILExpr value = _stack.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILFloat64(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.u1":
                case "conv.u2":
                case "conv.u4":
                    {
                        ILExpr value = _stack.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILInt32(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.u8":
                    {
                        ILExpr value = _stack.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILInt64(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.i":
                    {
                        ILExpr value = _stack.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.u":
                    {
                        ILExpr value = _stack.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                        _stack.Push(conv);
                        break;
                    }
                case "conv.r.un":
                    {
                        ILExpr value = _stack.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILNativeFloat(), value);
                        _stack.Push(conv);
                        break;
                    }
                // 
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
                // 
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
                        ILExpr value = _stack.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                        _stack.Push(conv);
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
        Console.WriteLine(args.Last().GetType().ToString());
        if (args.First() is ILObjectLiteral ilObj && args.Last() is ILLocal newArr)
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
                ILLiteral arrLit = new ILLiteral(new ILArrayRef(new ILInt32()), "[" + string.Join(", ", list.Select(v => v.ToString())) + "]");
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
    public List<string> ListLocalVars()
    {
        List<string> res = new List<string>();
        foreach (var mapping in _locals)
        {
            string buf = string.Format("{0} {1};", mapping.Type.ToString(), string.Join(", ", mapping.ToString()));
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