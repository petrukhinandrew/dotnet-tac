using System.Reflection.Emit;

namespace Usvm.IL.Parser;

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
    public record
    Instr(OpCode opCode, int offset) : ILInstr
    {
        public override string ToString()
        {
            return opCode.ToString() ?? "null opcode";
        }
    }
    public record SwitchArg() : ILInstr
    {
        public override string ToString()
        {
            return "SwitchArg";
        }
    }
    public record Back() : ILInstr
    {
        public override string ToString()
        {
            return "Back";
        }
    }
    public override int GetHashCode()
    {
        return idx;
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