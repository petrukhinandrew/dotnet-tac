using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace TACBuilder.ILMeta.ILBodyParser;

public class ILBodyParser(MethodBase methodBase)
{
    private MethodBase _methodBase = methodBase;
    private MethodBody _methodBody = methodBase.GetMethodBody()!;

    private Module _module = methodBase.Module;
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
    public List<ehClause> EhClauses => _ehs.ToList();

    private void ImportEH()
    {
        var clauses = _methodBody.ExceptionHandlingClauses
            .Select(ehc => new exceptionHandlingClause(ehc)).ToArray();
        _ehs = clauses.Select(ParseEh).ToArray();
        return;

        ehClause ParseEh(exceptionHandlingClause c)
        {
            ILInstr tryBegin = _offsetToInstr[c.tryOffset];

            // TODO check
            int te = c.tryOffset + c.tryLength - 1;
            while (_offsetToInstr[te] is null)
            {
                te--;
            }

            Debug.Assert(_offsetToInstr[te] is not null);
            ILInstr tryEnd = _offsetToInstr[te];

            ILInstr handlerBegin = _offsetToInstr[c.handlerOffset];
            Debug.Assert(handlerBegin is not null);
            int he = c.handlerOffset + c.handlerLength - 1;
            while (_offsetToInstr[he] is null)
            {
                he--;
            }

            Debug.Assert(_offsetToInstr[he] is not null);
            ILInstr handlerEnd = _offsetToInstr[he];
            int fd = 0;
            if (c.type is ehcType.Filter filt)
            {
                fd = filt.offset;
                while (_offsetToInstr[fd] is null) fd--;
            }

            rewriterEhcType type = c.type switch
            {
                ehcType.Filter f => new rewriterEhcType.FilterEH(_offsetToInstr[fd]),
                ehcType.Catch ct => new rewriterEhcType.CatchEH(ct.type),
                ehcType.Finally => new rewriterEhcType.FinallyEH(),
                ehcType.Fault => new rewriterEhcType.FaultEH(),
                _ => throw new Exception("unexpected ehcType")
            };

            return new ehClause(tryBegin, tryEnd, handlerBegin, handlerEnd, type);
        }
    }

    private void ImportIL()
    {
        _il = _methodBody.GetILAsByteArray() ?? [];
        _offsetToInstr = new ILInstr[_il.Length + 1];

        _back.next = _back;
        _back.prev = _back;
        _offsetToInstr[_il.Length] = _back;
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
            ILInstr.InsertBefore(_back, new ILInstr.Instr(op, opOffset));
            _offsetToInstr[opOffset] = _back.prev;
            ILInstr instr = _offsetToInstr[opOffset];
            Debug.Assert(_back.prev == instr);
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

                case OperandType.InlineMethod:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedMethod(
                        MetaBuilder.GetMethod(_methodBase, token)
                    );
                    break;
                }
                case OperandType.InlineType:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedType(MetaBuilder.GetType(_methodBase, token));
                    break;
                }
                case OperandType.InlineString:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedString(MetaBuilder.GetString(_methodBase, token).Value);
                    break;
                }
                case OperandType.InlineSig:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedSignature(
                        MetaBuilder.GetSignature(_methodBase, token).Value);
                    break;
                }
                case OperandType.InlineTok:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedMember(
                        MetaBuilder.GetMember(_methodBase, token));
                    break;
                }
                case OperandType.InlineField:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedField(
                        MetaBuilder.GetField(_methodBase, token));
                    break;
                }

                case OperandType.InlineI:
                case OperandType.ShortInlineR:
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
                {
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

                        ILInstr instrArg = new ILInstr.SwitchArg(i)
                        {
                            arg = new ILInstrOperand.Arg32(BitConverter.ToInt32(_il, offset) + baseOffset)
                        };
                        offset += sizeOfInt;

                        ILInstr.InsertBefore(_back, instrArg);
                    }

                    branch = true;
                    break;
                }
                default:
                    throw new Exception("Unexpected operand type!");
            }

            Debug.Assert(instr is not null);
            offset += size;
        }

        if (offset != _il.Length)
        {
            throw new Exception("offset != il.Length");
        }

        Debug.Assert(ILInstrs().All(i => i is not null));
        if (branch)
        {
            foreach (var cur in ILInstrs())
            {
                if (!cur.IsJump()) continue;
                if (cur.arg is ILInstrOperand.Arg32 a32)
                {
                    if (_offsetToInstr[a32.value] is null)
                    {
                        Debug.Assert(false);
                    }

                    cur.arg = new ILInstrOperand.Target(_offsetToInstr[a32.value]);
                }
                else
                {
                    throw new Exception("Wrong operand of branching instruction!");
                }
            }
        }
    }

    public IEnumerable<ILInstr> ILInstrs()
    {
        ILInstr cur = _back.next;
        while (cur != _back)
        {
            yield return cur;
            cur = cur.next;
        }
    }
}
