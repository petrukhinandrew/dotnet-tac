using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using TACBuilder.ILReflection;

namespace TACBuilder.BodyBuilder;

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

        /*
         * Here _offsetToInstr[endIdx].prev works because of IlInstr.Back at the end of list
         * Otherwise any handler with no instruction after it may fail
         */
        ehClause ParseEh(exceptionHandlingClause c)
        {
            ILInstr tryBegin = _offsetToInstr[c.tryOffset];
            Debug.Assert(tryBegin is not null);

            int te = c.tryOffset + c.tryLength;
            Debug.Assert(_offsetToInstr[te].prev is not null);
            ILInstr tryEnd = _offsetToInstr[te].prev;

            ILInstr handlerBegin = _offsetToInstr[c.handlerOffset];
            Debug.Assert(handlerBegin is not null);

            int he = c.handlerOffset + c.handlerLength;
            Debug.Assert(_offsetToInstr[he].prev is not null);
            ILInstr handlerEnd = _offsetToInstr[he].prev;
            Debug.Assert(handlerBegin.idx <= handlerEnd.idx);
            int fd = 0;
            if (c.type is ehcType.Filter filt)
            {
                fd = filt.offset;
                Debug.Assert(_offsetToInstr[fd] is not null);
            }

            if (c.type is ehcType.Catch excType)
            {
                IlInstanceBuilder.GetType(excType.type);
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
                        IlInstanceBuilder.GetMethod(_methodBase, token)
                    );
                    break;
                }
                case OperandType.InlineType:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedType(IlInstanceBuilder.GetType(_methodBase, token));
                    break;
                }
                case OperandType.InlineString:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedString(IlInstanceBuilder.GetString(_methodBase, token));
                    break;
                }
                case OperandType.InlineSig:
                {
                    var token = BitConverter.ToInt32(_il, offset);

                    instr.arg = new ILInstrOperand.ResolvedSignature(
                        IlInstanceBuilder.GetSignature(_methodBase, token));

                    break;
                }
                case OperandType.InlineTok:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedMember(
                        IlInstanceBuilder.GetMember(_methodBase, token));
                    break;
                }
                case OperandType.InlineField:
                {
                    var token = BitConverter.ToInt32(_il, offset);
                    instr.arg = new ILInstrOperand.ResolvedField(
                        IlInstanceBuilder.GetField(_methodBase, token));
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
                if (!cur.IsJump) continue;
                if (cur.arg is ILInstrOperand.Arg32 a32)
                {
                    Debug.Assert(_offsetToInstr[a32.value] != null);

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