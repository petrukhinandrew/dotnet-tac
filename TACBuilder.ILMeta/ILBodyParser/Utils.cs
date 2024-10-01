using System.Reflection;
using System.Reflection.Emit;

namespace TACBuilder.ILMeta.ILBodyParser;

public static class ReflectionUtils
{
    public static IEnumerable<Type> GetTypesChecked(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t is not null)!;
        }
    }
}

static class OpCodeOp
{
    private static OpCode[] singleByteOpCodes = new OpCode[0x100];
    private static OpCode[] twoBytesOpCodes = new OpCode[0x100];

    static OpCodeOp()
    {
        Load();
    }

    public static void Load()
    {
        foreach (var field in typeof(OpCodes).GetRuntimeFields())
        {
            if (field.GetValue(null) is OpCode opCode)
            {
                short value = opCode.Value;
                if (opCode.Size == 1)
                {
                    singleByteOpCodes[value] = opCode;
                }
                else
                {
                    twoBytesOpCodes[value & 0xFF] = opCode;
                }
            }
        }
    }

    private static bool isSingleByteOpCode(byte code)
    {
        return OpCodes.Prefix1.Value != code;
    }


    public static (OpCode, int) GetOpCode(byte[] src, int offset)
    {
        byte msb = src[offset];
        if (isSingleByteOpCode(msb))
        {
            return (singleByteOpCodes[msb], 1);
        }

        if (src.Length < offset + 1) throw new Exception("Prefix instruction FE without suffix!");
        int lsb = src[offset + 1];
        return (twoBytesOpCodes[lsb & 0xFF], 2);
    }
}
