using System.Diagnostics;
using System.Runtime.InteropServices;
using TACBuilder.BodyBuilder;
using TACBuilder.ILReflection;
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
                            new ILLiteral(new ILType(typeof(int)), switchBranch.Value.ToString())),
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
                        blockBuilder.NewLine(new ILAssignStmt((ILLocal)blockBuilder.Params[idx], value));
                        break;
                    }
                    case "starg.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                        ILExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt((ILLocal)blockBuilder.Params[idx], value));
                        break;
                    }
                    case "arglist":
                    {
                        blockBuilder.Push(new ILArgumentHandle(blockBuilder.Meta.MethodMeta));
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
                        ILField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        ILExpr inst = blockBuilder.Pop();
                        blockBuilder.Push(new ILFieldAccess(ilField, inst));
                        break;
                    }
                    case "ldflda":
                    {
                        ILField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        ILExpr inst = blockBuilder.Pop();
                        ILFieldAccess ilFieldAccess = new ILFieldAccess(ilField, inst);
                        blockBuilder.Push(inst.Type.IsManaged switch
                        {
                            true => new ILManagedRef(ilFieldAccess),
                            _ => new ILUnmanagedRef(ilFieldAccess)
                        });
                        break;
                    }

                    case "ldsfld":
                    {
                        ILField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new ILFieldAccess(ilField));
                        break;
                    }
                    case "ldsflda":
                    {
                        ILField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        ILFieldAccess ilFieldAccess = new ILFieldAccess(ilField);
                        if (ilField.Type!.BaseType.IsUnmanaged())
                        {
                            blockBuilder.Push(new ILUnmanagedRef(ilFieldAccess));
                        }
                        else
                        {
                            blockBuilder.Push(new ILManagedRef(ilFieldAccess));
                        }

                        break;
                    }

                    case "stfld":
                    {
                        ILField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        ILExpr value = blockBuilder.Pop();
                        ILExpr obj = blockBuilder.Pop();
                        ILFieldAccess ilFieldAccess = new ILFieldAccess(ilField, obj);
                        blockBuilder.NewLine(new ILAssignStmt(ilFieldAccess, value));
                        break;
                    }
                    case "stsfld":
                    {
                        ILField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        ILExpr value = blockBuilder.Pop();
                        ILFieldAccess ilFieldAccess = new ILFieldAccess(ilField);
                        blockBuilder.NewLine(new ILAssignStmt(ilFieldAccess, value));
                        break;
                    }
                    case "sizeof":
                    {
                        ILType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new ILSizeOfExpr(ilType));
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
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(int)));
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.u8":
                    case "ldind.i8":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(long)));
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.r4":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(NFloat)));
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.r8":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(NFloat)));
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.i":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(nint)));
                        blockBuilder.Push(deref);
                        break;
                    }

                    case "ldind.ref":
                    {
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref = PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(object)));
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldobj":
                    {
                        ILType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref =
                            PointerExprTypeResolver.DerefAs(addr, ilType);
                        blockBuilder.Push(deref);
                        break;
                    }

                    case "stind.i1":
                    case "stind.i2":
                    case "stind.i4":
                    case "stind.i8":
                    {
                        ILExpr val = blockBuilder.Pop();
                        ILExpr addr = blockBuilder.Pop();
                        blockBuilder.NewLine(
                            new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(int))), val));
                        break;
                    }
                    case "stind.r4":
                    {
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(float))),
                            val));
                        break;
                    }
                    case "stind.r8":
                    {
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(double))),
                            val));
                        break;
                    }
                    case "stind.i":
                    {
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(nint))),
                            val));
                        break;
                    }
                    case "stind.ref":
                    {
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, new ILType(typeof(object))),
                            val));
                        break;
                    }
                    case "stobj":
                    {
                        ILType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr val = blockBuilder.Pop();
                        ILLValue addr = (ILLValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, ilType),
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
                        ILMethod ilMethod =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;

                        ILCall call = new ILCall(ilMethod);
                        blockBuilder.Push(call);
                        break;
                    }
                    case "ldvirtftn":
                    {
                        ILMethod ilMethod =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                        ILCall ilCall = new ILCall(ilMethod, blockBuilder.Pop());
                        blockBuilder.Push(new ILManagedRef(ilCall));
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
                        ILMember ilMember = ((ILInstrOperand.ResolvedMember)blockBuilder.CurInstr.arg).value;
                        var token = new ILMemberToken(ilMember);
                        blockBuilder.Push(token);
                        break;
                    }
                    case "call":
                    {
                        ILMethod ilMethod =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;

                        ILCall ilCall = new ILCall(ilMethod);
                        ilCall.LoadArgs(blockBuilder.Pop);
                        if (ilCall.IsInitializeArray())
                        {
                            blockBuilder.InlineInitArray(ilCall.Args);
                        }
                        else
                        {
                            if (ilCall.Returns())
                            {
                                var tmp = blockBuilder.GetNewTemp(ilCall, blockBuilder.CurInstr.idx);
                                blockBuilder.NewLine(new ILAssignStmt(tmp, ilCall));
                                blockBuilder.Push(tmp);
                            }
                            else
                                blockBuilder.NewLine(new ILCallStmt(ilCall));
                        }

                        break;
                    }
                    case "callvirt":
                    {
                        ILMethod ilMethod =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                        ILCall ilCall = new ILCall(ilMethod);
                        ilCall.LoadArgs(blockBuilder.Pop);
                        if (ilCall.Returns())
                        {
                            var tmp = blockBuilder.GetNewTemp(ilCall, blockBuilder.CurInstr.idx);
                            blockBuilder.NewLine(new ILAssignStmt(tmp, ilCall));
                            blockBuilder.Push(tmp);
                        }
                        else
                            blockBuilder.NewLine(new ILCallStmt(ilCall));

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
                        ILMethod ilMethod = ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                        // ctor does not have return type
                        Debug.Assert(ilMethod.ReturnType is null);
                        int arity = ilMethod.Parameters.Count - 1; // TODO is hardcode -1 for `this` ok?
                        ILType objIlType = ilMethod.DeclaringType!;
                        ILExpr[] inParams = new ILExpr[arity];
                        for (int i = 0; i < arity; i++)
                        {
                            inParams[i] = blockBuilder.Pop();
                        }

                        blockBuilder.Push(new ILNewExpr(objIlType, inParams));
                        break;
                    }
                    case "newarr":
                    {
                        ILType arrIlType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr sizeExpr = blockBuilder.Pop();
                        if (!Equals(sizeExpr.Type, new ILType(typeof(int))) &&
                            !Equals(sizeExpr.Type, new ILType(typeof(nint))))
                        {
                            throw new Exception("expected arr size of type int32 or native int, got " +
                                                sizeExpr.Type);
                        }

                        ILExpr arrExpr = new ILNewArrayExpr(
                            arrIlType,
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
                        ILType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr addr = blockBuilder.Pop();
                        // ILType ilType = TypingUtil.ILTypeFrom(typeMeta.Type);
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
                    case "stelem.i":
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
                        ILConvExpr conv = new ILConvExpr(new ILType(typeof(int)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.i8":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILType(typeof(long)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.r4":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILType(typeof(float)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.r8":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILType(typeof(double)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.u1":
                    case "conv.u2":
                    case "conv.u4":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILType(typeof(uint)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.u8":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILType(typeof(ulong)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.i":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILType(typeof(nint)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.u":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILType(typeof(nuint)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.r.un":
                    {
                        ILExpr value = blockBuilder.Pop();
                        ILConvExpr conv = new ILConvExpr(new ILType(typeof(NFloat)), value);
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
                        ILConvExpr conv = new ILConvExpr(new ILType(typeof(nint)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "isinst":
                    {
                        ILType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr obj = blockBuilder.Pop();
                        ILExpr res = new ILCondCastExpr(ilType, obj);
                        blockBuilder.Push(res);
                        break;
                    }
                    case "castclass":
                    {
                        ILType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILExpr value = blockBuilder.Pop();
                        ILExpr casted = new ILCastClassExpr(ilType, value);
                        blockBuilder.Push(casted);
                        break;
                    }
                    case "box":
                    {
                        // TODO use typeMeta
                        ILType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILValue value = (ILValue)blockBuilder.Pop();
                        ILExpr boxed = new ILBoxExpr(value);
                        blockBuilder.Push(boxed);
                        break;
                    }
                    case "unbox":
                    case "unbox.any":
                    {
                        ILType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        ILValue obj = (ILValue)blockBuilder.Pop();
                        ILExpr unboxed = new ILUnboxExpr(ilType, obj);
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
            if (args.First() is ILLValue newArr && args.Last() is ILMemberToken { Value: ILField field })
            {
                try
                {
                    ILNewArrayExpr expr = (newArr as TempVar)?.Value as ILNewArrayExpr ??
                                          throw new Exception("expected temp var");
                    int arrSize = int.Parse(expr.Size.ToString());
                    Type arrType = expr.Type.BaseType;
                    var tmp = Array.CreateInstance(arrType, arrSize);
                    GCHandle handle = GCHandle.Alloc(tmp, GCHandleType.Pinned);
                    try
                    {
                        Marshal.StructureToPtr(field.Info.GetValue(null)!, handle.AddrOfPinnedObject(), false);
                    }
                    finally
                    {
                        handle.Free();
                    }

                    List<object> list = [.. tmp];
                    for (int i = 0; i < list.Count; i++)
                    {
                        blockBuilder.NewLine(new ILAssignStmt(
                            new ILArrayAccess(newArr, new ILLiteral(new ILType(typeof(int)), i.ToString())),
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
