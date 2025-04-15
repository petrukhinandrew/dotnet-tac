using System.Diagnostics;
using System.Runtime.InteropServices;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder.BodyBuilder;

static class BlockTacLineBuilder
{
    private static void StLocIdx(this BlockTacBuilder blockBuilder, int idx)
    {
        var value = blockBuilder.Pop();
        var typedValue = blockBuilder.EnsureTyped(value, blockBuilder.Locals[idx].Type);
        blockBuilder.Locals[idx].Value = typedValue;
        blockBuilder.NewLine(new IlAssignStmt(blockBuilder.Locals[idx], typedValue));
    }

    private static void LdIndTyped(this BlockTacBuilder blockBuilder, Type rawValueType, Type rawStackType)
    {
        var addr = blockBuilder.Pop();
        var valueType = IlInstanceBuilder.GetType(rawValueType);
        var stackType = IlInstanceBuilder.GetType(rawStackType);
        var deref = PointerExprTypeResolver.Deref(addr, valueType);
        var typed = blockBuilder.EnsureTyped(deref, stackType);
        blockBuilder.Push(typed);
    }

    private static void StIndTyped(this BlockTacBuilder blockBuilder, Type rawInstType)
    {
        var instType = IlInstanceBuilder.GetType(rawInstType);
        var val = blockBuilder.Pop();
        val = blockBuilder.EnsureTyped(val, instType);
        var addr = blockBuilder.Pop();
        var typedAddr = blockBuilder.MakeSimpleValue(new IlConvCastExpr(instType.MakePointerType(), addr));
        blockBuilder.NewLine(
            new IlAssignStmt(
                PointerExprTypeResolver.Deref(typedAddr, instType),
                val)
        );
    }

    private static IlSimpleValue MakeSimpleValue(this BlockTacBuilder blockBuilder, IlExpr expr)
    {
        if (expr is IlSimpleValue value) return value;
        var newTmp = blockBuilder.GetNewTemp(expr);
        blockBuilder.NewLine(new IlAssignStmt(newTmp, expr));
        return newTmp;
    }

    private static IlValue EnsureTyped(this BlockTacBuilder blockBuilder, IlExpr expr, IlType? expectedType)
    {
        if (expr is IlNullConst && expectedType != null)
        {
            return new IlNullConst(expectedType);
        }

        if (expr.Type.Equals(expectedType))
        {
            return blockBuilder.MakeSimpleValue(expr);
        }

        if (expectedType != null)
            return blockBuilder.MakeSimpleValue(new IlConvCastExpr(expectedType, blockBuilder.MakeSimpleValue(expr)));
        
        return blockBuilder.MakeSimpleValue(expr);
    }

    private static IlValue EnsureBool(this BlockTacBuilder blockBuilder, IlExpr expr)
    {
        return blockBuilder.EnsureTyped(expr, IlInstanceBuilder.GetType(typeof(bool)));
    }

