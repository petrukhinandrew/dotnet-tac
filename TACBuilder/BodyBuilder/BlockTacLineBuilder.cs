using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TACBuilder.BodyBuilder;
using TACBuilder.Exprs;
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
                    blockBuilder.NewLine(new IlIfStmt(
                        new IlBinaryOperation(blockBuilder._switchRegister,
                            new IlIntConst(switchBranch.Value)),
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
                    case "arglist":
                        throw new Exception("not implemented " + ((ILInstr.Instr)blockBuilder.CurInstr).opCode.Name);

                    case "constrained.":
                    case "volatile.":
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
                        IlExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[0], value));
                        break;
                    }
                    case "stloc.1":
                    {
                        IlExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[1], value));
                        break;
                    }
                    case "stloc.2":
                    {
                        IlExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[2], value));
                        break;
                    }
                    case "stloc.3":
                    {
                        IlExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[3], value));
                        break;
                    }
                    case "stloc.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                        IlExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[idx], value));
                        break;
                    }
                    case "starg":
                    {
                        int idx = ((ILInstrOperand.Arg16)blockBuilder.CurInstr.arg).value;
                        IlExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt((IlLocal)blockBuilder.Params[idx], value));
                        break;
                    }
                    case "starg.s":
                    {
                        int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                        IlExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt((IlLocal)blockBuilder.Params[idx], value));
                        break;
                    }
                    case "throw":
                    {
                        IlExpr obj = blockBuilder.Pop();
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
                        IlExpr value = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILEHStmt("endfilter", value));
                        return true;
                    }
                    case "localloc":
                    {
                        IlExpr size = blockBuilder.Pop();
                        blockBuilder.Push(new IlStackAlloc(size));
                        break;
                    }
                    case "ldfld":
                    {
                        IlField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        IlExpr inst = blockBuilder.Pop();
                        blockBuilder.Push(new IlFieldAccess(ilField, inst));
                        break;
                    }
                    case "ldflda":
                    {
                        IlField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        IlExpr inst = blockBuilder.Pop();
                        IlFieldAccess ilFieldAccess = new IlFieldAccess(ilField, inst);
                        blockBuilder.Push(inst.Type.IsManaged switch
                        {
                            true => new IlManagedRef(ilFieldAccess),
                            _ => new IlUnmanagedRef(ilFieldAccess)
                        });
                        break;
                    }

                    case "ldsfld":
                    {
                        IlField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new IlFieldAccess(ilField));
                        break;
                    }
                    case "ldsflda":
                    {
                        IlField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        IlFieldAccess ilFieldAccess = new IlFieldAccess(ilField);
                        if (ilField.Type!.BaseType.IsUnmanaged())
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
                        IlField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        IlExpr value = blockBuilder.Pop();
                        IlExpr obj = blockBuilder.Pop();
                        IlFieldAccess ilFieldAccess = new IlFieldAccess(ilField, obj);
                        blockBuilder.NewLine(new ILAssignStmt(ilFieldAccess, value));
                        break;
                    }
                    case "stsfld":
                    {
                        IlField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                        IlExpr value = blockBuilder.Pop();
                        IlFieldAccess ilFieldAccess = new IlFieldAccess(ilField);
                        blockBuilder.NewLine(new ILAssignStmt(ilFieldAccess, value));
                        break;
                    }
                    case "sizeof":
                    {
                        IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new IlSizeOfExpr(ilType));
                        break;
                    }
                    case "ldind.i1":
                    case "ldind.i2":
                    case "ldind.i4":
                    case "ldind.u1":
                    case "ldind.u2":
                    case "ldind.u4":
                    {
                        IlExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref =
                            PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(int)));
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.u8":
                    case "ldind.i8":
                    {
                        IlExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref =
                            PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(long)));
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.r4":
                    {
                        IlExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref =
                            PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(NFloat)));
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.r8":
                    {
                        IlExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref =
                            PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(NFloat)));
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldind.i":
                    {
                        IlExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref =
                            PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(nint)));
                        blockBuilder.Push(deref);
                        break;
                    }

                    case "ldind.ref":
                    {
                        IlExpr addr = blockBuilder.Pop();
                        ILDerefExpr deref =
                            PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(object)));
                        blockBuilder.Push(deref);
                        break;
                    }
                    case "ldobj":
                    {
                        IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        IlExpr addr = blockBuilder.Pop();
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
                        IlExpr val = blockBuilder.Pop();
                        IlExpr addr = blockBuilder.Pop();
                        blockBuilder.NewLine(
                            new ILAssignStmt(
                                PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(int))), val));
                        break;
                    }
                    case "stind.r4":
                    {
                        IlExpr val = blockBuilder.Pop();
                        IlValue addr = (IlValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(float))),
                            val));
                        break;
                    }
                    case "stind.r8":
                    {
                        var val = blockBuilder.Pop();
                        var addr = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(double))),
                            val));
                        break;
                    }
                    case "stind.i":
                    {
                        var val = blockBuilder.Pop();
                        var addr = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(nint))),
                            val));
                        break;
                    }
                    case "stind.ref":
                    {
                        IlExpr val = blockBuilder.Pop();
                        IlValue addr = (IlValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, IlInstanceBuilder.GetType(typeof(object))),
                            val));
                        break;
                    }
                    case "stobj":
                    {
                        IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        IlExpr val = blockBuilder.Pop();
                        IlValue addr = (IlValue)blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(
                            PointerExprTypeResolver.DerefAs(addr, ilType),
                            val));
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
                        ILInstr target = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                        blockBuilder.ClearStack();
                        return false;
                    }
                    case "switch":
                    {
                        int branchCnt = ((ILInstrOperand.Arg32)blockBuilder.CurInstr.arg).value;
                        IlExpr compVal = blockBuilder.Pop();
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
                        IlMethod ilMethod =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new IlMethodRef(ilMethod));
                        break;
                    }
                    case "ldvirtftn":
                    {
                        IlMethod ilMethod =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                        blockBuilder.Push(new IlMethodRef(ilMethod, blockBuilder.Pop()));
                        break;
                    }
                    case "ldnull":
                        blockBuilder.Push(new IlNullConst());
                        break;
                    case "ldc.i4.m1":
                    case "ldc.i4.M1":
                        blockBuilder.Push(new IlIntConst(-1));
                        break;
                    case "ldc.i4.0":
                        blockBuilder.Push(new IlIntConst(0));
                        break;
                    case "ldc.i4.1":
                        blockBuilder.Push(new IlIntConst(1));
                        break;
                    case "ldc.i4.2":
                        blockBuilder.Push(new IlIntConst(2));
                        break;
                    case "ldc.i4.3":
                        blockBuilder.Push(new IlIntConst(3));
                        break;
                    case "ldc.i4.4":
                        blockBuilder.Push(new IlIntConst(4));
                        break;
                    case "ldc.i4.5":
                        blockBuilder.Push(new IlIntConst(5));
                        break;
                    case "ldc.i4.6":
                        blockBuilder.Push(new IlIntConst(6));
                        break;
                    case "ldc.i4.7":
                        blockBuilder.Push(new IlIntConst(7));
                        break;
                    case "ldc.i4.8":
                        blockBuilder.Push(new IlIntConst(8));
                        break;
                    case "ldc.i4.s":
                        blockBuilder.Push(new IlIntConst(((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value));
                        break;
                    case "ldc.i4":
                        blockBuilder.Push(new IlIntConst(((ILInstrOperand.Arg32)blockBuilder.CurInstr.arg).value));
                        break;
                    case "ldc.i8":
                        blockBuilder.Push(new IlLongConst(((ILInstrOperand.Arg64)blockBuilder.CurInstr.arg).value));
                        break;
                    case "ldc.r4":
                        blockBuilder.Push(new IlFloatConst(((ILInstrOperand.Arg32)blockBuilder.CurInstr.arg).value));
                        break;
                    case "ldc.r8":
                        blockBuilder.Push(new IlDoubleConst(((ILInstrOperand.Arg64)blockBuilder.CurInstr.arg).value));
                        break;
                    case "ldstr":
                        blockBuilder.Push(new IlStringConst(((ILInstrOperand.ResolvedString)blockBuilder.CurInstr.arg)
                            .value));
                        break;
                    case "dup":
                    {
                        IlExpr dup = blockBuilder.Pop();
                        blockBuilder.Push(dup);
                        blockBuilder.Push(dup);
                        break;
                    }
                    case "pop":
                        blockBuilder.Pop();
                        break;
                    case "ldtoken":
                    {
                        IlMember ilMember = ((ILInstrOperand.ResolvedMember)blockBuilder.CurInstr.arg).value;
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
                        IlMethod ilMethod =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;

                        IlCall ilCall = new IlCall(ilMethod);
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
                                blockBuilder.NewLine(new IlCallStmt(ilCall));
                        }

                        break;
                    }
                    case "callvirt":
                    {
                        IlMethod ilMethod =
                            ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                        IlCall ilCall = new IlCall(ilMethod);
                        ilCall.LoadArgs(blockBuilder.Pop);
                        if (ilCall.Returns())
                        {
                            var tmp = blockBuilder.GetNewTemp(ilCall, blockBuilder.CurInstr.idx);
                            blockBuilder.NewLine(new ILAssignStmt(tmp, ilCall));
                            blockBuilder.Push(tmp);
                        }
                        else
                            blockBuilder.NewLine(new IlCallStmt(ilCall));

                        break;
                    }
                    case "ret":
                    {
                        var methodMeta = blockBuilder.Meta.MethodMeta!;

                        IlExpr? retVal = null;
                        if (methodMeta.ReturnType != null && methodMeta.ReturnType.BaseType != typeof(void))
                            retVal = blockBuilder.Pop();
                        blockBuilder.NewLine(
                            new IlReturnStmt(retVal)
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
                        IlExpr rhs = blockBuilder.Pop();
                        IlExpr lhs = blockBuilder.Pop();
                        IlBinaryOperation op = new IlBinaryOperation(lhs, rhs);
                        blockBuilder.Push(op);
                        break;
                    }
                    case "neg":
                    case "not":
                    {
                        IlExpr operand = blockBuilder.Pop();
                        IlUnaryOperation op = new IlUnaryOperation(operand);
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
                        IlExpr lhs = blockBuilder.Pop();
                        IlExpr rhs = blockBuilder.Pop();
                        ILInstr tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                        ILInstr fb = blockBuilder.CurInstr.next;
                        blockBuilder.NewLine(new IlIfStmt(
                            new IlBinaryOperation(lhs, rhs),
                            tb.idx));
                        // frame.ContinueBranchingTo(fb, tb);
                        return true;
                    }
                    case "brinst":
                    case "brinst.s":
                    case "brtrue.s":
                    case "brtrue":
                    {
                        IlExpr rhs = new IlBoolConst(true);
                        IlExpr lhs = blockBuilder.Pop();
                        ILInstr tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                        ILInstr fb = blockBuilder.CurInstr.next;
                        blockBuilder.NewLine(new IlIfStmt(
                            new IlBinaryOperation(lhs, rhs), tb.idx));
                        return true;
                    }
                    case "brnull":
                    case "brnull.s":
                    case "brzero":
                    case "brzero.s":
                    case "brfalse.s":
                    case "brfalse":
                    {
                        IlExpr rhs = new IlBoolConst(false);
                        IlExpr lhs = blockBuilder.Pop();
                        ILInstr tb = ((ILInstrOperand.Target)blockBuilder.CurInstr.arg).value;
                        ILInstr fb = blockBuilder.CurInstr.next;
                        blockBuilder.NewLine(new IlIfStmt(
                            new IlBinaryOperation(lhs, rhs), tb.idx));
                        return true;
                    }
                    case "newobj":
                    {
                        IlMethod ilMethod = ((ILInstrOperand.ResolvedMethod)blockBuilder.CurInstr.arg).value;
                        // ctor does not have return type
                        Debug.Assert(ilMethod.ReturnType is null);
                        int arity = ilMethod.Parameters.Count - 1; // TODO is hardcode -1 for `this` ok?
                        IlType objIlType = ilMethod.DeclaringType!;
                        IlExpr[] inParams = new IlExpr[arity];
                        for (int i = 0; i < arity; i++)
                        {
                            inParams[i] = blockBuilder.Pop();
                        }

                        blockBuilder.Push(new IlNewExpr(objIlType, inParams));
                        break;
                    }
                    case "newarr":
                    {
                        IlType arrIlType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        IlExpr sizeExpr = blockBuilder.Pop();
                        if (!Equals(sizeExpr.Type, IlInstanceBuilder.GetType(typeof(int))) &&
                            !Equals(sizeExpr.Type, IlInstanceBuilder.GetType(typeof(nint))))
                        {
                            throw new Exception("expected arr size of type int32 or native int, got " +
                                                sizeExpr.Type);
                        }

                        IlExpr arrExpr = new IlNewArrayExpr(
                            arrIlType,
                            sizeExpr);
                        IlValue arrTemp = blockBuilder.GetNewTemp(arrExpr, blockBuilder.CurInstr.idx);
                        blockBuilder.NewLine(new ILAssignStmt(
                            arrTemp,
                            arrExpr
                        ));
                        blockBuilder.Push(arrTemp);
                        break;
                    }
                    case "initobj":
                    {
                        IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        IlExpr addr = blockBuilder.Pop();
                        // Type ilType = TypingUtil.ILTypeFrom(typeMeta.Type);
                        blockBuilder.NewLine(new ILAssignStmt(PointerExprTypeResolver.DerefAs(addr, ilType),
                            new IlInitExpr(ilType)));
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
                        IlExpr index = blockBuilder.Pop();
                        IlExpr arr = blockBuilder.Pop();
                        blockBuilder.Push(new IlArrayAccess(arr, index));
                        break;
                    }
                    case "ldelema":
                    {
                        IlExpr idx = blockBuilder.Pop();
                        IlExpr arr = blockBuilder.Pop();
                        blockBuilder.Push(new IlManagedRef(new IlArrayAccess(arr, idx)));
                        break;
                    }
                    case "ldlen":
                    {
                        IlExpr arr = blockBuilder.Pop();
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
                        IlExpr value = blockBuilder.Pop();
                        IlExpr index = blockBuilder.Pop();
                        IlExpr arr = blockBuilder.Pop();
                        blockBuilder.NewLine(new ILAssignStmt(new IlArrayAccess(arr, index), value));
                        break;
                    }
                    case "conv.i1":
                    case "conv.i2":
                    case "conv.i4":
                    {
                        IlExpr value = blockBuilder.Pop();
                        IlConvExpr conv = new IlConvExpr(IlInstanceBuilder.GetType(typeof(int)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.i8":
                    {
                        IlExpr value = blockBuilder.Pop();
                        IlConvExpr conv = new IlConvExpr(IlInstanceBuilder.GetType(typeof(long)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.r4":
                    {
                        IlExpr value = blockBuilder.Pop();
                        IlConvExpr conv = new IlConvExpr(IlInstanceBuilder.GetType(typeof(float)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.r8":
                    {
                        IlExpr value = blockBuilder.Pop();
                        IlConvExpr conv = new IlConvExpr(IlInstanceBuilder.GetType(typeof(double)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.u1":
                    case "conv.u2":
                    case "conv.u4":
                    {
                        IlExpr value = blockBuilder.Pop();
                        IlConvExpr conv = new IlConvExpr(IlInstanceBuilder.GetType(typeof(uint)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.u8":
                    {
                        IlExpr value = blockBuilder.Pop();
                        IlConvExpr conv = new IlConvExpr(IlInstanceBuilder.GetType(typeof(ulong)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.i":
                    {
                        IlExpr value = blockBuilder.Pop();
                        IlConvExpr conv = new IlConvExpr(IlInstanceBuilder.GetType(typeof(nint)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.u":
                    {
                        IlExpr value = blockBuilder.Pop();
                        IlConvExpr conv = new IlConvExpr(IlInstanceBuilder.GetType(typeof(nuint)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "conv.r.un":
                    {
                        IlExpr value = blockBuilder.Pop();
                        IlConvExpr conv = new IlConvExpr(IlInstanceBuilder.GetType(typeof(NFloat)), value);
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
                        IlExpr value = blockBuilder.Pop();
                        IlConvExpr conv = new IlConvExpr(IlInstanceBuilder.GetType(typeof(nint)), value);
                        blockBuilder.Push(conv);
                        break;
                    }
                    case "isinst":
                    {
                        IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        IlExpr obj = blockBuilder.Pop();
                        IlExpr res = new IlIsInstExpr(ilType, obj);
                        blockBuilder.Push(res);
                        break;
                    }
                    case "castclass":
                    {
                        IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        IlExpr value = blockBuilder.Pop();
                        IlExpr casted = new IlCastClassExpr(ilType, value);
                        blockBuilder.Push(casted);
                        break;
                    }
                    case "box":
                    {
                        // TODO use typeMeta
                        IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        IlValue value = (IlValue)blockBuilder.Pop();
                        IlExpr boxed = new IlBoxExpr(value);
                        blockBuilder.Push(boxed);
                        break;
                    }
                    case "unbox":
                    case "unbox.any":
                    {
                        IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                        IlValue obj = (IlValue)blockBuilder.Pop();
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

        private static void InlineInitArray(this BlockTacBuilder blockBuilder, List<IlExpr> args)
        {
            if (args.First() is IlValue newArr && args.Last() is IlFieldRef fieldRef)
            {
                try
                {
                    IlNewArrayExpr expr = (newArr as IlTempVar)?.Value as IlNewArrayExpr ??
                                          throw new Exception("expected temp var");
                    IlIntConst arrSize = (IlIntConst)expr.Size;
                    Type arrType = expr.Type.BaseType;
                    var tmp = Array.CreateInstance(arrType, arrSize.Value);
                    GCHandle handle = GCHandle.Alloc(tmp, GCHandleType.Pinned);
                    try
                    {
                        Marshal.StructureToPtr(fieldRef.Field.GetValue(null)!, handle.AddrOfPinnedObject(), false);
                    }
                    finally
                    {
                        handle.Free();
                    }

                    List<object> list = [.. tmp];
                    for (int i = 0; i < list.Count; i++)
                    {
                        blockBuilder.NewLine(new ILAssignStmt(
                            new IlArrayAccess(newArr, new IlIntConst(i)),
                            GetConstant(list[i])
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

        private static IlConstant GetConstant(object value)
        {
            if (value is byte bt) return new IlByteConst(bt);
            if (value is int i) return new IlIntConst(i);
            if (value is long l) return new IlLongConst(l);
            if (value is float f) return new IlFloatConst(f);
            if (value is double d) return new IlDoubleConst(d);
            if (value is bool b) return new IlBoolConst(b);
            throw new Exception($"bad constant type {value}");
        }
    }
}
