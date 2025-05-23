using System.Reflection.Emit;
using TACBuilder.ILReflection;

namespace TACBuilder.BodyBuilder.ILBodyParser;

public abstract class IlInstr
{
    public ILInstrOperand arg = new ILInstrOperand.NoArg();
    public int idx;
    public IlInstr next;
    public IlInstr prev;

    IlInstr()
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

    public static void InsertBefore(IlInstr where, IlInstr what)
    {
        what.next = where;
        what.prev = where.prev;
        what.next.prev = what;
        what.prev.next = what;
        what.idx = what.prev.idx + 1;
    }

    public sealed class Instr(OpCode op, int offset) : IlInstr
    {
        public OpCode opCode = op;

        public override string ToString()
        {
            return opCode.ToString() ?? "null opcode";
        }
    }

    public sealed class SwitchArg(int value) : IlInstr
    {
        public int Value => value;

        public override string ToString()
        {
            return "SwitchArg";
        }
    }

    public sealed class Back : IlInstr
    {
        public override string ToString()
        {
            return "Back";
        }
    }

    public virtual bool Equals(IlInstr? other)
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

    public record Target(IlInstr value) : ILInstrOperand
    {
        public override string ToString()
        {
            return base.ToString() + " to IL_" + value.idx;
        }
    }

    public record ResolvedString(IlString value) : ILInstrOperand;

    public record ResolvedField(IlField value) : ILInstrOperand;

    public record ResolvedType(IlType value) : ILInstrOperand;

    public record ResolvedSignature(IlSignature value) : ILInstrOperand;

    public record ResolvedMethod(IlMethod value) : ILInstrOperand;

    public record ResolvedMember(IlMember value) : ILInstrOperand;
}