    /*
     * returns true if successors may be rebuilt
     */
    public static bool Rebuild(this BlockTacBuilder blockBuilder)
    {
        var sameStack = blockBuilder.StackInitIsTheSame() && blockBuilder.BuiltAtLeastOnce;
        if (sameStack) return false;
        blockBuilder._builtAtLeastOnce = true;

        blockBuilder.Reset();
        while (true)
        {
            // TODO check if it is proper
            if (blockBuilder.CurInstr is ILInstr.Back) return true;
            if (blockBuilder.CurInstr is ILInstr.SwitchArg switchBranch)
            {
                var target = (ILInstrOperand.Target)switchBranch.arg;
                Debug.Assert(blockBuilder.SwitchRegister is not null);
                // targets.Add(target.value);
                IlExpr comparison = new IlCeqOp(blockBuilder.SwitchRegister,
                    new IlInt32Const(switchBranch.Value));
                var typed = blockBuilder.EnsureBool(comparison);
                blockBuilder.NewLine(new IlIfStmt(
                    typed,
                    target.value.idx
                ));
                blockBuilder.Successors.ForEach(s => s.SwitchRegister = blockBuilder.SwitchRegister);
                if (blockBuilder.CurInstrIsLast()) return true;
            }

            Debug.Assert(blockBuilder.CurInstr is ILInstr.Instr,
                blockBuilder.CurInstr + " on " + blockBuilder.Meta.MethodMeta.Name);
            switch (((ILInstr.Instr)blockBuilder.CurInstr).opCode.Name)
            {
                case "ckfinite":
                case "mkrefany":
                case "refanytype":
                case "refanyval":
                case "jmp":
                case "initblk":
                case "cpobj":
                case "cpblk":
                    throw new KnownBug("not implemented " +
                                       ((ILInstr.Instr)blockBuilder.CurInstr).opCode.Name);
                case "arglist":
                {
                    blockBuilder.Push(new IlArgListRef(blockBuilder.Meta.MethodMeta));
                    break;
                }
                case "tail.":
                case "unaligned.":
                case "no.":
                case "constrained.":
                case "volatile.":
                case "readonly.":
                case "nop":
                case "break": break;

                case "ldarg.0":
                    blockBuilder.Push(blockBuilder.Params[0]);
                    break;
                case "ldarg.1":
                    blockBuilder.Push(blockBuilder.Params[1]);
                    break;
                case "ldarg.2":
                    blockBuilder.Push(blockBuilder.Params[2]);
                    break;
                case "ldarg.3":
                    blockBuilder.Push(blockBuilder.Params[3]);
                    break;
                case "ldarg":
                    blockBuilder.Push(blockBuilder.Params[((ILInstrOperand.Arg16)blockBuilder.CurInstr.arg).value]);
                    break;
                case "ldarg.s":
                    blockBuilder.Push(blockBuilder.Params[((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value]);
                    break;
                case "ldloc.0":
                    blockBuilder.Push(blockBuilder.Locals[0]);
                    break;
                case "ldloc.1":
                    blockBuilder.Push(blockBuilder.Locals[1]);
                    break;
                case "ldloc.2":
                    blockBuilder.Push(blockBuilder.Locals[2]);
                    break;
                case "ldloc.3":
                    blockBuilder.Push(blockBuilder.Locals[3]);
                    break;
                case "ldloc":
                    blockBuilder.Push(blockBuilder.Locals[((ILInstrOperand.Arg16)blockBuilder.CurInstr.arg).value]);
                    break;
                case "ldloc.s":
                    blockBuilder.Push(blockBuilder.Locals[((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value]);
                    break;
                case "stloc.0":
                {
                    blockBuilder.StLocIdx(0);
                    break;
                }
                case "stloc.1":
                {
                    blockBuilder.StLocIdx(1);
                    break;
                }
                case "stloc.2":
                {
                    blockBuilder.StLocIdx(2);
                    break;
                }
                case "stloc.3":
                {
                    blockBuilder.StLocIdx(3);
                    break;
                }
                case "stloc.s":
                {
                    int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                    blockBuilder.StLocIdx(idx);
                    break;
                }
                case "starg":
                {
                    int idx = ((ILInstrOperand.Arg16)blockBuilder.CurInstr.arg).value;
                    var value = blockBuilder.Pop();
                    var typedValue = blockBuilder.EnsureTyped(value, blockBuilder.Params[idx].Type);
                    blockBuilder.NewLine(new IlAssignStmt((IlLocal)blockBuilder.Params[idx], typedValue));
                    break;
                }
                case "starg.s":
                {
                    int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                    var value = blockBuilder.Pop();
                    var typedValue = blockBuilder.EnsureTyped(value, blockBuilder.Params[idx].Type);
                    blockBuilder.NewLine(new IlAssignStmt((IlLocal)blockBuilder.Params[idx], typedValue));
                    break;
                }
                case "throw":
                {
                    var obj = blockBuilder.Pop();
                    blockBuilder.ClearStack();
                    blockBuilder.NewLine(new IlThrowStmt(obj));
                    return false;
                }
                case "rethrow":
                {
                    blockBuilder.NewLine(new IlRethrowStmt());
                    break;
                }
                case "endfault":
                {
                    blockBuilder.NewLine(new IlEndFaultStmt());
                    blockBuilder.ClearStack();
                    return false;
                }
                case "endfinally":
                {
                    blockBuilder.NewLine(new IlEndFinallyStmt());
                    blockBuilder.ClearStack();
                    return false;
                }
                case "endfilter":
                {
                    var value = blockBuilder.Pop();
                    blockBuilder.NewLine(new IlEndFilterStmt(value));
                    return true;
                }
                case "localloc":
                {
                    var size = blockBuilder.Pop();
                    blockBuilder.Push(new IlStackAlloc(size));
                    break;
                }
                case "ldfld":
                {
                    var ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                    var inst = blockBuilder.Pop();
                    blockBuilder.Push(new IlFieldAccess(ilField, inst));
                    break;
                }
                case "ldflda":
                {
                    var ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                    var inst = blockBuilder.Pop();
                    var ilFieldAccess = new IlFieldAccess(ilField, inst);
                    blockBuilder.Push(inst.Type.IsManaged switch
                    {
                        true => new IlManagedRef(ilFieldAccess),
                        _ => new IlUnmanagedRef(ilFieldAccess)
                    });
                    break;
                }

                case "ldsfld":
                {
                    var ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                    blockBuilder.Push(new IlFieldAccess(ilField));
                    break;
                }
                case "ldsflda":
                {
                    var ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                    var ilFieldAccess = new IlFieldAccess(ilField);
                    if (ilField.Type!.IsUnmanaged)
                    {
                        blockBuilder.Push(new IlUnmanagedRef(ilFieldAccess));
                    }
                    else
                    {
                        blockBuilder.Push(new IlManagedRef(ilFieldAccess));
                    }

                    break;
                }

                case "stfld":
                {
                    var ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                    var value = blockBuilder.Pop();
                    var obj = blockBuilder.Pop();
                    var ilFieldAccess = new IlFieldAccess(ilField, obj);
                    var typedValue = blockBuilder.EnsureTyped(value, ilFieldAccess.Type);
                    blockBuilder.NewLine(new IlAssignStmt(ilFieldAccess, typedValue));
                    break;
                }
                case "stsfld":
                {
                    var ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                    var ilFieldAccess = new IlFieldAccess(ilField);
                    var value = blockBuilder.Pop();
                    var typedValue = blockBuilder.EnsureTyped(value, ilFieldAccess.Type);
                    blockBuilder.NewLine(new IlAssignStmt(ilFieldAccess, typedValue));
                    break;
                }
                case "sizeof":
                {
                    var ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    blockBuilder.Push(new IlSizeOfExpr(ilType));
                    break;
                }
                case "ldind.i1":
                {
                    blockBuilder.LdIndTyped(typeof(sbyte), typeof(int));
                    break;
                }
                case "ldind.i2":
                {
                    blockBuilder.LdIndTyped(typeof(short), typeof(int));
                    break;
                }
                case "ldind.i4":
                {
                    blockBuilder.LdIndTyped(typeof(int), typeof(int));
                    break;
                }
                case "ldind.u1":
                {
                    blockBuilder.LdIndTyped(typeof(byte), typeof(int));
                    break;
                }
                case "ldind.u2":
                {
                    blockBuilder.LdIndTyped(typeof(ushort), typeof(int));
                    break;
                }
                case "ldind.u4":
                {
                    blockBuilder.LdIndTyped(typeof(uint), typeof(int));
                    break;
                }
                case "ldind.u8":
                {
                    blockBuilder.LdIndTyped(typeof(ulong), typeof(long));
                    break;
                }
                case "ldind.i8":
                {
                    blockBuilder.LdIndTyped(typeof(long), typeof(long));
                    break;
                }
                case "ldind.r4":
                {
                    blockBuilder.LdIndTyped(typeof(float), typeof(float));
                    break;
                }
                case "ldind.r8":
                {
                    blockBuilder.LdIndTyped(typeof(double), typeof(double));
                    break;
                }
                case "ldind.i":
                {
                    blockBuilder.LdIndTyped(typeof(nint), typeof(nint));
                    break;
                }
                case "ldind.ref":
                {
                    var addr = blockBuilder.Pop();
                    var deref = PointerExprTypeResolver.Deref(addr, null);
                    blockBuilder.Push(deref);
                    break;
                }
                case "ldobj":
                {
                    var ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    var addr = blockBuilder.Pop();
                    var deref = PointerExprTypeResolver.Deref(addr, ilType);
                    blockBuilder.Push(deref);
                    break;
                }

                case "stind.i1":
                {
                    blockBuilder.StIndTyped(typeof(sbyte));
                    break;
                }

                case "stind.i2":
                {
                    blockBuilder.StIndTyped(typeof(short));
                    break;
                }
                case "stind.i4":
                    blockBuilder.StIndTyped(typeof(int));
                    break;
                case "stind.i8":
                {
                    blockBuilder.StIndTyped(typeof(long));
                    break;
                }
                case "stind.r4":
                {
                    blockBuilder.StIndTyped(typeof(float));
                    // var val = blockBuilder.Pop();
                    // var addr = (IlValue)blockBuilder.Pop();
                    // blockBuilder.NewLine(new IlAssignStmt(
                    //     PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(float))),
                    //     val));
                    break;
                }
                case "stind.r8":
                {
                    blockBuilder.StIndTyped(typeof(double));
                    // var val = blockBuilder.Pop();
                    // var addr = blockBuilder.Pop();
                    // blockBuilder.NewLine(new IlAssignStmt(
                    //     PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(double))),
                    //     val));
                    break;
                }
                case "stind.i":
                {
                    blockBuilder.StIndTyped(typeof(nint));
                    // var val = blockBuilder.Pop();
                    // var addr = blockBuilder.Pop();
                    // blockBuilder.NewLine(new IlAssignStmt(
                    //     PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(nint))),
                    //     val));
                    break;
                }
                case "stind.ref":
                {
                    var val = blockBuilder.Pop();
                    var addr = (IlValue)blockBuilder.Pop();
                    blockBuilder.NewLine(new IlAssignStmt(PointerExprTypeResolver.Deref(addr, val.Type), val));
                    break;
                }
                case "stobj":
                {
                    var ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    var val = blockBuilder.Pop();
                    var addr = (IlValue)blockBuilder.Pop();
                    blockBuilder.NewLine(
                        new IlAssignStmt(
                            PointerExprTypeResolver.Deref(addr, ilType),
                            blockBuilder.EnsureTyped(val, ilType)
                        )
                    );
                    break;
                }
                case "ldarga":
                {
                    int idx = ((ILInstrOperand.Arg16)blockBuilder.CurInstr.arg).value;
                    blockBuilder.Push(new IlManagedRef(blockBuilder.Params[idx]));
                    break;
                }
                case "ldarga.s":
                {
                    int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                    blockBuilder.Push(new IlManagedRef(blockBuilder.Params[idx]));
                    break;
                }
                case "ldloca":
                {
                    int idx = ((ILInstrOperand.Arg16)blockBuilder.CurInstr.arg).value;
                    blockBuilder.Push(new IlManagedRef(blockBuilder.Locals[idx]));
                    break;
                }
                case "ldloca.s":
                {
                    int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                    blockBuilder.Push(new IlManagedRef(blockBuilder.Locals[idx]));
                    break;
                }
                case "leave":
                case "leave.s":
                {
                    var target = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    blockBuilder.NewLine(new IlLeaveStmt(target.idx));
                    blockBuilder.ClearStack();
                    return true;
                }
                case "switch":
                {
                    var branchCnt = ((ILInstrOperand.Arg32)blockBuilder.CurInstr.arg).value;
                    var compVal = blockBuilder.Pop();
                    blockBuilder.Successors.ForEach(s => s.SwitchRegister = compVal);
                    // ILInstr switchBranch = blockBuilder.CurInstr;
                    // List<ILInstr> targets = [];
                    // for (int branch = 0; branch < branchCnt; branch++)
                    // {
                    //     switchBranch = switchBranch.next;
                    //     ILInstrOperand.Target target = (ILInstrOperand.Target)((ILInstr.SwitchArg)switchBranch).arg;
                    //     targets.Add(target.value);
                    //     blockBuilder.NewLine(new ILIfStmt(
                    //         new ILBinaryOperation(compVal, new ILLiteral(new ILInt32(), branch.ToString())),
                    //         target.value.idx
                    //     ));
                    // }

                    break;
                }
                case "ldftn":
                {
                    var ilMethod =
                        ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                    blockBuilder.Push(new IlMethodRef(ilMethod));
                    break;
                }
                case "ldvirtftn":
                {
                    var ilMethod =
                        ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                    blockBuilder.Push(new IlMethodRef(ilMethod, blockBuilder.Pop()));
                    break;
                }
                case "ldnull":
                    blockBuilder.Push(new IlNullConst(IlInstanceBuilder.GetType(typeof(object))));
                    break;
                case "ldc.i4.m1":
                case "ldc.i4.M1":
                    blockBuilder.Push(new IlInt32Const(-1));
                    break;
                case "ldc.i4.0":
                    blockBuilder.Push(new IlInt32Const(0));
                    break;
                case "ldc.i4.1":
                    blockBuilder.Push(new IlInt32Const(1));
                    break;
                case "ldc.i4.2":
                    blockBuilder.Push(new IlInt32Const(2));
                    break;
                case "ldc.i4.3":
                    blockBuilder.Push(new IlInt32Const(3));
                    break;
                case "ldc.i4.4":
                    blockBuilder.Push(new IlInt32Const(4));
                    break;
                case "ldc.i4.5":
                    blockBuilder.Push(new IlInt32Const(5));
                    break;
                case "ldc.i4.6":
                    blockBuilder.Push(new IlInt32Const(6));
                    break;
                case "ldc.i4.7":
                    blockBuilder.Push(new IlInt32Const(7));
                    break;
                case "ldc.i4.8":
                    blockBuilder.Push(new IlInt32Const(8));
                    break;
                case "ldc.i4.s":
                    blockBuilder.Push(new IlInt32Const(((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value));
                    break;
                case "ldc.i4":
                    blockBuilder.Push(new IlInt32Const(((ILInstrOperand.Arg32)blockBuilder.CurInstr.arg).value));
                    break;
                case "ldc.i8":
                    blockBuilder.Push(new IlInt64Const(((ILInstrOperand.Arg64)blockBuilder.CurInstr.arg).value));
                    break;
                case "ldc.r4":
                    blockBuilder.Push(new IlFloatConst(((ILInstrOperand.Arg32)blockBuilder.CurInstr.arg).value));
                    break;
                case "ldc.r8":
                    blockBuilder.Push(new IlDoubleConst(((ILInstrOperand.Arg64)blockBuilder.CurInstr.arg).value));
                    break;
                case "ldstr":
                    blockBuilder.Push(
                        new IlStringConst(((ILInstrOperand.ResolvedString)blockBuilder.CurInstr.arg).value.Value));
                    break;
                case "dup":
                {
                    var dup = blockBuilder.Pop();
                    blockBuilder.Push(dup);
                    blockBuilder.Push(dup);
                    break;
                }
                case "pop":
                    blockBuilder.Pop();
                    break;
                case "ldtoken":
                {
                    var ilMember = ((ILInstrOperand.ResolvedMember)blockBuilder.CurInstr.arg).value;
                    if (ilMember is IlType type)
                    {
                        blockBuilder.Push(new IlTypeRef(type));
                        break;
                    }

                    if (ilMember is IlField field)
                    {
                        blockBuilder.Push(new IlFieldRef(field));
                        break;
                    }

                    if (ilMember is IlMethod method)
                    {
                        blockBuilder.Push(new IlMethodRef(method));
                        break;
                    }

                    throw new Exception("ldtoken type not resolved");
                }
                case "call":
                {
                    var ilMethod =
                        ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;

                    if (ilMethod.DeclaringType?.FullName == "System.Runtime.CompilerServices.RuntimeHelpers" &&
                        ilMethod.Name == "InitializeArray")
                    {
                        var arr = blockBuilder.Pop();
                        var fld = blockBuilder.Pop();

                        break;
                    }


                    var rawArgs = ilMethod.Parameters.Select(_ => blockBuilder.Pop()).ToList();
                    rawArgs.Reverse();
                    var args = rawArgs.Zip(ilMethod.Parameters)
                        .Select(IlExpr (ap) => blockBuilder.EnsureTyped(ap.First, ap.Second.Type)).ToList();

                    var ilCall = new IlCall(ilMethod, args);

                    if (ilCall.Returns())
                    {
                        var tmp = blockBuilder.GetNewTemp(ilCall, blockBuilder.CurInstr.idx);
                        blockBuilder.NewLine(new IlAssignStmt(tmp, ilCall));
                        blockBuilder.Push(tmp);
                    }
                    else
                        blockBuilder.NewLine(new IlCallStmt(ilCall));

                    break;
                }
                case "callvirt":
                {
                    var ilMethod =
                        ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;

                    var rawArgs = ilMethod.Parameters.Select(_ => blockBuilder.Pop()).ToList();
                    rawArgs.Reverse();
                    var args = rawArgs.Zip(ilMethod.Parameters)
                        .Select(IlExpr (ap) => blockBuilder.EnsureTyped(ap.First, ap.Second.Type)).ToList();

                    var ilCall = new IlCall(ilMethod, args);

                    if (ilCall.Returns())
                    {
                        var tmp = blockBuilder.GetNewTemp(ilCall, blockBuilder.CurInstr.idx);
                        blockBuilder.NewLine(new IlAssignStmt(tmp, ilCall));
                        blockBuilder.Push(tmp);
                    }
                    else
                        blockBuilder.NewLine(new IlCallStmt(ilCall));

                    break;
                }
                case "calli":
                {
                    var sig =
                        ((ILInstrOperand.ResolvedSignature)blockBuilder.CurInstr.arg).value;
                    var ftn = blockBuilder.Pop();
                    var rawArgs = sig.ParameterTypes.Select(_ => blockBuilder.Pop()).ToList();
                    rawArgs.Reverse();
                    var args = rawArgs.Zip(sig.ParameterTypes)
                        .Select(IlExpr (ap) => blockBuilder.EnsureTyped(ap.First, ap.Second)).ToList();

                    var calli = new IlCallIndirect(sig, ftn, args);

                    if (sig.ReturnType != null && !Equals(sig.ReturnType, IlInstanceBuilder.GetType(typeof(void))))
                    {
                        var tmp = blockBuilder.GetNewTemp(calli, blockBuilder.CurInstr.idx);
                        blockBuilder.NewLine(new IlAssignStmt(tmp, calli));
                        blockBuilder.Push(tmp);
                    }
                    else
                        blockBuilder.NewLine(new IlCalliStmt(calli));

                    break;
                }
                case "ret":
                {
                    var methodMeta = blockBuilder.Meta.MethodMeta!;
                    var voidType = IlInstanceBuilder.GetType(typeof(void));
                    IlExpr? retVal = null;
                    if (methodMeta.ReturnType != null && !Equals(methodMeta.ReturnType, voidType))
                    {
                        retVal = blockBuilder.Pop();
                        retVal = blockBuilder.EnsureTyped(retVal, methodMeta.ReturnType);
                    }

                    blockBuilder.NewLine(
                        new IlReturnStmt(retVal)
                    );
                    break;
                }
                case "add":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlAddOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }
                case "add.ovf":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlAddOp(lhs, rhs, isChecked: true);
                    blockBuilder.Push(op);
                    break;
                }
                case "add.ovf.un":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlAddOp(lhs, rhs, isChecked: true, isUnsigned: true);
                    blockBuilder.Push(op);
                    break;
                }

                case "sub":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlSubOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }
                case "sub.ovf":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlSubOp(lhs, rhs, isChecked: true);
                    blockBuilder.Push(op);
                    break;
                }
                case "sub.ovf.un":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlSubOp(lhs, rhs, isChecked: true, isUnsigned: true);
                    blockBuilder.Push(op);
                    break;
                }

                case "mul":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlMulOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }
                case "mul.ovf":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlMulOp(lhs, rhs, isChecked: true);
                    blockBuilder.Push(op);
                    break;
                }
                case "mul.ovf.un":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlMulOp(lhs, rhs, isChecked: true, isUnsigned: true);
                    blockBuilder.Push(op);
                    break;
                }

