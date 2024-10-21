using System.Reflection.Emit;
using TACBuilder.ILReflection;

namespace TACBuilder.BodyBuilder;

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

    public bool IsJump => this is SwitchArg || this is Instr
    {
        opCode.OperandType: OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget
    };

    public bool IsCondJump => this is SwitchArg || this is Instr { opCode.FlowControl: FlowControl.Cond_Branch };

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

    public sealed class SwitchArg(int value) : ILInstr
    {
        public int Value => value;
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

    public record ResolvedField(ILField value) : ILInstrOperand;

    public record ResolvedType(ILType value) : ILInstrOperand;

    public record ResolvedSignature(byte[] value) : ILInstrOperand;

    public record ResolvedMethod(ILMethod value) : ILInstrOperand;

    public record ResolvedMember(ILMember value) : ILInstrOperand;
}
