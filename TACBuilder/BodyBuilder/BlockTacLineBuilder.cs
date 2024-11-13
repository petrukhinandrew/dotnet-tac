using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TACBuilder.BodyBuilder;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;
using TACBuilder.Utils;

namespace TACBuilder;

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
            // TODO check if it is proper
            if (blockBuilder.CurInstr is ILInstr.Back) return true;
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
                    IlExpr value = blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[0],
                        value.WithTypeEnsured(blockBuilder.Locals[0].Type)));
                    blockBuilder.Locals[0].Value = value;
                    break;
                }
                case "stloc.1":
                {
                    IlExpr value = blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[1],
                        value.WithTypeEnsured(blockBuilder.Locals[1].Type)));
                    blockBuilder.Locals[1].Value = value;
                    break;
                }
                case "stloc.2":
                {
                    IlExpr value = blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[2],
                        value.WithTypeEnsured(blockBuilder.Locals[2].Type)));
                    blockBuilder.Locals[2].Value = value;
                    break;
                }
                case "stloc.3":
                {
                    IlExpr value = blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[3],
                        value.WithTypeEnsured(blockBuilder.Locals[3].Type)));
                    blockBuilder.Locals[3].Value = value;
                    break;
                }
                case "stloc.s":
                {
                    int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                    IlExpr value = blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt(blockBuilder.Locals[idx],
                        value.WithTypeEnsured(blockBuilder.Locals[idx].Type)));
                    blockBuilder.Locals[idx].Value = value;
                    break;
                }
                case "starg":
                {
                    int idx = ((ILInstrOperand.Arg16)blockBuilder.CurInstr.arg).value;
                    IlExpr value = blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt((IlLocal)blockBuilder.Params[idx],
                        value.WithTypeEnsured(blockBuilder.Params[idx].Type)));
                    break;
                }
                case "starg.s":
                {
                    int idx = ((ILInstrOperand.Arg8)blockBuilder.CurInstr.arg).value;
                    IlExpr value = blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt((IlLocal)blockBuilder.Params[idx],
                        value.WithTypeEnsured(blockBuilder.Params[idx].Type)));
                    break;
                }
                case "throw":
                {
                    IlExpr obj = blockBuilder.Pop();
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
                    IlExpr value = blockBuilder.Pop();
                    blockBuilder.NewLine(new IlEndFilterStmt(value));
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
                    IlField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                    IlExpr value = blockBuilder.Pop();
                    IlExpr obj = blockBuilder.Pop();
                    IlFieldAccess ilFieldAccess = new IlFieldAccess(ilField, obj);
                    blockBuilder.NewLine(new ILAssignStmt(ilFieldAccess, value.WithTypeEnsured(ilFieldAccess.Type)));
                    break;
                }
                case "stsfld":
                {
                    IlField ilField = ((ILInstrOperand.ResolvedField)blockBuilder.CurInstr.arg).value;
                    IlExpr value = blockBuilder.Pop();
                    IlFieldAccess ilFieldAccess = new IlFieldAccess(ilField);
                    blockBuilder.NewLine(new ILAssignStmt(ilFieldAccess, value.WithTypeEnsured(ilFieldAccess.Type)));
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
                        PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(int)));
                    blockBuilder.Push(deref);
                    break;
                }
                case "ldind.u8":
                case "ldind.i8":
                {
                    IlExpr addr = blockBuilder.Pop();
                    ILDerefExpr deref =
                        PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(long)));
                    blockBuilder.Push(deref);
                    break;
                }
                case "ldind.r4":
                {
                    IlExpr addr = blockBuilder.Pop();
                    ILDerefExpr deref =
                        PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(NFloat)));
                    blockBuilder.Push(deref);
                    break;
                }
                case "ldind.r8":
                {
                    IlExpr addr = blockBuilder.Pop();
                    ILDerefExpr deref =
                        PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(NFloat)));
                    blockBuilder.Push(deref);
                    break;
                }
                case "ldind.i":
                {
                    IlExpr addr = blockBuilder.Pop();
                    ILDerefExpr deref =
                        PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(nint)));
                    blockBuilder.Push(deref);
                    break;
                }

                case "ldind.ref":
                {
                    IlExpr addr = blockBuilder.Pop();
                    ILDerefExpr deref =
                        PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(object)));
                    blockBuilder.Push(deref);
                    break;
                }
                case "ldobj":
                {
                    IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    IlExpr addr = blockBuilder.Pop();
                    ILDerefExpr deref =
                        PointerExprTypeResolver.Deref(addr, ilType);
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
                            PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(int))), val));
                    break;
                }
                case "stind.r4":
                {
                    IlExpr val = blockBuilder.Pop();
                    IlValue addr = (IlValue)blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt(
                        PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(float))),
                        val));
                    break;
                }
                case "stind.r8":
                {
                    var val = blockBuilder.Pop();
                    var addr = blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt(
                        PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(double))),
                        val));
                    break;
                }
                case "stind.i":
                {
                    var val = blockBuilder.Pop();
                    var addr = blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt(
                        PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(nint))),
                        val));
                    break;
                }
                case "stind.ref":
                {
                    IlExpr val = blockBuilder.Pop();
                    IlValue addr = (IlValue)blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt(
                        PointerExprTypeResolver.Deref(addr, IlInstanceBuilder.GetType(typeof(object))),
                        val));
                    break;
                }
                case "stobj":
                {
                    IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    IlExpr val = blockBuilder.Pop();
                    IlValue addr = (IlValue)blockBuilder.Pop();
                    blockBuilder.NewLine(new ILAssignStmt(
                        PointerExprTypeResolver.Deref(addr, ilType),
                        val.WithTypeEnsured(ilType)));
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
                    blockBuilder.Push(
                        new IlStringConst(((ILInstrOperand.ResolvedString)blockBuilder.CurInstr.arg).value.Value));
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
                case "calli":
                {
                    IlSignature sig =
                        ((ILInstrOperand.ResolvedSignature)blockBuilder.CurInstr.arg).value;
                    var ftn = blockBuilder.Pop();
                    var args = new List<IlExpr>();
                    foreach (var paramType in sig.ParameterTypes)
                    {
                        args.Add(blockBuilder.Pop().WithTypeEnsured(paramType));
                    }

                    args.Reverse();

                    var calli = new IlCallIndirect(sig, ftn, args);

                    if (sig.ReturnType != null && !Equals(sig.ReturnType, IlInstanceBuilder.GetType(typeof(void))))
                    {
                        var tmp = blockBuilder.GetNewTemp(calli, blockBuilder.CurInstr.idx);
                        blockBuilder.NewLine(new ILAssignStmt(tmp, calli));
                        blockBuilder.Push(tmp);
                    }
                    else
                        blockBuilder.NewLine(new IlCalliStmt(calli));

                    break;
                }
                case "ret":
                {
                    var methodMeta = blockBuilder.Meta.MethodMeta!;

                    IlExpr? retVal = null;
                    if (methodMeta.ReturnType != null &&
                        !Equals(methodMeta.ReturnType, IlInstanceBuilder.GetType(typeof(void))))
                        retVal = blockBuilder.Pop().WithTypeEnsured(methodMeta.ReturnType);
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
                    if (lhs.Type == null)
                        Console.WriteLine("suka");
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
                    IlExpr lhs = blockBuilder.Pop();
                    IlExpr rhs = TypingUtil.GetBrFalseConst(lhs);
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
                    IlType elemType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    IlArrayType arrType = elemType.MakeArrayType();
                    IlExpr sizeExpr = blockBuilder.Pop();
                    IlExpr arrExpr = new IlNewArrayExpr(
                        arrType,
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
                    blockBuilder.NewLine(new ILAssignStmt(PointerExprTypeResolver.Deref(addr, ilType),
                        new IlInitExpr(ilType)));
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
                    IlConvCastExpr convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(int)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.i8":
                {
                    IlExpr value = blockBuilder.Pop();
                    IlConvCastExpr convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(long)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.r4":
                {
                    IlExpr value = blockBuilder.Pop();
                    IlConvCastExpr convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(float)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.r8":
                {
                    IlExpr value = blockBuilder.Pop();
                    IlConvCastExpr convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(double)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.u1":
                case "conv.u2":
                case "conv.u4":
                {
                    IlExpr value = blockBuilder.Pop();
                    IlConvCastExpr convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(uint)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.u8":
                {
                    IlExpr value = blockBuilder.Pop();
                    IlConvCastExpr convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(ulong)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.i":
                {
                    IlExpr value = blockBuilder.Pop();
                    IlConvCastExpr convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(nint)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.u":
                {
                    IlExpr value = blockBuilder.Pop();
                    IlConvCastExpr convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(nuint)), value);
                    blockBuilder.Push(convCast);
                    break;
                }
                case "conv.r.un":
                {
                    IlExpr value = blockBuilder.Pop();
                    IlConvCastExpr convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(NFloat)), value);
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
                    IlExpr value = blockBuilder.Pop();
                    IlConvCastExpr convCast = new IlConvCastExpr(IlInstanceBuilder.GetType(typeof(nint)), value);
                    blockBuilder.Push(convCast);
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
                    IlExpr casted = new IlConvCastExpr(ilType, value);
                    blockBuilder.Push(casted);
                    break;
                }
                case "box":
                {
                    IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    IlExpr value = blockBuilder.Pop();
                    IlExpr boxed = new IlBoxExpr(ilType, value);
                    blockBuilder.Push(boxed);
                    break;
                }
                case "unbox":
                case "unbox.any":
                {
                    IlType ilType = ((ILInstrOperand.ResolvedType)blockBuilder.CurInstr.arg).value;
                    IlExpr obj = blockBuilder.Pop();
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
    private static void InlineInitArray(this BlockTacBuilder blockBuilder, List<IlExpr> rawArgs)
    {
        var args = rawArgs.ArrayInitCastDiscarded();
        if (args.First() is IlValue newArr && args.Last() is IlFieldRef fieldRef)
        {
            var arrVar = newArr as IlVar ?? throw new Exception("expected var, got " + newArr.Type);
            if (arrVar.Value is IlNewExpr newExpr) throw new KnownBug("inline multidimensional array");
            IlNewArrayExpr expr = arrVar.Value as IlNewArrayExpr ??
                                  throw new Exception("expected NewArray, got " + arrVar.Value!.Type);
            IlIntConst arrSize = (IlIntConst)expr.Size;
            Type elemType = ((IlArrayType)expr.Type).ElementType.Type;
            var tmp = Array.CreateInstance(elemType, arrSize.Value);
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
            var arrConst = new IlArrayConst((IlArrayType)IlInstanceBuilder.GetType(elemType.MakeArrayType()),
                list.Select(obj => TypingUtil.ResolveConstant(obj, elemType.IsEnum ? elemType : null)));
            blockBuilder.NewLine(new ILAssignStmt(newArr, arrConst));
            return;
        }

        throw new Exception("bad array init values");
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
