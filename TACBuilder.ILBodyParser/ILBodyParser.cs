using System.Reflection;
using System.Reflection.Emit;
using Usvm.IL.Parser;
using Usvm.IL.Utils;

namespace TACBuilder.ILBodyParser;
// TODO exepcted API is:
// parse(MethodBody) void
// getters for ILInstr list and ehClauses list (now these are ImportIL and ImportEH)

public class ILBodyParser(Module mod)
{
    byte[]? il;
    ILInstr[] offsetToInstr = [];
    ILInstr back = new ILInstr.Back();
    ehClause[] ehs = [];
    public void ImportEH(MethodBody methodBody)
    {
        ehClause parseEH(exceptionHandlingClause c)
        {
            ILInstr tryBegin = offsetToInstr![c.tryOffset];
            ILInstr tryEnd = offsetToInstr[c.tryOffset + c.tryLength].prev; // - //-
            ILInstr handlerBegin = offsetToInstr[c.handlerOffset];
            ILInstr handlerEnd = offsetToInstr[c.handlerOffset + c.handlerLength].prev; // take closest not null
            rewriterEhcType type = c.type switch
            {
                ehcType.Filter f => new rewriterEhcType.FilterEH(offsetToInstr[f.offset]),
                ehcType.Catch ct => type = new rewriterEhcType.CatchEH(ct.type),
                ehcType.Finally => type = new rewriterEhcType.FinallyEH(),
                ehcType.Fault => type = new rewriterEhcType.FaultEH(),
                _ => throw new Exception("unexpected ehcType")
            };
            return new ehClause(tryBegin, tryEnd, handlerBegin, handlerEnd, type);
        }
        exceptionHandlingClause[] clauses = methodBody.ExceptionHandlingClauses.Select(ehc => new exceptionHandlingClause(ehc)).ToArray();
        ehs = clauses.Select(parseEH).ToArray();
    }
    public ILInstr GetBeginning()
    {
        return back.next;
    }
    public ehClause[] GetEHs()
    {
        return ehs;
    }
    public void ImportIL(MethodBody methodBody)
    {
        il = methodBody.GetILAsByteArray() ?? [];
        offsetToInstr = new ILInstr[il.Length + 1];
        int offset = 0;
        bool branch = false;
        while (offset < il.Length)
        {
            int opOffset = offset;
            (OpCode op, int d) = OpCodeOp.GetOpCode(il, offset);
            offset += op.Size;
            int size =
            op.OperandType switch
            {
                OperandType.InlineNone or
                OperandType.InlineSwitch => 0,
                OperandType.ShortInlineVar or
                OperandType.ShortInlineI or
                OperandType.ShortInlineBrTarget => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineMethod or
                OperandType.InlineI or
                OperandType.InlineType or
                OperandType.InlineString or
                OperandType.InlineSig or
                OperandType.InlineTok or
                OperandType.ShortInlineR or
                OperandType.InlineField or
                OperandType.InlineBrTarget => 4,
                OperandType.InlineI8 or
                OperandType.InlineR => 8,
                _ => throw new Exception("unreachable " + op.OperandType.ToString())

            };
            if (offset + size > il.Length) throw new Exception("IL stream unexpectedly ended!");
            ILInstr instr = new ILInstr.Instr(op, opOffset);
            offsetToInstr[opOffset] = instr;
            ILInstr.InsertBefore(back, instr);
            switch (op.OperandType)
            {
                case OperandType.InlineNone:
                    {
                        instr.arg = new ILInstrOperand.NoArg();
                        break;
                    }
                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineI:
                    {
                        instr.arg = new ILInstrOperand.Arg8(il[offset]);
                        break;
                    }
                case OperandType.InlineVar:
                    {
                        instr.arg = new ILInstrOperand.Arg16(BitConverter.ToInt16(il, offset));
                        break;
                    }
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineString:
                case OperandType.InlineSig:
                case OperandType.InlineTok:
                case OperandType.ShortInlineR:
                case OperandType.InlineField:
                    {
                        instr.arg = new ILInstrOperand.Arg32(BitConverter.ToInt32(il, offset));
                        break;
                    }
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    {
                        instr.arg = new ILInstrOperand.Arg64(BitConverter.ToInt64(il, offset));
                        break;
                    }
                case OperandType.ShortInlineBrTarget:
                    {
                        int delta = Convert.ToInt32((sbyte)il[offset]);

                        instr.arg = new ILInstrOperand.Arg32(offset + delta + sizeof(sbyte));
                        branch = true;
                        break;
                    }
                case OperandType.InlineBrTarget:
                    {
                        int delta = BitConverter.ToInt32(il, offset);
                        instr.arg = new ILInstrOperand.Arg32(delta + sizeof(int) + offset);
                        branch = true;
                        break;
                    }
                case OperandType.InlineSwitch:
                    int sizeOfInt = sizeof(int);
                    if (offset + sizeOfInt > il.Length)
                    {
                        throw new Exception("IL stream unexpectedly ended!");
                    }
                    int targetCnt = BitConverter.ToInt32(il, offset);
                    instr.arg = new ILInstrOperand.Arg32(targetCnt);
                    offset += sizeOfInt;
                    int baseOffset = offset + targetCnt * sizeOfInt;
                    for (int i = 0; i < targetCnt; i++)
                    {
                        if (offset + sizeOfInt > il.Length)
                        {
                            throw new Exception("IL stream unexpectedly ended!");
                        }
                        ILInstr instrArg = new ILInstr.SwitchArg
                        {
                            arg = new ILInstrOperand.Arg32(BitConverter.ToInt32(il, offset) + baseOffset)
                        };
                        offset += sizeOfInt;

                        ILInstr.InsertBefore(back, instrArg);
                    }
                    branch = true;
                    break;
                default:
                    throw new Exception("Unexpected operand type!");

            }
            offset += size;
        }
        if (offset != il.Length)
        {
            throw new Exception("offset != il.Length");
        }
        if (branch)
        {
            back = ILInstrs().Select(cur =>
            {
                if (cur.isJump())
                {
                    if (cur.arg is ILInstrOperand.Arg32 a32)
                    {
                        cur.arg = new ILInstrOperand.Target(offsetToInstr[a32.value]);
                    }
                    else
                    {
                        throw new Exception("Wrong operand of branching instruction!");
                    }
                }
                return cur;
            }).Last().next;
        }

        foreach (var instr in ILInstrs())
        {
            if (instr is ILInstr.Instr ilinstr)
            {
                ILInstrOperand.Arg32 arg;
                switch (ilinstr.opCode.Name)
                {
                    case "newarr":
                    case "isinst":
                        arg = (ILInstrOperand.Arg32)ilinstr.arg;
                        // tryResolveType(arg.value);
                        break;
                    case "ldtoken":
                        arg = (ILInstrOperand.Arg32)ilinstr.arg;
                        // tryResolveToken(arg.value);
                        break;
                    case "newobj":
                    case "jmp":
                    case "ldvirtftn":
                    case "ldftn":
                    case "call":
                    case "callvirt":
                        arg = (ILInstrOperand.Arg32)ilinstr.arg;
                        // tryResolveMethod(arg.value);
                        break;
                    case "ldstr":
                        arg = (ILInstrOperand.Arg32)ilinstr.arg;
                        // tryResolveString(arg.value);
                        break;
                    case "stsfld":
                    case "stfld":
                    case "ldsflda":
                    case "ldsfld":
                    case "ldfld":
                    case "ldflda":
                        arg = (ILInstrOperand.Arg32)ilinstr.arg;
                        // tryResolveField(arg.value);
                        break;
                    default: continue;
                }
            }
        }
    }
    private void tryResolveType(int arg)
    {
        try
        {
            Type t = mod.ResolveType(arg);
        }
        catch (Exception e)
        {
            throw new Exception("error resolving type " + e.Message);
        }
    }
    private void tryResolveMethod(int arg)
    {
        try
        {
            MethodBase? mb = mod.ResolveMethod(arg);
            if (mb != null)
            {
            }
        }
        catch (Exception e)
        {
            throw new Exception("error resolving method " + e.Message);
        }
    }
    private void tryResolveField(int arg)
    {
        try
        {
            FieldInfo? fi = mod.ResolveField(arg);
            if (fi != null)
            {
            }
        }
        catch (Exception e)
        {
            throw new Exception("error resolving field " + e.Message);
        }
    }
    private void tryResolveString(int arg)
    {
        try
        {
            string res = mod.ResolveString(arg);
        }
        catch (Exception e)
        {
            throw new Exception("error resolving string " + e.Message);
        }
    }
    private void tryResolveToken(int arg)
    {
        try
        {
            MemberInfo? res = mod.ResolveMember(arg);
        }
        catch (Exception e)
        {
            throw new Exception("error resolving token " + e.Message);
        }
    }

    private IEnumerable<ILInstr> ILInstrs()
    {

        ILInstr cur = back.next;
        while (cur != back)
        {
            yield return cur;
            cur = cur.next;
        }
    }
}
