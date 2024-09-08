using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Usvm.IL.Parser;
using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

static class TACLineBuilder
{
    private static ILInstr.Instr? AdvanceIP(ILInstr.Instr ip)
    {
        ILInstr tmp = ip.next;
        while (tmp is not ILInstr.Instr)
        {
            if (tmp is ILInstr.Back) return null;
            tmp = tmp.next;
        }

        return (ILInstr.Instr)tmp;
    }

    public static void Branch(this SMFrame frame)
    {
        while (true)
        {
            // foreach (CatchScope scope in frame.Scopes.Where(s =>
            //              s is CatchScope cs && cs.ilLoc.hb.idx == frame.CurInstr.idx))
            // {
            //     // frame.Stack.Push(frame.Errs[scope.ErrIdx]);
            //     frame.NewLine(new ILEHStmt("catch", frame.Errs[scope.ErrIdx]));
            // }
            //
            // foreach (FilterScope scope in frame.Scopes.Where(b => b is FilterScope fs && fs.fb.idx == frame.CurInstr.idx))
            // {
            //     frame.Stack.Push(frame.Errs[scope.ErrIdx]);
            // }
            //
            // foreach (FilterScope scope in frame.Scopes.Where(b =>
            //              b is FilterScope fs && fs.ilLoc.hb.idx == frame.CurInstr.idx))
            // {
            //     frame.Stack.Push(new ILLiteral(TypeSolver.Resolve(typeof(Exception)), "err"));
            // }

            switch (frame.CurInstr.opCode.Name)
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
                case "cpblk": throw new Exception("not implemented " + frame.CurInstr.opCode.Name);

                case "nop":
                case "break": break;

                case "ldarg.0":
                    frame.Stack.Push(frame.Params[0]);
                    break;
                case "ldarg.1":
                    frame.Stack.Push(frame.Params[1]);
                    break;
                case "ldarg.2":
                    frame.Stack.Push(frame.Params[2]);
                    break;
                case "ldarg.3":
                    frame.Stack.Push(frame.Params[3]);
                    break;
                case "ldarg":
                    frame.Stack.Push(frame.Params[((ILInstrOperand.Arg16)frame.CurInstr.arg).value]);
                    break;
                case "ldarg.s":
                    frame.Stack.Push(frame.Params[((ILInstrOperand.Arg8)frame.CurInstr.arg).value]);
                    break;
                case "ldloc.0":
                    frame.Stack.Push(frame.Locals[0]);
                    break;
                case "ldloc.1":
                    frame.Stack.Push(frame.Locals[1]);
                    break;
                case "ldloc.2":
                    frame.Stack.Push(frame.Locals[2]);
                    break;
                case "ldloc.3":
                    frame.Stack.Push(frame.Locals[3]);
                    break;
                case "ldloc":
                    frame.Stack.Push(frame.Locals[((ILInstrOperand.Arg16)frame.CurInstr.arg).value]);
                    break;
                case "ldloc.s":
                    frame.Stack.Push(frame.Locals[((ILInstrOperand.Arg8)frame.CurInstr.arg).value]);
                    break;
                case "stloc.0":
                {
                    ILExpr value = frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(frame.Locals[0], value));
                    break;
                }
                case "stloc.1":
                {
                    ILExpr value = frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(frame.Locals[1], value));
                    break;
                }
                case "stloc.2":
                {
                    ILExpr value = frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(frame.Locals[2], value));
                    break;
                }
                case "stloc.3":
                {
                    ILExpr value = frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(frame.Locals[3], value));
                    break;
                }
                case "stloc.s":
                {
                    int idx = ((ILInstrOperand.Arg8)frame.CurInstr.arg).value;
                    ILExpr value = frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(frame.Locals[idx], value));
                    break;
                }
                case "starg":
                {
                    int idx = ((ILInstrOperand.Arg16)frame.CurInstr.arg).value;
                    ILExpr value = frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(frame.Params[idx], value));
                    break;
                }
                case "starg.s":
                {
                    int idx = ((ILInstrOperand.Arg8)frame.CurInstr.arg).value;
                    ILExpr value = frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(frame.Params[idx], value));
                    break;
                }
                case "arglist":
                {
                    frame.Stack.Push(new ILVarArgValue(frame.GetMethodName()));
                    break;
                }
                case "throw":
                {
                    ILExpr obj = frame.PopSingleAddr();
                    frame.Stack.Clear();
                    frame.NewLine(new ILEHStmt("throw", obj));
                    break;
                }
                case "rethrow":
                {
                    frame.NewLine(new ILEHStmt("rethrow"));
                    break;
                }
                case "endfault":
                {
                    frame.NewLine(new ILEHStmt("endfault"));
                    break;
                }
                case "endfinally":
                {
                    frame.NewLine(new ILEHStmt("endfinally"));
                    break;
                }
                case "endfilter":
                {
                    ILExpr value = frame.PopSingleAddr();
                    frame.NewLine(new ILEHStmt("endfilter", value));
                    break;
                }
                case "localloc":
                {
                    ILExpr size = frame.PopSingleAddr();
                    frame.Stack.Push(new ILStackAlloc(size));
                    break;
                }
                case "ldfld":
                {
                    FieldInfo field = frame.ResolveField(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    ILExpr inst = frame.PopSingleAddr();
                    frame.Stack.Push(ILField.Instance(field, inst));
                    break;
                }
                case "ldflda":
                {
                    FieldInfo field = frame.ResolveField(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    ILExpr inst = frame.PopSingleAddr();
                    ILField ilField = ILField.Instance(field, inst);
                    if (inst.Type is ILUnmanagedPointer)
                    {
                        frame.Stack.Push(new ILUnmanagedRef(ilField));
                    }
                    else
                    {
                        frame.Stack.Push(new ILManagedRef(ilField));
                    }

                    break;
                }

                case "ldsfld":
                {
                    FieldInfo field = frame.ResolveField(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    frame.Stack.Push(ILField.Static(field));
                    break;
                }
                case "ldsflda":
                {
                    FieldInfo field = frame.ResolveField(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    ILField ilField = ILField.Static(field);
                    if (field.FieldType.IsUnmanaged())
                    {
                        frame.Stack.Push(new ILUnmanagedRef(ilField));
                    }
                    else
                    {
                        frame.Stack.Push(new ILManagedRef(ilField));
                    }

                    break;
                }

                case "stfld":
                {
                    FieldInfo field = frame.ResolveField(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    ILExpr value = frame.PopSingleAddr();
                    ILExpr obj = frame.PopSingleAddr();
                    ILField ilField = ILField.Instance(field, obj);
                    frame.NewLine(new ILAssignStmt(ilField, value));
                    break;
                }
                case "stsfld":
                {
                    FieldInfo field = frame.ResolveField(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    ILExpr value = frame.PopSingleAddr();
                    ILField ilField = ILField.Static(field);
                    frame.NewLine(new ILAssignStmt(ilField, value));
                    break;
                }
                case "sizeof":
                {
                    Type mbType = frame.ResolveType(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    frame.Stack.Push(new ILSizeOfExpr(TypeSolver.Resolve(mbType)));
                    break;
                }
                case "ldind.i1":
                case "ldind.i2":
                case "ldind.i4":
                case "ldind.u1":
                case "ldind.u2":
                case "ldind.u4":
                {
                    ILExpr addr = frame.PopSingleAddr();
                    ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILInt32());
                    frame.Stack.Push(deref);
                    break;
                }
                case "ldind.u8":
                case "ldind.i8":
                {
                    ILExpr addr = frame.PopSingleAddr();
                    ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILInt64());
                    frame.Stack.Push(deref);
                    break;
                }
                case "ldind.r4":
                {
                    ILExpr addr = frame.PopSingleAddr();
                    ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeFloat());
                    frame.Stack.Push(deref);
                    break;
                }
                case "ldind.r8":
                {
                    ILExpr addr = frame.PopSingleAddr();
                    ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeFloat());
                    frame.Stack.Push(deref);
                    break;
                }
                case "ldind.i":
                {
                    ILExpr addr = frame.PopSingleAddr();
                    ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeInt());
                    frame.Stack.Push(deref);
                    break;
                }

                case "ldind.ref":
                {
                    ILExpr addr = frame.PopSingleAddr();
                    ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILObject());
                    frame.Stack.Push(deref);
                    break;
                }
                case "ldobj":
                {
                    Type? mbType = frame.ResolveType(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    if (mbType == null) throw new Exception("type not resolved for ldobj");
                    ILExpr addr = frame.PopSingleAddr();
                    ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, TypeSolver.Resolve(mbType));
                    frame.Stack.Push(deref);
                    break;
                }

                case "stind.i1":
                case "stind.i2":
                case "stind.i4":
                case "stind.i8":
                {
                    ILExpr val = frame.PopSingleAddr();
                    ILLValue addr = (ILLValue)frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILInt32()), val));
                    break;
                }
                case "stind.r4":
                {
                    ILExpr val = frame.PopSingleAddr();
                    ILLValue addr = (ILLValue)frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILFloat32()), val));
                    break;
                }
                case "stind.r8":
                {
                    ILExpr val = frame.PopSingleAddr();
                    ILLValue addr = (ILLValue)frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILFloat64()), val));
                    break;
                }
                case "stind.i":
                {
                    ILExpr val = frame.PopSingleAddr();
                    ILLValue addr = (ILLValue)frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILNativeInt()), val));
                    break;
                }
                case "stind.ref":
                {
                    ILExpr val = frame.PopSingleAddr();
                    ILLValue addr = (ILLValue)frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILObject()), val));
                    break;
                }
                case "stobj":
                {
                    Type? mbType = frame.ResolveType(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    if (mbType == null) throw new Exception("type not resolved for sizeof");
                    ILExpr val = frame.PopSingleAddr();
                    ILLValue addr = (ILLValue)frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, TypeSolver.Resolve(mbType)),
                        val));
                    break;
                }
                case "ldarga":
                {
                    int idx = ((ILInstrOperand.Arg16)frame.CurInstr.arg).value;
                    frame.Stack.Push(new ILManagedRef(frame.Params[idx]));
                    break;
                }
                case "ldarga.s":
                {
                    int idx = ((ILInstrOperand.Arg8)frame.CurInstr.arg).value;
                    frame.Stack.Push(new ILManagedRef(frame.Params[idx]));
                    break;
                }
                case "ldloca":
                {
                    int idx = ((ILInstrOperand.Arg16)frame.CurInstr.arg).value;
                    frame.Stack.Push(new ILManagedRef(frame.Locals[idx]));
                    break;
                }
                case "ldloca.s":
                {
                    int idx = ((ILInstrOperand.Arg8)frame.CurInstr.arg).value;
                    frame.Stack.Push(new ILManagedRef(frame.Locals[idx]));
                    break;
                }
                case "leave":
                case "leave.s":
                {
                    ILInstr target = ((ILInstrOperand.Target)frame.CurInstr.arg).value;
                    frame.Stack.Clear();
                    // var succScopes = frame.Scopes.Where(s =>
                    //     s.ilLoc.te.idx == frame.CurInstr.idx);
                    // var handlerScopes = succScopes.Select(s => s.ilLoc.hb).ToList();
                    // var filterScopes = succScopes.Where(s => s is FilterScope).Select(s => (s as FilterScope).fb).ToList();
                    // if (handlerScopes.Count > 0)
                    // {
                    //     frame.ContinueBranchingToMultiple(handlerScopes);
                    // }
                    //
                    // if (filterScopes.Count > 0)
                    // {
                    //     frame.ContinueBranchingToMultiple(filterScopes);
                    // }

                    frame.ContinueBranchingTo(target, null);
                    return;
                }
                case "switch":
                {
                    int branchCnt = ((ILInstrOperand.Arg32)frame.CurInstr.arg).value;
                    ILExpr compVal = frame.PopSingleAddr();
                    ILInstr switchBranch = frame.CurInstr;
                    List<ILInstr> targets = [];
                    for (int branch = 0; branch < branchCnt; branch++)
                    {
                        switchBranch = switchBranch.next;
                        ILInstrOperand.Target target = (ILInstrOperand.Target)((ILInstr.SwitchArg)switchBranch).arg;
                        targets.Add(target.value);
                        frame.NewLine(new ILIfStmt(
                            new ILBinaryOperation(compVal, new ILLiteral(new ILInt32(), branch.ToString())),
                            target.value.idx
                        ));
                    }

                    frame.ContinueBranchingToMultiple(targets);
                    break;
                }

                case "ldftn":
                {
                    MethodBase? mb = frame.ResolveMethod(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    if (mb == null) throw new Exception("method not resolved at " + frame.CurInstr.idx);
                    ILMethod method = ILMethod.FromMethodBase(mb);
                    frame.Stack.Push(method);
                    break;
                }
                case "ldvirtftn":
                {
                    MethodBase? mb = frame.ResolveMethod(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    if (mb == null) throw new Exception("method not resolved at " + frame.CurInstr.idx);
                    ILMethod ilMethod = ILMethod.FromMethodBase(mb);
                    ilMethod.Receiver = frame.PopSingleAddr();
                    frame.Stack.Push(ilMethod);
                    break;
                }
                case "ldnull":
                    frame.Stack.Push(new ILNullValue());
                    break;
                case "ldc.i4.m1":
                case "ldc.i4.M1":
                    frame.PushLiteral(-1);
                    break;
                case "ldc.i4.0":
                    frame.PushLiteral(0);
                    break;
                case "ldc.i4.1":
                    frame.PushLiteral(1);
                    break;
                case "ldc.i4.2":
                    frame.PushLiteral(2);
                    break;
                case "ldc.i4.3":
                    frame.PushLiteral(3);
                    break;
                case "ldc.i4.4":
                    frame.PushLiteral(4);
                    break;
                case "ldc.i4.5":
                    frame.PushLiteral(5);
                    break;
                case "ldc.i4.6":
                    frame.PushLiteral(6);
                    break;
                case "ldc.i4.7":
                    frame.PushLiteral(7);
                    break;
                case "ldc.i4.8":
                    frame.PushLiteral(8);
                    break;
                case "ldc.i4.s":
                    frame.PushLiteral<int>(((ILInstrOperand.Arg8)frame.CurInstr.arg).value);
                    break;
                case "ldc.i4":
                    frame.PushLiteral(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    break;
                case "ldc.i8":
                    frame.PushLiteral(((ILInstrOperand.Arg64)frame.CurInstr.arg).value);
                    break;
                case "ldc.r4":
                    frame.PushLiteral<float>(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    break;
                case "ldc.r8":
                    frame.PushLiteral<double>(((ILInstrOperand.Arg64)frame.CurInstr.arg).value);
                    break;
                case "ldstr":
                    frame.PushLiteral(frame.ResolveString(((ILInstrOperand.Arg32)frame.CurInstr.arg).value));
                    break;
                case "dup":
                {
                    ILExpr dup = frame.PopSingleAddr();
                    frame.Stack.Push(dup);
                    frame.Stack.Push(dup);
                    break;
                }
                case "pop":
                    frame.PopSingleAddr();
                    break;
                case "ldtoken":
                {
                    ILObjectLiteral? token;
                    try
                    {
                        MethodBase mbMethod = frame.ResolveMethod(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                        token = new ILObjectLiteral(new ILHandleRef(), mbMethod.Name);
                        frame.Stack.Push(token);
                        break;
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        FieldInfo mbField = frame.ResolveField(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                        token = new ILObjectLiteral(new ILHandleRef(), mbField.GetValue(null));
                        frame.Stack.Push(token);
                        break;
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        Type mbType = frame.ResolveType(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                        token = new ILObjectLiteral(new ILHandleRef(), mbType.Name);
                        frame.Stack.Push(token);
                        break;
                    }
                    catch (Exception)
                    {
                    }

                    throw new Exception("cannot resolve token at " + frame.CurInstr.idx);
                }
                case "call":
                {
                    MethodBase method = frame.ResolveMethod(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);

                    ILMethod ilMethod = ILMethod.FromMethodBase(method);
                    // TODO it use raw frame.Stack.Pop inside, must be using PopSingle-like
                    ilMethod.LoadArgs(frame.Stack);
                    if (ilMethod.IsInitializeArray())
                    {
                        frame.InlineInitArray(ilMethod.Args);
                    }
                    else
                    {
                        var call = new ILCallExpr(ilMethod);
                        if (ilMethod.Returns())
                            frame.Stack.Push(call);
                        else
                            frame.NewLine(new ILCallStmt(call));
                    }

                    break;
                }
                case "callvirt":
                {
                    MethodBase? method = frame.ResolveMethod(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    if (method == null) throw new Exception("call not resolved at " + frame.CurInstr.idx);
                    ILMethod ilMethod = ILMethod.FromMethodBase(method);
                    ilMethod.LoadArgs(frame.Stack);
                    var call = new ILCallExpr(ilMethod);
                    if (ilMethod.Returns())
                        frame.Stack.Push(call);
                    else
                        frame.NewLine(new ILCallStmt(call));
                    break;
                }
                case "ret":
                {
                    ILExpr? retVal = frame.GetMethodReturnType() != typeof(void) ? frame.PopSingleAddr() : null;
                    frame.NewLine(
                        new ILReturnStmt(retVal)
                    );
                    if (frame.Stack.Count > 0)
                        throw new Exception("stack not empty after ret at " + frame.CurInstr.idx);
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
                    ILExpr rhs = frame.PopSingleAddr();
                    ILExpr lhs = frame.PopSingleAddr();
                    ILBinaryOperation op = new ILBinaryOperation(lhs, rhs);
                    frame.Stack.Push(op);
                    break;
                }
                case "neg":
                case "not":
                {
                    ILExpr operand = frame.PopSingleAddr();
                    ILUnaryOperation op = new ILUnaryOperation(operand);
                    frame.Stack.Push(op);
                    break;
                }

                case "br.s":
                case "br":
                {
                    ILInstr target = ((ILInstrOperand.Target)frame.CurInstr.arg).value;
                    frame.ContinueBranchingTo(target, null);
                    return;
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
                    ILExpr lhs = frame.PopSingleAddr();
                    ILExpr rhs = frame.PopSingleAddr();
                    ILInstr tb = ((ILInstrOperand.Target)frame.CurInstr.arg).value;
                    ILInstr fb = frame.CurInstr.next;
                    frame.NewLine(new ILIfStmt(
                        new ILBinaryOperation(lhs, rhs),
                        tb.idx));
                    frame.ContinueBranchingTo(fb, tb);
                    return;
                }
                case "brinst":
                case "brinst.s":
                case "brtrue.s":
                case "brtrue":
                {
                    frame.PushLiteral<bool>(true);
                    ILExpr rhs = frame.PopSingleAddr();
                    ILExpr lhs = frame.PopSingleAddr();
                    ILInstr tb = ((ILInstrOperand.Target)frame.CurInstr.arg).value;
                    ILInstr fb = frame.CurInstr.next;
                    frame.NewLine(new ILIfStmt(
                        new ILBinaryOperation(lhs, rhs), tb.idx));
                    frame.ContinueBranchingTo(fb, tb);
                    return;
                }
                case "brnull":
                case "brnull.s":
                case "brzero":
                case "brzero.s":
                case "brfalse.s":
                case "brfalse":
                {
                    frame.PushLiteral<bool>(false);
                    ILExpr rhs = frame.PopSingleAddr();
                    ILExpr lhs = frame.PopSingleAddr();
                    ILInstr tb = ((ILInstrOperand.Target)frame.CurInstr.arg).value;
                    ILInstr fb = frame.CurInstr.next;
                    frame.NewLine(new ILIfStmt(
                        new ILBinaryOperation(lhs, rhs), tb.idx));
                    frame.ContinueBranchingTo(fb, tb);
                    return;
                }
                case "newobj":
                {
                    MethodBase mb = frame.ResolveMethod(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    if (!mb.IsConstructor) throw new Exception("expected constructor for newobj");
                    int arity = mb.GetParameters().Where(p => !p.IsRetval).Count();
                    Type objType = mb.DeclaringType!;
                    ILExpr[] inParams = new ILExpr[arity];
                    for (int i = 0; i < arity; i++)
                    {
                        inParams[i] = frame.PopSingleAddr();
                    }

                    frame.Stack.Push(new ILNewExpr(
                        TypeSolver.Resolve(objType),
                        inParams));
                    break;
                }
                case "newarr":
                {
                    Type arrType = frame.ResolveType(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    ILExpr sizeExpr = frame.PopSingleAddr();
                    if (sizeExpr.Type is not ILInt32 && sizeExpr.Type is not ILNativeInt)
                    {
                        throw new Exception("expected arr size of type int32 or native int, got " +
                                            sizeExpr.Type.ToString());
                    }

                    ILArray resolvedType = new ILArray(TypeSolver.Resolve(arrType));
                    ILExpr arrExpr = new ILNewArrayExpr(
                        resolvedType,
                        sizeExpr);
                    ILLocal arrTemp = frame.GetNewTemp(resolvedType, arrExpr);
                    frame.NewLine(new ILAssignStmt(
                        arrTemp,
                        arrExpr
                    ));
                    frame.Stack.Push(arrTemp);
                    break;
                }
                case "initobj":
                {
                    Type type = frame.ResolveType(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    ILExpr addr = frame.PopSingleAddr();
                    ILType ilType = TypeSolver.Resolve(type);
                    frame.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, ilType),
                        new ILNewDefaultExpr(ilType)));
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
                    ILExpr index = frame.PopSingleAddr();
                    ILExpr arr = frame.PopSingleAddr();
                    frame.Stack.Push(new ILArrayAccess(arr, index));
                    break;
                }
                case "ldelema":
                {
                    ILExpr idx = frame.PopSingleAddr();
                    ILExpr arr = frame.PopSingleAddr();
                    frame.Stack.Push(new ILManagedRef(new ILArrayAccess(arr, idx)));
                    break;
                }
                case "ldlen":
                {
                    ILExpr arr = frame.PopSingleAddr();
                    frame.Stack.Push(new ILArrayLength(arr));
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
                    ILExpr value = frame.PopSingleAddr();
                    ILExpr index = frame.PopSingleAddr();
                    ILExpr arr = frame.PopSingleAddr();
                    frame.NewLine(new ILAssignStmt(new ILArrayAccess(arr, index), value));
                    break;
                }
                case "conv.i1":
                case "conv.i2":
                case "conv.i4":
                {
                    ILExpr value = frame.PopSingleAddr();
                    ILConvExpr conv = new ILConvExpr(new ILInt32(), value);
                    frame.Stack.Push(conv);
                    break;
                }
                case "conv.i8":
                {
                    ILExpr value = frame.PopSingleAddr();
                    ILConvExpr conv = new ILConvExpr(new ILInt64(), value);
                    frame.Stack.Push(conv);
                    break;
                }
                case "conv.r4":
                {
                    ILExpr value = frame.PopSingleAddr();
                    ILConvExpr conv = new ILConvExpr(new ILFloat32(), value);
                    frame.Stack.Push(conv);
                    break;
                }
                case "conv.r8":
                {
                    ILExpr value = frame.PopSingleAddr();
                    ILConvExpr conv = new ILConvExpr(new ILFloat64(), value);
                    frame.Stack.Push(conv);
                    break;
                }
                case "conv.u1":
                case "conv.u2":
                case "conv.u4":
                {
                    ILExpr value = frame.PopSingleAddr();
                    ILConvExpr conv = new ILConvExpr(new ILInt32(), value);
                    frame.Stack.Push(conv);
                    break;
                }
                case "conv.u8":
                {
                    ILExpr value = frame.PopSingleAddr();
                    ILConvExpr conv = new ILConvExpr(new ILInt64(), value);
                    frame.Stack.Push(conv);
                    break;
                }
                case "conv.i":
                {
                    ILExpr value = frame.PopSingleAddr();
                    ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                    frame.Stack.Push(conv);
                    break;
                }
                case "conv.u":
                {
                    ILExpr value = frame.PopSingleAddr();
                    ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                    frame.Stack.Push(conv);
                    break;
                }
                case "conv.r.un":
                {
                    ILExpr value = frame.PopSingleAddr();
                    ILConvExpr conv = new ILConvExpr(new ILNativeFloat(), value);
                    frame.Stack.Push(conv);
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
                    ILExpr value = frame.PopSingleAddr();
                    ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                    frame.Stack.Push(conv);
                    break;
                }
                case "isinst":
                {
                    Type? mbType = frame.ResolveType(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    if (mbType == null)
                    {
                        Console.WriteLine("error resolving method at " + frame.CurInstr.idx);
                        break;
                    }

                    ILExpr obj = frame.PopSingleAddr();
                    ILExpr res = new ILCondCastExpr(TypeSolver.Resolve(mbType), obj);
                    frame.Stack.Push(res);
                    break;
                }
                case "castclass":
                {
                    Type? mbType = frame.ResolveType(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    if (mbType == null)
                    {
                        Console.WriteLine("error resolving method at " + frame.CurInstr.idx);
                        break;
                    }

                    ILExpr value = frame.PopSingleAddr();
                    ILExpr casted = new ILCastClassExpr(TypeSolver.Resolve(mbType), value);
                    frame.Stack.Push(casted);
                    break;
                }
                case "box":
                {
                    Type? mbType = frame.ResolveType(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    if (mbType == null)
                    {
                        Console.WriteLine("error resolving type at " + frame.CurInstr.idx);
                        break;
                    }

                    ILValue value = (ILValue)frame.PopSingleAddr();
                    ILExpr boxed = new ILBoxExpr(value);
                    frame.Stack.Push(boxed);
                    break;
                }
                case "unbox":
                case "unbox.any":
                {
                    Type? mbType = frame.ResolveType(((ILInstrOperand.Arg32)frame.CurInstr.arg).value);
                    if (mbType == null)
                    {
                        Console.WriteLine("error resolving type at " + frame.CurInstr.idx);
                        break;
                    }

                    ILValue obj = (ILValue)frame.PopSingleAddr();
                    ILExpr unboxed = new ILUnboxExpr(TypeSolver.Resolve(mbType), obj);
                    frame.Stack.Push(unboxed);
                    break;
                }
                default: throw new Exception("unhandled frame.CurInstr " + frame.CurInstr.ToString());
            }

            var adv = AdvanceIP(frame.CurInstr!);
            if (adv == null)
            {
                frame.StopBranching();
                return;
            }

            if (frame.IsLeader(adv))
            {
                frame.ContinueBranchingTo(adv, null);
                return;
            }

            frame.CurInstr = adv;
        }
    }

    private static void InlineInitArray(this SMFrame frame, List<ILExpr> args)
    {
        if (args.Last() is ILLocal newArr && args.First() is ILObjectLiteral ilObj)
        {
            try
            {
                ILNewArrayExpr expr = (ILNewArrayExpr)frame.Temps[Logger.NameToIndex(newArr.ToString())];
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
                ILLiteral arrLit = new ILLiteral(new ILArray(new ILInt32()),
                    "[" + string.Join(", ", list.Select(v => v.ToString())) + "]");
                for (int i = 0; i < list.Count; i++)
                {
                    frame.NewLine(new ILAssignStmt(
                        new ILArrayAccess(newArr, new ILLiteral(new ILInt32(), i.ToString())),
                        new ILLiteral(expr.Type, list[i].ToString()!)
                    ));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return;
        }

        throw new Exception("bad static array init");
    }
}