                case "div":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlDivOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }
                case "div.un":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlMulOp(lhs, rhs, isUnsigned: true);
                    blockBuilder.Push(op);
                    break;
                }

                case "rem":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlRemOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }
                case "rem.un":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlRemOp(lhs, rhs, isUnsigned: true);
                    blockBuilder.Push(op);
                    break;
                }

                case "and":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlAndOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }

                case "or":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlOrOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }

                case "xor":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlXorOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }

                case "shl":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlShlOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }

                case "shr":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlShrOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }
                case "shr.un":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlShrOp(lhs, rhs, isUnsigned: true);
                    blockBuilder.Push(op);
                    break;
                }

                case "ceq":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlCeqOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }

                case "cgt":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlCgtOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }
                case "cgt.un":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlCgtOp(lhs, rhs, isUnsigned: true);
                    blockBuilder.Push(op);
                    break;
                }

                case "clt":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlCltOp(lhs, rhs);
                    blockBuilder.Push(op);
                    break;
                }
                case "clt.un":
                {
                    var rhs = blockBuilder.Pop();
                    var lhs = blockBuilder.Pop();
                    var op = new IlCltOp(lhs, rhs, isUnsigned: true);
                    blockBuilder.Push(op);
                    break;
                }
                case "neg":
                {
                    var operand = blockBuilder.Pop();
                    IlNegOp op = new(operand);
                    blockBuilder.Push(op);
                    break;
                }

                case "not":
                {
                    var operand = blockBuilder.Pop();
                    IlNotOp op = new(operand);
                    blockBuilder.Push(op);
                    break;
                }

                case "br.s":
                case "br":
                {
                    var target = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    blockBuilder.NewLine(new IlGotoStmt(target.idx));
                    // frame.ContinueBranchingTo(target, null);
                    return true;
                }
                case "beq":
                case "beq.s":
                {
                    var lhs = blockBuilder.Pop();
                    var rhs = blockBuilder.Pop();
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCeqOp(lhs, rhs);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "bne.un":
                case "bne.un.s":
                {
                    var lhs = blockBuilder.Pop();
                    var rhs = blockBuilder.Pop();
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCneOp(lhs, rhs);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "bge":
                case "bge.s":
                {
                    var lhs = blockBuilder.Pop();
                    var rhs = blockBuilder.Pop();
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCgeOp(lhs, rhs);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "bge.un":
                case "bge.un.s":
                {
                    var lhs = blockBuilder.Pop();
                    var rhs = blockBuilder.Pop();
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCgeOp(lhs, rhs, isUnsigned: true);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "bgt":
                case "bgt.s":
                {
                    var lhs = blockBuilder.Pop();
                    var rhs = blockBuilder.Pop();
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCgtOp(lhs, rhs);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "bgt.un":
                case "bgt.un.s":
                {
                    var lhs = blockBuilder.Pop();
                    var rhs = blockBuilder.Pop();
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCgtOp(lhs, rhs, isUnsigned: true);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "ble":
                case "ble.s":
                {
                    var lhs = blockBuilder.Pop();
                    var rhs = blockBuilder.Pop();
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCleOp(lhs, rhs);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "ble.un":
                case "ble.un.s":
                {
                    var lhs = blockBuilder.Pop();
                    var rhs = blockBuilder.Pop();
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCleOp(lhs, rhs, isUnsigned: true);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "blt":
                case "blt.s":
                {
                    var lhs = blockBuilder.Pop();
                    var rhs = blockBuilder.Pop();
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCltOp(lhs, rhs);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "blt.un":
                case "blt.un.s":
                {
                    var lhs = blockBuilder.Pop();
                    var rhs = blockBuilder.Pop();
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCltOp(lhs, rhs, isUnsigned: true);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "brinst":
                case "brinst.s":
                case "brtrue.s":
                case "brtrue":
                {
                    var lhs = blockBuilder.Pop();
                    IlExpr rhs = IlConstant.BrFalseWith(lhs);
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCneOp(lhs, rhs);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "brnull":
                case "brnull.s":
                case "brzero":
                case "brzero.s":
                case "brfalse.s":
                case "brfalse":
                {
                    var lhs = blockBuilder.Pop();
                    IlExpr rhs = IlConstant.BrFalseWith(lhs);
                    var tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                    var comparison = new IlCeqOp(lhs, rhs);
                    var typed = blockBuilder.EnsureBool(comparison);
                    blockBuilder.NewLine(new IlIfStmt(typed, tb.idx));
                    return true;
                }
                case "newobj":
                {
                    var ilMethod = ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    Debug.Assert(ilMethod.ReturnType == IlInstanceBuilder.GetType(typeof(void)));
                    var objIlType = ilMethod.DeclaringType!;
                    var allocExpr = new IlNewExpr(objIlType);
                    var newInstance = blockBuilder.GetNewTemp(allocExpr, blockBuilder.CurInstr.idx);
                    blockBuilder.NewLine(new IlAssignStmt(newInstance, allocExpr));
                    blockBuilder.Push(newInstance);
                    List<IlExpr> args = new();
                    foreach (var parameter in ilMethod.Parameters)
                    {
                        var arg = blockBuilder.EnsureTyped(blockBuilder.Pop(), parameter.Type);
                        args.Add(arg);
                    }

                    var ctorCall = new IlCall(ilMethod, args);
                    blockBuilder.NewLine(new IlCallStmt(ctorCall));
                    blockBuilder.Push(newInstance);
                    break;
                }
                case "newarr":
                {
                    var elemType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    var arrType = elemType.MakeArrayType();
                    var sizeExpr = blockBuilder.Pop();
                    IlExpr arrExpr = new IlNewArrayExpr(
                        arrType,
                        sizeExpr);
                    IlValue arrTemp = blockBuilder.GetNewTemp(arrExpr, blockBuilder.CurInstr.idx);
                    blockBuilder.NewLine(new IlAssignStmt(
                        arrTemp,
                        arrExpr
                    ));
                    blockBuilder.Push(arrTemp);
                    break;
                }
                case "initobj":
                {
                    var ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    var addr = blockBuilder.Pop();
                    blockBuilder.NewLine(new IlAssignStmt(PointerExprTypeResolver.Deref(addr, ilType),
                        new IlNewExpr(ilType)));
                    break;
                }
                case "ldelem.i":
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
                    var index = blockBuilder.Pop();
                    var arr = blockBuilder.Pop();
                    blockBuilder.Push(new IlArrayAccess(arr, index));
                    break;
                }
                case "ldelema":
                {
                    var idx = blockBuilder.Pop();
                    var arr = blockBuilder.Pop();
                    blockBuilder.Push(new IlManagedRef(new IlArrayAccess(arr, idx)));
                    break;
                }
                case "ldlen":
                {
                    var arr = blockBuilder.Pop();
                    blockBuilder.Push(new IlArrayLength(arr));
                    break;
                }
                case "stelem.i1":
                case "stelem.i2":
                case "stelem.i4":
                case "stelem.i8":
                case "stelem.r4":
                case "stelem.r8":
                case "stelem.ref":
                case "stelem.i":
                case "stelem":
                {
                    var value = blockBuilder.Pop();
                    var index = blockBuilder.Pop();
                    var arr = blockBuilder.Pop();
                    blockBuilder.NewLine(new IlAssignStmt(new IlArrayAccess(arr, index), value));
                    break;
                }
                case "conv.i1":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(sbyte)), value);
                    blockBuilder.Push(convCast);
                    break;
                }

                case "conv.i2":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(short)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.i4":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(int)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.i8":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(long)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.r4":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(float)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.r8":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(double)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.u1":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(byte)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.u2":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(ushort)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.u4":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(uint)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.u8":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(ulong)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.i":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(nint)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.u":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(nuint)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.r.un":
                {
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(NFloat)), value);
                    blockBuilder.Push(convCast);
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
                    var value = blockBuilder.Pop();
                    var convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(nint)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "isinst":
                {
                    var ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    var obj = blockBuilder.Pop();
                    IlExpr res = new IlIsInstExpr(ilType, obj);
                    blockBuilder.Push(res);
                    break;
                }
                case "castclass":
                {
                    var ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    var value = blockBuilder.Pop();
                    IlExpr casted = new IlConvCastExpr(ilType, value);
                    blockBuilder.Push(casted);
                    break;
                }
                case "box":
                {
                    var ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    var value = blockBuilder.Pop();
                    IlExpr boxed = new IlBoxExpr(ilType, value);
                    blockBuilder.Push(boxed);
                    break;
                }
                case "unbox":
                case "unbox.any":
                {
                    var ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    var obj = blockBuilder.Pop();
                    IlExpr unboxed = new IlUnboxExpr(ilType, obj);
                    blockBuilder.Push(unboxed);
                    break;
                }
                default: throw new Exception("unhandled frame.CurInstr " + blockBuilder.CurInstr);
            }

            if (blockBuilder.CurInstrIsLast()) return true;
            blockBuilder.CurInstr = blockBuilder.CurInstr.next;
        }
    }

    // TODO eliminate temp = newarr; temp = [1, 2, ..]; to temp = [1,2, ..];
    // TODO try using call instead
    private static void InlineInitArray(this BlockTacBuilder blockBuilder, List<IlExpr> rawArgs)
    {
        var args = rawArgs.ArrayInitCastDiscarded();
        if (args.First() is IlValue newArr && args.Last() is IlFieldRef fieldRef)
        {
            var arrVar = newArr as IlVar ?? throw new Exception("expected var, got " + newArr.Type);
            var expr = arrVar.Value as IlNewArrayExpr ??
                       throw new KnownBug("inline multidimensional array, got " +
                                          (arrVar.Value?.Type.ToString() ?? "null") +
                                          " instead of NewArrayExpr");
            var arrSize = (IlInt32Const)expr.Size;
            var elemType = ((IlArrayType)expr.Type).ElementType.Type;
            // TODO use runtime helpers initialize array instead 
            var tmp = Array.CreateInstance(elemType, arrSize.Value);
            var handle = GCHandle.Alloc(tmp, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr(fieldRef.Field.GetValue(null)!, handle.AddrOfPinnedObject(), false);
            }
            finally
            {
                handle.Free();
            }

            List<object> list = [.. tmp];
            var arrConst = new IlArrayConst((IlArrayType)IlInstanceBuilder.GetType(elemType.MakeArrayType()),
                list.Select(IlConstant.From));
            blockBuilder.NewLine(new IlAssignStmt(newArr, arrConst));
            return;
        }

        Console.WriteLine("bad array init values");
    }

    private static List<IlExpr> ArrayInitCastDiscarded(this List<IlExpr> args)
    {
        var res = new List<IlExpr>();
        Debug.Assert(args.Count == 2);

        if (args[0] is IlConvCastExpr fldCast && fldCast.Type.Type == typeof(RuntimeFieldHandle))
            res.Add(fldCast.Target);
        else res.Add(args[0]);

        if (args[1] is IlConvCastExpr arrCast && arrCast.Type.Type == typeof(Array))
            res.Add(arrCast.Target);
        else res.Add(args[1]);

        return res;
    }
}