using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TACBuilder.ILMeta;
using TACBuilder.ILMeta.ILBodyParser;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder
{
    static class BlockTacLineBuilder
    {
        /*
         * returns true if successors may be rebuilt
         */
        public static bool Rebuild(this BlockTacBuilder blockBuilder)
        {
            var sameStack = blockBuilder.StackInitIsTheSame() && blockBuilder.BuiltAtLeastOnce;
            if (sameStack) return false;
            blockBuilder._builtAtLeastOnce = true;

            blockBuilder.ResetStackToInitial();
            blockBuilder.TacLines.Clear();
            blockBuilder.CurInstr = blockBuilder._firstInstr;
            while (true)
            {
                if (blockBuilder.CurInstr is ILInstr.SwitchArg switchBranch)
                {
                    ILInstrOperand.Target target = (ILInstrOperand.Target)switchBranch.arg;
                    Debug.Assert(blockBuilder._switchRegister is not null);
                    // targets.Add(target.value);
                    blockBuilder.NewLine(new ILIfStmt(
                        new ILBinaryOperation(blockBuilder._switchRegister,
                            new ILLiteral(new ILInt32(), switchBranch.Value.ToString())),
                        target.value.idx
                    ));
                    blockBuilder.Successors.ForEach(s => s._switchRegister = blockBuilder._switchRegister);
                    if (blockBuilder.CurInstrIsLast()) return true;
                }

                Debug.Assert(blockBuilder.CurInstr is ILInstr.Instr,
                    blockBuilder.CurInstr.ToString() + " on " + blockBuilder.Meta.MethodMeta.Name);
                switch (((ILInstr.Instr)blockBuilder.CurInstr).opCode.Name)
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
                    case "cpblk":
                        throw new Exception("not implemented " + ((ILInstr.Instr)blockBuilder.CurInstr).opCode.Name);

                    case "constrained.":
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
                        ILExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[0], value));
                        break;
                    }
                    case "stloc.1":
                    {
                        ILExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[1], value));
                        break;
                    }
                    case "stloc.2":
                    {
                        ILExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[2], value));
                        break;
                    }
                    case "stloc.3":
                    {
                        ILExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[3], value));
                        break;
                    }
                    case "stloc.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                        ILExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[idx], value));
                        break;
                    }
                    case "starg":
                    {
                        int idx = ((ILInstrOperand.Arg16)blockBuilder.CurInstr.arg).value;
                        ILExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Params[idx], value));
                        break;
                    }
                    case "starg.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                        ILExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Params[idx], value));
                        break;
                    }
                    case "arglist":
                    {
                        blockBuilder.Push(new ILVarArgValue(blockBuilder.Meta.MethodMeta.Name));
                        break;
                    }
                    case "throw":
                    {
                        ILExpr obj = blockBuilder.Pop();
                        blockBuilder.ClearStack();
                        blockBuilder.NewLine(new ILEHStmt("throw", obj));
                        return false;
                    }
                    case "rethrow":
                    {
                        blockBuilder.NewLine(new ILEHStmt("rethrow"));
                        break;
                    }
                    case "endfault":
                    {
                        blockBuilder.NewLine(new ILEHStmt("endfault"));
                        blockBuilder.ClearStack();
                        return false;
                    }
                    case "endfinally":
                    {
                        blockBuilder.NewLine(new ILEHStmt("endfinally"));
                        blockBuilder.ClearStack();
                        return false;
                    }
                    case "endfilter":
                    {
                        ILExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILEHStmt("endfilter", value));
                        return true;
                    }
                    case "localloc":
                    {
                        ILExpr size = blockBuilder.Pop();
                        blockBuilder.Push(new ILStackAlloc(size));
                        break;
                    }
                    case "ldfld":
                    {
                        FieldMeta field = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        ILExpr inst = blockBuilder.Pop();
                        blockBuilder.Push(ILField.Instance(field, inst));
                        break;
                    }
                    case "ldflda":
                    {
                        FieldMeta field = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        ILExpr inst = blockBuilder.Pop();
                        ILField ilField = ILField.Instance(field, inst);
                        if (inst.Type is ILUnmanagedPointer)
                        {
                            blockBuilder.Push(new ILUnmanagedRef(ilField));
                        }
                        else
                        {
                            blockBuilder.Push(new ILManagedRef(ilField));
                        }

                        break;
                    }

                    case "ldsfld":
                    {
                        FieldMeta field = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(ILField.Static(field));
                        break;
                    }
                    case "ldsflda":
                    {
                        FieldMeta field = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        ILField ilField = ILField.Static(field);
                        if (field.Type!.BaseType.IsUnmanaged())
                        {
                            blockBuilder.Push(new ILUnmanagedRef(ilField));
                        }
                        else
                        {
                            blockBuilder.Push(new ILManagedRef(ilField));
                        }

                        break;
                    }

                    case "stfld":
                    {
                        FieldMeta field = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        ILExpr value = blockBuilder.Pop();
                        ILExpr obj = blockBuilder.Pop();
                        ILField ilField = ILField.Instance(field, obj);
                        blockBuilder.NewLine(new ILAssignStmt(ilField, value));
                        break;
                    }
                    case "stsfld":
                    {
                        FieldMeta field = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        ILExpr value = blockBuilder.Pop();
                        ILField ilField = ILField.Static(field);
                        blockBuilder.NewLine(new ILAssignStmt(ilField, value));
                        break;
                    }
                    case "sizeof":
                    {
                        TypeMeta typeMeta = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new ILSizeOfExpr(TypingUtil.ILTypeFrom(typeMeta.Type)));
                        break;
                    }
                    case "ldind.i1":
                    case "ldind.i2":
                    case "ldind.i4":
                    case "ldind.u1":
                    case "ldind.u2":
                    case "ldind.u4":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILInt32());
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.u8":
                    case "ldind.i8":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILInt64());
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.r4":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeFloat());
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.r8":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeFloat());
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.i":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILNativeInt());
                        blockBuilder.Push(deref);
                        break;
                    }

                    case "ldind.ref":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILObject());
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldobj":
                    {
                        TypeMeta typeMeta = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, TypingUtil.ILTypeFrom(typeMeta.Type));
                        blockBuilder.Push(deref);
                        break;
                    }

                    case "stind.i1":
                    case "stind.i2":
                    case "stind.i4":
                    case "stind.i8":
                    {
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(
                            new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILInt32()), val));
                        break;
                    }
                    case "stind.r4":
                    {
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILFloat32()),
                            val));
                        break;
                    }
                    case "stind.r8":
                    {
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILFloat64()),
                            val));
                        break;
                    }
                    case "stind.i":
                    {
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILNativeInt()),
                            val));
                        break;
                    }
                    case "stind.ref":
                    {
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILObject()),
                            val));
                        break;
                    }
                    case "stobj":
                    {
                        TypeMeta typeMeta = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, TypingUtil.ILTypeFrom(typeMeta.Type)),
                            val));
                        break;
                    }
                    case "ldarga":
                    {
                        int idx = ((ILInstrOperand.Arg16)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new ILManagedRef(blockBuilder.Params[idx]));
                        break;
                    }
                    case "ldarga.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new ILManagedRef(blockBuilder.Params[idx]));
                        break;
                    }
                    case "ldloca":
                    {
                        int idx = ((ILInstrOperand.Arg16)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new ILManagedRef(blockBuilder.Locals[idx]));
                        break;
                    }
                    case "ldloca.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new ILManagedRef(blockBuilder.Locals[idx]));
                        break;
                    }
                    case "leave":
                    case "leave.s":
                    {
                        ILInstr target = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                        blockBuilder.ClearStack();
                        return false;
                    }
                    case "switch":
                    {
                        int branchCnt = ((ILInstrOperand.Arg32)blockBuilder.CurInstr.arg).value;
                        ILExpr compVal = blockBuilder.Pop();
                        blockBuilder.Successors.ForEach(s => s._switchRegister = compVal);
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
                        MethodMeta methodMeta =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;

                        ILMethod method = new ILMethod(methodMeta);
                        blockBuilder.Push(method);
                        break;
                    }
                    case "ldvirtftn":
                    {
                        MethodMeta methodMeta =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                        ILMethod ilMethod = new ILMethod(methodMeta);
                        ilMethod.Receiver = blockBuilder.Pop();
                        blockBuilder.Push(ilMethod);
                        break;
                    }
                    case "ldnull":
                        blockBuilder.Push(new ILNullValue());
                        break;
                    case "ldc.i4.m1":
                    case "ldc.i4.M1":
                        blockBuilder.PushLiteral(-1);
                        break;
                    case "ldc.i4.0":
                        blockBuilder.PushLiteral(0);
                        break;
                    case "ldc.i4.1":
                        blockBuilder.PushLiteral(1);
                        break;
                    case "ldc.i4.2":
                        blockBuilder.PushLiteral(2);
                        break;
                    case "ldc.i4.3":
                        blockBuilder.PushLiteral(3);
                        break;
                    case "ldc.i4.4":
                        blockBuilder.PushLiteral(4);
                        break;
                    case "ldc.i4.5":
                        blockBuilder.PushLiteral(5);
                        break;
                    case "ldc.i4.6":
                        blockBuilder.PushLiteral(6);
                        break;
                    case "ldc.i4.7":
                        blockBuilder.PushLiteral(7);
                        break;
                    case "ldc.i4.8":
                        blockBuilder.PushLiteral(8);
                        break;
                    case "ldc.i4.s":
                        blockBuilder.PushLiteral<int>(((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value);
                        break;
                    case "ldc.i4":
                        blockBuilder.PushLiteral(((ILInstrOperand.Arg32)blockBuilder.CurInstr.arg).value);
                        break;
                    case "ldc.i8":
                        blockBuilder.PushLiteral(((ILInstrOperand.Arg64)blockBuilder.CurInstr.arg).value);
                        break;
                    case "ldc.r4":
                        blockBuilder.PushLiteral<float>(((ILInstrOperand.Arg32)blockBuilder.CurInstr.arg).value);
                        break;
                    case "ldc.r8":
                        blockBuilder.PushLiteral<double>(((ILInstrOperand.Arg64)blockBuilder.CurInstr.arg).value);
                        break;
                    case "ldstr":
                        blockBuilder.PushLiteral(((ILInstrOperand.ResolvedString)blockBuilder.CurInstr.arg).value);
                        break;
                    case "dup":
                    {
                        ILExpr dup = blockBuilder.Pop();
                        blockBuilder.Push(dup);
                        blockBuilder.Push(dup);
                        break;
                    }
                    case "pop":
                        blockBuilder.Pop();
                        break;
                    case "ldtoken":
                    {
                        MemberMeta memberMeta = ((ILInstrOperand.ResolvedMember)blockBuilder.CurInstr.arg).value;
                        var token = new ILObjectLiteral(new ILHandleRef(), memberMeta.Name);
                        blockBuilder.Push(token);
                        break;
                    }
                    case "call":
                    {
                        MethodMeta methodMeta =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;

                        ILMethod ilMethod = new ILMethod(methodMeta);
                        ilMethod.LoadArgs(blockBuilder.Pop);
                        if (ilMethod.IsInitializeArray())
                        {
                            blockBuilder.InlineInitArray(ilMethod.Args);
                        }
                        else
                        {
                            var call = new ILCallExpr(ilMethod);
                            if (ilMethod.Returns())
                            {
                                var tmp = blockBuilder.GetNewTemp(call, blockBuilder.CurInstr.idx);
                                blockBuilder.NewLine(new ILAssignStmt(tmp, call));
                                blockBuilder.Push(tmp);
                            }
                            else
                                blockBuilder.NewLine(new ILCallStmt(call));
                        }

                        break;
                    }
                    case "callvirt":
                    {
                        MethodMeta methodMeta =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                        ILMethod ilMethod = new ILMethod(methodMeta);
                        ilMethod.LoadArgs(blockBuilder.Pop);
                        var call = new ILCallExpr(ilMethod);
                        if (ilMethod.Returns())
                            blockBuilder.Push(call);
                        else
                            blockBuilder.NewLine(new ILCallStmt(call));
                        break;
                    }
                    case "ret":
                    {
                        var methodMeta = blockBuilder.Meta.MethodMeta!;

                        ILExpr? retVal = null;
                        if (methodMeta.ReturnType != null && methodMeta.ReturnType.BaseType != typeof(void))
                            retVal = blockBuilder.Pop();
                        blockBuilder.NewLine(
                            new ILReturnStmt(retVal)
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
                        ILExpr rhs = blockBuilder.Pop();
                        ILExpr lhs = blockBuilder.Pop();
                        ILBinaryOperation op = new ILBinaryOperation(lhs, rhs);
                        blockBuilder.Push(op);
                        break;
                    }
                    case "neg":
                    case "not":
                    {
                        ILExpr operand = blockBuilder.Pop();
                        ILUnaryOperation op = new ILUnaryOperation(operand);
                        blockBuilder.Push(op);
                        break;
                    }

                    case "br.s":
                    case "br":
                    {
                        ILInstr target = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                        // frame.ContinueBranchingTo(target, null);
                        return true;
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
                        ILExpr lhs = blockBuilder.Pop();
                        ILExpr rhs = blockBuilder.Pop();
                        ILInstr tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                        ILInstr fb = blockBuilder.CurInstr.next;
                        blockBuilder.NewLine(new ILIfStmt(
                            new ILBinaryOperation(lhs, rhs),
                            tb.idx));
                        // frame.ContinueBranchingTo(fb, tb);
                        return true;
                    }
                    case "brinst":
                    case "brinst.s":
                    case "brtrue.s":
                    case "brtrue":
                    {
                        blockBuilder.PushLiteral<bool>(true);
                        ILExpr rhs = blockBuilder.Pop();
                        ILExpr lhs = blockBuilder.Pop();
                        ILInstr tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                        ILInstr fb = blockBuilder.CurInstr.next;
                        blockBuilder.NewLine(new ILIfStmt(
                            new ILBinaryOperation(lhs, rhs), tb.idx));
                        return true;
                    }
                    case "brnull":
                    case "brnull.s":
                    case "brzero":
                    case "brzero.s":
                    case "brfalse.s":
                    case "brfalse":
                    {
                        blockBuilder.PushLiteral<bool>(false);
                        ILExpr rhs = blockBuilder.Pop();
                        ILExpr lhs = blockBuilder.Pop();
                        ILInstr tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                        ILInstr fb = blockBuilder.CurInstr.next;
                        blockBuilder.NewLine(new ILIfStmt(
                            new ILBinaryOperation(lhs, rhs), tb.idx));
                        return true;
                    }
                    case "newobj":
                    {
                        // TODO hide method info
                        MethodMeta methodMeta = ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                        // ctor does not have return type
                        Debug.Assert(methodMeta.ReturnType is null);
                        int arity = methodMeta.ParametersType.Count;
                        TypeMeta objType = methodMeta.DeclaringType!;
                        ILExpr[] inParams = new ILExpr[arity];
                        for (int i = 0; i < arity; i++)
                        {
                            inParams[i] = blockBuilder.Pop();
                        }

                        blockBuilder.Push(new ILNewExpr(
                            TypingUtil.ILTypeFrom(objType.BaseType),
                            inParams));
                        break;
                    }
                    case "newarr":
                    {
                        TypeMeta arrTypeMeta = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr sizeExpr = blockBuilder.Pop();
                        if (sizeExpr.Type is not ILInt32 && sizeExpr.Type is not ILNativeInt)
                        {
                            throw new Exception("expected arr size of type int32 or native int, got " +
                                                sizeExpr.Type);
                        }

                        ILArray resolvedType = new ILArray(arrTypeMeta.Type, TypingUtil.ILTypeFrom(arrTypeMeta.Type));
                        ILExpr arrExpr = new ILNewArrayExpr(
                            resolvedType,
                            sizeExpr);
                        ILLValue arrTemp = blockBuilder.GetNewTemp(arrExpr, blockBuilder.CurInstr.idx);
                        blockBuilder.NewLine(new ILAssignStmt(
                            arrTemp,
                            arrExpr
                        ));
                        blockBuilder.Push(arrTemp);
                        break;
                    }
                    case "initobj":
                    {
                        TypeMeta typeMeta = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr addr = blockBuilder.Pop();
                        ILType ilType = TypingUtil.ILTypeFrom(typeMeta.Type);
                        blockBuilder.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, ilType),
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
                        ILExpr index = blockBuilder.Pop();
                        ILExpr arr = blockBuilder.Pop();
                        blockBuilder.Push(new ILArrayAccess(arr, index));
                        break;
                    }
                    case "ldelema":
                    {
                        ILExpr idx = blockBuilder.Pop();
                        ILExpr arr = blockBuilder.Pop();
                        blockBuilder.Push(new ILManagedRef(new ILArrayAccess(arr, idx)));
                        break;
                    }
                    case "ldlen":
                    {
                        ILExpr arr = blockBuilder.Pop();
                        blockBuilder.Push(new ILArrayLength(arr));
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
                        ILExpr value = blockBuilder.Pop();
                        ILExpr index = blockBuilder.Pop();
                        ILExpr arr = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(new ILArrayAccess(arr, index), value));
                        break;
                    }
                    case "conv.i1":
                    case "conv.i2":
                    case "conv.i4":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILInt32(), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.i8":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILInt64(), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.r4":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILFloat32(), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.r8":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILFloat64(), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.u1":
                    case "conv.u2":
                    case "conv.u4":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILInt32(), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.u8":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILInt64(), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.i":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.u":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.r.un":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILNativeFloat(), value);
                        blockBuilder.Push(conv);
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
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILNativeInt(), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "isinst":
                    {
                        TypeMeta typeMeta = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr obj = blockBuilder.Pop();
                        ILExpr res = new ILCondCastExpr(TypingUtil.ILTypeFrom(typeMeta.Type), obj);
                        blockBuilder.Push(res);
                        break;
                    }
                    case "castclass":
                    {
                        TypeMeta typeMeta = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr value = blockBuilder.Pop();
                        ILExpr casted = new ILCastClassExpr(TypingUtil.ILTypeFrom(typeMeta.Type), value);
                        blockBuilder.Push(casted);
                        break;
                    }
                    case "box":
                    {
                        // TODO use typeMeta
                        TypeMeta typeMeta = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILValue value = (ILValue)blockBuilder.Pop();
                        ILExpr boxed = new ILBoxExpr(value);
                        blockBuilder.Push(boxed);
                        break;
                    }
                    case "unbox":
                    case "unbox.any":
                    {
                        TypeMeta typeMeta = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILValue obj = (ILValue)blockBuilder.Pop();
                        ILExpr unboxed = new ILUnboxExpr(TypingUtil.ILTypeFrom(typeMeta.Type), obj);
                        blockBuilder.Push(unboxed);
                        break;
                    }
                    default: throw new Exception("unhandled frame.CurInstr " + blockBuilder.CurInstr);
                }

                if (blockBuilder.CurInstrIsLast()) return true;
                blockBuilder.CurInstr = blockBuilder.CurInstr.next;
            }
        }

        private static void InlineInitArray(this BlockTacBuilder blockBuilder, List<ILExpr> args)
        {
            if (args.First() is ILLocal newArr && args.Last() is ILObjectLiteral ilObj)
            {
                try
                {
                    ILNewArrayExpr expr =
                        (ILNewArrayExpr)blockBuilder.Temps[NamingUtil.TakeIndexFrom(newArr.ToString())].Value;
                    int arrSize = int.Parse(expr.Size.ToString());
                    Type arrType = ((ILPrimitiveType)expr.Type).ReflectedType;
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
                    for (int i = 0; i < list.Count; i++)
                    {
                        blockBuilder.NewLine(new ILAssignStmt(
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
}
