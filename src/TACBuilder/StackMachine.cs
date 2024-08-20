
using System.Reflection;
using System.Runtime.InteropServices;
using Usvm.IL.TypeSystem;
using Usvm.IL.Parser;

namespace Usvm.IL.TACBuilder;

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
    private List<EHScope> _scopes = [];
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
        InitEHScopes();
        IntroduceLabels();
        ProcessIL();
        UpdateEHScopes();
    }

    private void InitEHScopes()
    {
        foreach (var ehc in _ehs)
        {
            EHScope scope = EHScope.FromClause(ehc);
            if (!_scopes.Contains(scope))
                _scopes.Add(scope);
        }
    }
    private void UpdateEHScopes()
    {
        foreach (var scope in _scopes)
        {
            scope.tacLoc.tb = (int)_labels[scope.ilLoc.tb]!;
            scope.tacLoc.te = (int)_labels[scope.ilLoc.te]!;
            scope.tacLoc.hb = (int)_labels[scope.ilLoc.hb]!;
            scope.tacLoc.he = (int)_labels[scope.ilLoc.he]!;
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

        foreach (var scope in _scopes)
        {
            foreach (int idx in scope.ilLoc.Indices())
            {
                _labels.TryAdd(idx, null);
            }
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

            foreach (CatchScope scope in _scopes.Where(s => s is CatchScope cs && cs.ilLoc.hb == curInstr.idx))
            {
                ILType type = TypeSolver.Resolve(scope.Type);
                _stack.Push(new ILLiteral(type, "err"));
                _tac.Add(new ILCatchStmt(GetNewStmtLoc(), type));
            }
            foreach (FilterScope scope in _scopes.Where(b => b is FilterScope fs && fs.other == curInstr.idx))
            {
                _stack.Push(new ILLiteral(TypeSolver.Resolve(typeof(Exception)), "err"));
            }

            switch (instr.opCode.Name)
            {
                case "ckfinite":
                case "mkrefany":
                case "refanytype":
                case "refanyval":
                case "jmp":
                case "calli":
                case "stelem.i":
                case "ldelem.i":
                case "initblk":
                case "cpobj":
                case "cpblk": throw new Exception("not implemented " + instr.opCode.Name);

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
                    new ILAssignStmt(GetNewStmtLoc(), _locals[0], PopSingleAddr())
                    ); break;
                case "stloc.1":
                    _tac.Add(
                    new ILAssignStmt(GetNewStmtLoc(), _locals[1], PopSingleAddr())
                    ); break;
                case "stloc.2":
                    _tac.Add(
                    new ILAssignStmt(GetNewStmtLoc(), _locals[2], PopSingleAddr())
                    ); break;
                case "stloc.3":
                    _tac.Add(
                    new ILAssignStmt(GetNewStmtLoc(), _locals[3], PopSingleAddr())
                    ); break;
                case "stloc.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _tac.Add(
                        new ILAssignStmt(GetNewStmtLoc(), _locals[idx], PopSingleAddr())
                        ); break;
                    }
                case "starg":
                case "starg.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)instr.arg).value;
                        _tac.Add(
                        new ILAssignStmt(GetNewStmtLoc(), _params[idx], PopSingleAddr())
                        ); break;
                    }
                case "arglist":
                    {
                        _stack.Push(new ILVarArgValue(_methodInfo.Name));
                        break;
                    }
                case "throw":
                    {
                        ILExpr obj = PopSingleAddr();
                        _stack.Clear();
                        _tac.Add(new ILEHStmt(GetNewStmtLoc(), "throw", obj));
                        break;
                    }
                case "rethrow":
                    {
                        _tac.Add(new ILEHStmt(GetNewStmtLoc(), "rethrow"));
                        break;
                    }
                case "endfault":
                    {
                        _tac.Add(new ILEHStmt(GetNewStmtLoc(), "endfault"));
                        break;
                    }
                case "endfinally":
                    {
                        _tac.Add(new ILEHStmt(GetNewStmtLoc(), "endfinally"));
                        break;
                    }
                case "endfilter":
                    {
                        ILExpr value = _stack.Peek();
                        _tac.Add(new ILEHStmt(GetNewStmtLoc(), "endfilter", value));
                        break;
                    }
                case "localloc":
                    {
                        ILExpr size = PopSingleAddr();
                        _stack.Push(new ILStackAlloc(size));
                        break;
                    }
                case "ldfld":
                    {
                        FieldInfo? mbField = safeFieldResolve(((ILInstrOperand.Arg32)instr.arg).value, _methodInfo.DeclaringType!.GetGenericArguments(), _methodInfo.GetGenericArguments());
                        if (mbField == null) throw new Exception("field not resolved at " + instr.idx);
                        ILExpr inst = PopSingleAddr();
                        _stack.Push(ILField.Instance(mbField, inst));
                        break;
                    }
                case "ldflda":
                    {
                        FieldInfo? mbField = safeFieldResolve(((ILInstrOperand.Arg32)instr.arg).value, _methodInfo.DeclaringType!.GetGenericArguments(), _methodInfo.GetGenericArguments());
                        if (mbField == null) throw new Exception("field not resolved at " + instr.idx);
                        ILExpr inst = PopSingleAddr();
                        ILField field = ILField.Instance(mbField, inst);
                        if (inst.Type is ILUnmanagedPointer)
                        {
                            _stack.Push(new ILUnmanagedRef(field));
                        }
                        else
                        {
                            _stack.Push(new ILManagedRef(field));
                        }
                        break;
                    }

                case "ldsfld":
                    {
                        FieldInfo? mbField = safeFieldResolve(((ILInstrOperand.Arg32)instr.arg).value, _methodInfo.DeclaringType!.GetGenericArguments(), _methodInfo.GetGenericArguments());
                        if (mbField == null) throw new Exception("field not resolved at " + instr.idx);
                        _stack.Push(ILField.Static(mbField));
                        break;
                    }
                case "ldsflda":
                    {
                        FieldInfo? mbField = safeFieldResolve(((ILInstrOperand.Arg32)instr.arg).value, _methodInfo.DeclaringType!.GetGenericArguments(), _methodInfo.GetGenericArguments());
                        if (mbField == null) throw new Exception("field not resolved at " + instr.idx);
                        ILField field = ILField.Static(mbField);
                        if (mbField.FieldType.IsUnmanaged())
                        {
                            _stack.Push(new ILUnmanagedRef(field));
                        }
                        else
                        {
                            _stack.Push(new ILManagedRef(field));
                        }
                        break;
                    }

                case "stfld":
                    {
                        FieldInfo? mbField = safeFieldResolve(((ILInstrOperand.Arg32)instr.arg).value, _methodInfo.DeclaringType!.GetGenericArguments(), _methodInfo.GetGenericArguments());
                        if (mbField == null) throw new Exception("field not resolved at " + instr.idx);
                        ILExpr value = PopSingleAddr();
                        ILExpr obj = PopSingleAddr();
                        ILField field = ILField.Instance(mbField, obj);
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), field, value));
                        break;
                    }
                case "stsfld":
                    {
                        FieldInfo? mbField = safeFieldResolve(((ILInstrOperand.Arg32)instr.arg).value, _methodInfo.DeclaringType!.GetGenericArguments(), _methodInfo.GetGenericArguments());
                        if (mbField == null) throw new Exception("field not resolved at " + instr.idx);
                        ILExpr value = PopSingleAddr();
                        ILField field = ILField.Static(mbField);
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), field, value));
                        break;
                    }
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
                        ILExpr addr = PopSingleAddr();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILInt32());
                        _stack.Push(deref);
                        break;
                    }
                case "ldind.u8":
                case "ldind.i8":
                    {
                        ILExpr addr = PopSingleAddr();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILInt64());
                        _stack.Push(deref);
                        break;
                    }
                case "ldind.r4":
                    {
                        ILExpr addr = PopSingleAddr();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeFloat());
                        _stack.Push(deref);
                        break;
                    }
                case "ldind.r8":
                    {
                        ILExpr addr = PopSingleAddr();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeFloat());
                        _stack.Push(deref);
                        break;
                    }
                case "ldind.i":
                    {
                        ILExpr addr = PopSingleAddr();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeInt());
                        _stack.Push(deref);
                        break;
                    }

                case "ldind.ref":
                    {
                        ILExpr addr = PopSingleAddr();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILObject());
                        _stack.Push(deref);
                        break;
                    }
                case "ldobj":
                    {
                        Type? mbType = safeTypeResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mbType == null) throw new Exception("type not resolved for ldobj");
                        ILExpr addr = PopSingleAddr();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, TypeSolver.Resolve(mbType));
                        _stack.Push(deref);
                        break;
                    }

                case "stind.i1":
                case "stind.i2":
                case "stind.i4":
                case "stind.i8":
                    {
                        ILExpr val = PopSingleAddr();
                        ILLValue addr = (ILLValue)PopSingleAddr();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, new ILInt32()), val));
                        break;
                    }
                case "stind.r4":
                    {
                        ILExpr val = PopSingleAddr();
                        ILLValue addr = (ILLValue)PopSingleAddr();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, new ILFloat32()), val));
                        break;
                    }
                case "stind.r8":
                    {
                        ILExpr val = PopSingleAddr();
                        ILLValue addr = (ILLValue)PopSingleAddr();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, new ILFloat64()), val));
                        break;
                    }
                case "stind.i":
                    {
                        ILExpr val = PopSingleAddr();
                        ILLValue addr = (ILLValue)PopSingleAddr();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, new ILNativeInt()), val));
                        break;
                    }
                case "stind.ref":
                    {
                        ILExpr val = PopSingleAddr();
                        ILLValue addr = (ILLValue)PopSingleAddr();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, new ILObject()), val));
                        break;
                    }
                case "stobj":
                    {
                        Type? mbType = safeTypeResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mbType == null) throw new Exception("type not resolved for sizeof");
                        ILExpr val = PopSingleAddr();
                        ILLValue addr = (ILLValue)PopSingleAddr();
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, TypeSolver.Resolve(mbType)), val));
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
                case "ldvirtftn":
                    {
                        MethodBase? mb = safeMethodResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (mb == null) throw new Exception("method not resolved at " + instr.idx);
                        ILMethod ilMethod = ILMethod.FromMethodBase(mb);
                        ilMethod.Receiver = PopSingleAddr();
                        _stack.Push(ilMethod);
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
                        ILExpr dup = PopSingleAddr();
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
                case "callvirt":
                    {
                        MethodBase? method = safeMethodResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (method == null) throw new Exception("call not resolved at " + instr.idx);
                        ILMethod ilMethod = ILMethod.FromMethodBase(method);
                        ilMethod.LoadArgs(_stack);
                        var call = new ILCallExpr(ilMethod);
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
                        ILExpr rhs = PopSingleAddr();
                        ILExpr lhs = PopSingleAddr();
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
                        ILExpr rhs = PopSingleAddr();
                        ILExpr lhs = PopSingleAddr();
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
                        ILExpr sizeExpr = PopSingleAddr();
                        if (sizeExpr is not ILInt32 && sizeExpr is not ILNativeInt)
                        {
                            throw new Exception("expected arr size of type int32 or native int");
                        }
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
                case "initobj":
                    {
                        Type? type = safeTypeResolve(((ILInstrOperand.Arg32)instr.arg).value);
                        if (type == null) throw new Exception("type not resolved at " + instr.idx);
                        ILExpr addr = PopSingleAddr();
                        ILType ilType = TypeSolver.Resolve(type);
                        _tac.Add(new ILAssignStmt(GetNewStmtLoc(), PointerExprTypeResolver.DerefAs(addr, ilType), new ILNewDefaultExpr(ilType)));
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
                case "ldelem.ref":
                case "ldelem":
                    {
                        ILExpr index = PopSingleAddr();
                        ILExpr arr = PopSingleAddr();
                        _stack.Push(new ILArrayAccess(arr, index));
                        break;
                    }
                case "ldelema":
                    {
                        ILExpr idx = PopSingleAddr();
                        ILExpr arr = PopSingleAddr();
                        _stack.Push(new ILManagedRef(new ILArrayAccess(arr, idx)));
                        break;
                    }
                case "ldlen":
                    {
                        ILExpr arr = PopSingleAddr();
                        _stack.Push(new ILArrayLength(arr));
                        break;
                    }
                case "stelem.i1":
                case "stelem.i2":
                case "stelem.i4":
                case "stelem.i8":
                case "stelem.r4":
                case "stelem.r8":
                case "stelem.ref":
                case "stelem":
                    {
                        ILExpr value = PopSingleAddr();
                        ILExpr index = PopSingleAddr();
                        ILExpr arr = PopSingleAddr();
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
                default: throw new Exception("unhandled instr " + instr.ToString());
            }

            curInstr = curInstr.next;
        }
        foreach (var l in labelsPool)
        {
            l.Index = _labels[l.ILIndex]!.Value;
        }
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
    public string FormatMethodSignature()
    {
        ILType retType = TypeSolver.Resolve(_methodInfo.ReturnType);
        return string.Format("{0} {1}({2})", retType.ToString(), _methodInfo.Name, string.Join(", ", _methodInfo.GetParameters().Select(mi => TypeSolver.Resolve(mi.ParameterType).ToString())));
    }
    public void DumpMethodSignature()
    {
        Console.WriteLine(FormatMethodSignature());
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
    public void DumpEHS()
    {
        foreach (var scope in _scopes)
        {
            Console.WriteLine(scope.ToString());
        }
    }
    public void DumpAll()
    {
        DumpMethodSignature();
        DumpEHS();
        DumpLocalVars();
        DumpTAC();
        if (_stack.Count != 0) throw new Exception(_stack.Count.ToString() + " left on stack");
    }

    public void DumpLabels()
    {
        foreach (var (i, j) in _labels)
        {
            Console.WriteLine("{0} -> {1}", i, j);
        }
    }
}