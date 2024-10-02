using System.Net.Http.Headers;
using System.Reflection.Emit;
using Microsoft.Win32.SafeHandles;

namespace TACBuilder.ILMeta.ILBodyParser;

public abstract class ILInstr
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

    public bool IsJump() => this is SwitchArg || this is Instr
    {
        opCode.OperandType: OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget
    };

    public bool IsUncondJump() => this is Instr instr && instr.opCode.FlowControl == FlowControl.Cond_Branch;

    public bool IsControlFlowInterruptor() => IsJump() || this is Instr
    {
        opCode.FlowControl: FlowControl.Throw or FlowControl.Return
    } || next is Back;

    public static void InsertBefore(ILInstr where, ILInstr what)
    {
        what.next = where;
        what.prev = where.prev;
        what.next.prev = what;
        what.prev.next = what;
        what.idx = what.prev.idx + 1;
    }

    public sealed class Instr(OpCode op, int offset) : ILInstr
    {
        public OpCode opCode = op;

        public override string ToString()
        {
            return opCode.ToString() ?? "null opcode";
        }
    }

    public sealed class SwitchArg : ILInstr
    {
        public override string ToString()
        {
            return "SwitchArg";
        }
    }

    public sealed class Back : ILInstr
    {
        public override string ToString()
        {
            return "Back";
        }
    }

    public virtual bool Equals(ILInstr? other)
    {
        return other != null && idx.Equals(other.idx);
    }

    public override int GetHashCode()
    {
        return idx;
    }
}

public abstract record ILInstrOperand
{
    public record NoArg : ILInstrOperand;

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

    public record ResolvedString(string value) : ILInstrOperand;

    public record ResolvedField(FieldMeta value) : ILInstrOperand;

    public record ResolvedType(TypeMeta value) : ILInstrOperand;

    public record ResolvedSignature(byte[] value) : ILInstrOperand;

    public record ResolvedMethod(MethodMeta value) : ILInstrOperand;

    public record ResolvedMember(MemberMeta value) : ILInstrOperand;
}
