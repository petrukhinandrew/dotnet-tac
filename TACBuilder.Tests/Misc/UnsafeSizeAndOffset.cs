using System.Runtime.InteropServices;
using TACBuilder.ILReflection;

namespace TACBuilder.Tests.Misc;

public class UnsafeSizeAndOffset
{
    [Fact]
    public void DefaultSizesCheck()
    {
        var intSize = Marshal.SizeOf(typeof(int)); //IlInstanceBuilder.GetType(typeof(int)).Size;
        var doubleSize = Marshal.SizeOf(typeof(double));//IlInstanceBuilder.GetType(typeof(double)).Size;
        var objSize = Marshal.SizeOf(typeof(DefaultPackStructure));//;IlInstanceBuilder.GetType(typeof(object)).Size;
        var voidSize = Marshal.SizeOf(typeof(void));
        IlInstanceBuilder.Construct();
        Assert.Equal(4, intSize);
        Assert.Equal(8, doubleSize);
        Assert.Equal(1, voidSize);
        Assert.True(objSize is >= 11 and <= 16);
    }

    struct DefaultPackStructure
    {
        public int V1;
        public int V2;
        public byte B1;
        public byte B2;
        public byte B3;
    }

    [Fact]
    public void StructPackingCheck()
    {
        var structSize = IlInstanceBuilder.GetType(typeof(DefaultPackStructure)).Size;
        IlInstanceBuilder.Construct();
        Assert.Equal(24, structSize);
    }
}