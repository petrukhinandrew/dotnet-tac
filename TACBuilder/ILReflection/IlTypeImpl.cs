using System.Diagnostics;
using TACBuilder.Exprs;

namespace TACBuilder.ILReflection;

public class IlPointerType(Type targetType, bool isUnmanaged = true) : IlType(targetType)
{
    public override bool IsManaged => !isUnmanaged;
    public override bool IsUnmanaged => isUnmanaged;
    public IlType TargetType => IlInstanceBuilder.GetType(Type);
    public override string FullName => (isUnmanaged ? "*" : "&") +  base.FullName;
}

public class IlValueType(Type type) : IlType(type);

public class IlReferenceType(Type type) : IlType(type)
{
    public override bool IsManaged => true;
    public override bool IsUnmanaged => false;
}

public class IlPrimitiveType(Type type) : IlValueType(type)
{
    public override bool IsManaged => false;
    public override bool IsUnmanaged => true;

    public override IlPrimitiveType ExpectedStackType()
    {
        if (Type == typeof(IntPtr) || Type == typeof(UIntPtr))
            return (IlPrimitiveType)
                IlInstanceBuilder.GetType(Type);
        return (IlPrimitiveType)(Type.GetTypeCode(Type) switch
        {
            TypeCode.Boolean => IlInstanceBuilder.GetType(typeof(bool)),
            TypeCode.Char => IlInstanceBuilder.GetType(typeof(char)),
            TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 => IlInstanceBuilder.GetType(typeof(int)),
            TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 => IlInstanceBuilder.GetType(typeof(uint)),
            TypeCode.Int64 => IlInstanceBuilder.GetType(typeof(long)),
            TypeCode.UInt64 => IlInstanceBuilder.GetType(typeof(ulong)),
            TypeCode.Single => IlInstanceBuilder.GetType(typeof(float)),
            TypeCode.Double => IlInstanceBuilder.GetType(typeof(double)),
            _ => throw new NotSupportedException("unhandled primitive stack type " + ToString()),
        });
    }
}

public class IlEnumType(Type type) : IlValueType(type)
{
    public override bool IsManaged => false;
    public override bool IsUnmanaged => true;

    // TODO #2 
    public IlType UnderlyingType = type.IsGenericTypeParameter
        ? IlInstanceBuilder.GetType(typeof(object))
        : IlInstanceBuilder.GetType(Enum.GetUnderlyingType(type));

    public Dictionary<string, IlConstant> NameToValueMapping = new();

    public override void Construct()
    {
        Debug.Assert(Type.IsEnum);
        base.Construct();
        if (Type.IsGenericTypeParameter || Type.GetGenericArguments().Any(arg => arg.IsGenericTypeParameter)) return;
        foreach (var value in Enum.GetValues(Type))
        {
            var name = Enum.GetName(Type, value) ?? "";
            NameToValueMapping.TryAdd(name, IlConstant.From(value));
        }
    }
}

// TODO add null value getter for such thing 
public class IlStructType(Type type) : IlValueType(type);

public class IlArrayType(Type type) : IlReferenceType(type)
{
    public IlType ElementType => IlInstanceBuilder.GetType(Type.GetElementType()!);
}

public class IlClassType(Type type) : IlReferenceType(type);