using System.Reflection;
using System.Reflection.Emit;

namespace Usvm.IL.Parser;
class ILRewriter
{
    private Module _module;
    public ILRewriter(Module mod)
    {
        _module = mod;
    }
    int offset = 0;
    byte[]? il;
    ILInstr[]? offsetToInstr;
    bool branch = false;
    ILInstr back = new ILInstr.Back();
    ehClause[] ehs = [];
    public void ImportEH(MethodBody methodBody)
    {
        ehClause parseEH(exceptionHandlingClause c)
        {
            ILInstr tryBegin = offsetToInstr![c.tryOffset];
            ILInstr tryEnd = offsetToInstr[c.tryOffset + c.tryLength].prev;
            ILInstr handlerBegin = offsetToInstr[c.handlerOffset];
            ILInstr handlerEnd = offsetToInstr[c.handlerOffset + c.handlerLength].prev;
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
        Console.WriteLine("found {0} ehcs", clauses.Length);
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
        Console.WriteLine("Importing IL with size of {0}", il.Length);
        offsetToInstr = new ILInstr[il.Length + 1];
        foreach (var v in methodBody.LocalVariables)
        {
            Console.WriteLine("Local {0}", v.ToString());
        }

        while (offset < il.Length)
        {
            int opOffset = offset;
            (OpCode op, int delta) = OpCodeOp.GetOpCode(il, offset);
            offset += delta;
            int size;
            switch (op.OperandType)
            {
                case OperandType.InlineNone:
                case OperandType.InlineSwitch: size = 0; break;
                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineBrTarget: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineString:
                case OperandType.InlineSig:
                case OperandType.InlineTok:
                case OperandType.ShortInlineR:
                case OperandType.InlineField:
                case OperandType.InlineBrTarget: size = 4; break;
                case OperandType.InlineI8:
                case OperandType.InlineR: size = 8; break;
                default:
                    string exc = op.OperandType.ToString();
                    throw new Exception("unreachable " + exc);

            };
            if (offset + size > il.Length) throw new Exception("IL stream unexpectedly ended!");
            ILInstr instr = new ILInstr.Instr(op, opOffset);
            offsetToInstr[opOffset] = instr;
            ILInstr.InsertBefore(back, instr);
            switch (op.OperandType)
            {
                case OperandType.InlineNone:
                    instr.arg = new ILInstrOperand.NoArg();
                    break;
                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineI:
                    instr.arg = new ILInstrOperand.Arg8(il[offset]);
                    break;
                case OperandType.InlineVar:
                    instr.arg = new ILInstrOperand.Arg16(BitConverter.ToInt16(il, offset));
                    break;

                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineString:
                case OperandType.InlineSig:
                case OperandType.InlineTok:
                case OperandType.ShortInlineR:
                case OperandType.InlineField:
                    instr.arg = new ILInstrOperand.Arg32(BitConverter.ToInt32(il, offset));
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    instr.arg = new ILInstrOperand.Arg64(BitConverter.ToInt64(il, offset));
                    break;
                case OperandType.ShortInlineBrTarget:
                    instr.arg = new ILInstrOperand.Arg32(il[offset] + offset + sizeof(byte));
                    branch = true;
                    break;
                case OperandType.InlineBrTarget:
                    int d = BitConverter.ToInt32(il, offset);
                    instr.arg = new ILInstrOperand.Arg32(d + sizeof(int) + offset);
                    branch = true;
                    break;
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
            Console.WriteLine("IL_{0} {1} {2}", instr.idx, instr.ToString(), instr.arg.ToString());
            if (instr is ILInstr.Instr ilinstr)
            {
                ILInstrOperand.Arg32 arg;
                switch (ilinstr.opCode.Name)
                {
                    case "newarr":
                        arg = (ILInstrOperand.Arg32)ilinstr.arg;
                        tryResolveType(arg.value);
                        break;
                    case "ldtoken":
                        arg = (ILInstrOperand.Arg32)ilinstr.arg;
                        tryResolveToken(arg.value);
                        break;
                    case "newobj":
                    case "jmp":
                    case "ldvirtftn":
                    case "ldftn":
                    case "call":
                    case "callvirt":
                        arg = (ILInstrOperand.Arg32)ilinstr.arg;
                        tryResolveMethod(arg.value);
                        break;
                    case "ldstr":
                        arg = (ILInstrOperand.Arg32)ilinstr.arg;
                        tryResolveString(arg.value);
                        break;
                    case "stsfld":
                    case "stfld":
                    case "ldsflda":
                    case "ldsfld":
                    case "ldfld":
                    case "ldflda":
                        arg = (ILInstrOperand.Arg32)ilinstr.arg;
                        tryResolveField(arg.value);
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
            Type t = _module.ResolveType(arg);
            Console.WriteLine(" ∟--resolved {0}", t);
        }
        catch (Exception e)
        {
            Console.WriteLine("error resolving {0}", e.Message);
        }
    }
    private void tryResolveMethod(int arg)
    {
        try
        {
            MethodBase? mb = _module.ResolveMethod(arg);
            if (mb != null)
            {
                Console.WriteLine(" ∟--resolved {1} {0}", mb.Name, mb.DeclaringType);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("error resolving {0}", e.Message);
        }
    }
    private void tryResolveField(int arg)
    {
        try
        {
            FieldInfo? fi = _module.ResolveField(arg);
            if (fi != null)
            {
                Console.WriteLine(" ∟--resolved {1} {0}", fi.Name, fi.DeclaringType);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("error resolving {0}", e.Message);
        }
    }
    private void tryResolveString(int arg)
    {
        try
        {
            string res = _module.ResolveString(arg);
            Console.WriteLine(" ∟--resolved `{0}`", res);
        }
        catch (Exception e)
        {
            Console.WriteLine("error resolving {0}", e.Message);
        }
    }
    private void tryResolveToken(int arg)
    {
        try
        {
            MemberInfo? res = _module.ResolveMember(arg);
            if (res == null) return;
            Console.WriteLine(" ∟--resolved `{0}`", res);
        }
        catch (Exception e)
        {
            Console.WriteLine("error resolving {0}", e.Message);
        }
    }

    public IEnumerable<ILInstr> ILInstrs()
    {

        ILInstr cur = back.next;
        while (cur != back)
        {
            yield return cur;
            cur = cur.next;
        }
    }
}

public abstract record ILInstr
{
    public ILInstrOperand arg = new ILInstrOperand.NoArg();
    public int idx;
    public ILInstr next;
    public ILInstr prev;

    ILInstr()
    {
        next = this;
        prev = this;
        idx = 0;
    }
    public bool isJump()
    {
        if (this is SwitchArg)
        {
            return true;
        }
        if (this is Instr instr)
        {
            switch (instr.opCode.OperandType)
            {
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget: return true;
                default: return false;
            }
        }
        return false;
    }
    public static void InsertBefore(ILInstr where, ILInstr what)
    {
        what.next = where;
        what.prev = where.prev;
        what.next.prev = what;
        what.prev.next = what;
        what.idx = what.prev.idx + 1;
    }
    public record Instr(OpCode opCode, int offset) : ILInstr
    {
        public override string ToString()
        {
            return idx.ToString() + " " + opCode.ToString() ?? "null opcode";
        }
    }
    public record SwitchArg() : ILInstr
    {
        public override string ToString()
        {
            return idx.ToString() + " SwitchArg";
        }
    }
    public record Back() : ILInstr
    {
        public override string ToString()
        {
            return idx.ToString() + " Back";
        }
    }

}

public abstract record ILInstrOperand
{
    public record NoArg() : ILInstrOperand;
    public record Arg8(byte value) : ILInstrOperand;
    public record Arg16(short value) : ILInstrOperand;
    public record Arg32(int value) : ILInstrOperand;
    public record Arg64(long value) : ILInstrOperand;
    public record Target(ILInstr value) : ILInstrOperand
    {
        public override string ToString()
        {
            return base.ToString() + " to IL_" + value.idx;
        }
    }

}