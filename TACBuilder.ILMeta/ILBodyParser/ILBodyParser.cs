using System.Reflection;
using System.Reflection.Emit;

namespace TACBuilder.ILMeta.ILBodyParser;

public class ILBodyParser(MethodBody methodBody)
{
    private MethodBody _methodBody = methodBody;
    private byte[] _il = [];
    private ILInstr[] _offsetToInstr = [];
    private ILInstr _back = new ILInstr.Back();
    private ehClause[] _ehs = [];

    public void Parse()
    {
        ImportIL();
        ImportEH();
    }

    public ILInstr Instructions => _back.next;
    public int InstructionsCount => ILInstrs().Count() - 1;
    public List<ehClause> EhClauses => _ehs.ToList();

    private void ImportEH()
    {
        var clauses = _methodBody.ExceptionHandlingClauses
            .Select(ehc => new exceptionHandlingClause(ehc)).ToArray();
        _ehs = clauses.Select(ParseEh).ToArray();
        return;

        ehClause ParseEh(exceptionHandlingClause c)
        {
            ILInstr tryBegin = _offsetToInstr![c.tryOffset];
            ILInstr tryEnd = _offsetToInstr[c.tryOffset + c.tryLength].prev; // - //-
            ILInstr handlerBegin = _offsetToInstr[c.handlerOffset];
            ILInstr handlerEnd = _offsetToInstr[c.handlerOffset + c.handlerLength].prev; // take closest not null
            rewriterEhcType type = c.type switch
            {
                ehcType.Filter f => new rewriterEhcType.FilterEH(_offsetToInstr[f.offset]),
                ehcType.Catch ct => type = new rewriterEhcType.CatchEH(ct.type),
                ehcType.Finally => type = new rewriterEhcType.FinallyEH(),
                ehcType.Fault => type = new rewriterEhcType.FaultEH(),
                _ => throw new Exception("unexpected ehcType")
            };
            return new ehClause(tryBegin, tryEnd, handlerBegin, handlerEnd, type);
        }
    }

    private void ImportIL()
    {
        _il = _methodBody.GetILAsByteArray() ?? [];
        _offsetToInstr = new ILInstr[_il.Length + 1];
        int offset = 0;
        bool branch = false;
        while (offset < _il.Length)
        {
            int opOffset = offset;
            (OpCode op, int d) = OpCodeOp.GetOpCode(_il, offset);
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
            if (offset + size > _il.Length) throw new Exception("IL stream unexpectedly ended!");
            ILInstr instr = new ILInstr.Instr(op, opOffset);
            _offsetToInstr[opOffset] = instr;
            ILInstr.InsertBefore(_back, instr);
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
                    instr.arg = new ILInstrOperand.Arg8(_il[offset]);
                    break;
                }
                case OperandType.InlineVar:
                {
                    instr.arg = new ILInstrOperand.Arg16(BitConverter.ToInt16(_il, offset));
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
                    instr.arg = new ILInstrOperand.Arg32(BitConverter.ToInt32(_il, offset));
                    break;
                }
                case OperandType.InlineI8:
                case OperandType.InlineR:
                {
                    instr.arg = new ILInstrOperand.Arg64(BitConverter.ToInt64(_il, offset));
                    break;
                }
                case OperandType.ShortInlineBrTarget:
                {
                    int delta = Convert.ToInt32((sbyte)_il[offset]);

                    instr.arg = new ILInstrOperand.Arg32(offset + delta + sizeof(sbyte));
                    branch = true;
                    break;
                }
                case OperandType.InlineBrTarget:
                {
                    int delta = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.Arg32(delta + sizeof(int) + offset);
                    branch = true;
                    break;
                }
                case OperandType.InlineSwitch:
                    int sizeOfInt = sizeof(int);
                    if (offset + sizeOfInt > _il.Length)
                    {
                        throw new Exception("IL stream unexpectedly ended!");
                    }

                    int targetCnt = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.Arg32(targetCnt);
                    offset += sizeOfInt;
                    int baseOffset = offset + targetCnt * sizeOfInt;
                    for (int i = 0; i < targetCnt; i++)
                    {
                        if (offset + sizeOfInt > _il.Length)
                        {
                            throw new Exception("IL stream unexpectedly ended!");
                        }

                        ILInstr instrArg = new ILInstr.SwitchArg
                        {
                            arg = new ILInstrOperand.Arg32(BitConverter.ToInt32(_il, offset) + baseOffset)
                        };
                        offset += sizeOfInt;

                        ILInstr.InsertBefore(_back, instrArg);
                    }

                    branch = true;
                    break;
                default:
                    throw new Exception("Unexpected operand type!");
            }

            offset += size;
        }

        if (offset != _il.Length)
        {
            throw new Exception("offset != il.Length");
        }

        if (branch)
        {
            _back = ILInstrs().Select(cur =>
            {
                if (cur.IsJump())
                {
                    if (cur.arg is ILInstrOperand.Arg32 a32)
                    {
                        cur.arg = new ILInstrOperand.Target(_offsetToInstr[a32.value]);
                    }
                    else
                    {
                        throw new Exception("Wrong operand of branching instruction!");
                    }
                }

                return cur;
            }).Last().next;
        }
    }

    private IEnumerable<ILInstr> ILInstrs()
    {
        ILInstr cur = _back.next;
        while (cur != _back)
        {
            yield return cur;
            cur = cur.next;
        }
    }
}