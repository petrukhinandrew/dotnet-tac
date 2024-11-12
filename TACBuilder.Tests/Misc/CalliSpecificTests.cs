using System.Reflection.Emit;
using System.CodeDom.Compiler;

namespace TACBuilder.Tests.Misc;

public unsafe class CalliSpecificTests
{
    public double SimpleMethod(int x)
    {
        return x;
    }
    public static void SimpleCalliUsage()
    {

    }
}
