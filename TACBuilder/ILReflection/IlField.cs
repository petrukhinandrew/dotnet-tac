using System.Reflection;

namespace TACBuilder.ILReflection;

public class IlField(FieldInfo fieldInfo) : IlMember(fieldInfo)
{
    // TODO attributes
    private readonly FieldInfo _fieldInfo = fieldInfo;
    public readonly IlType DeclaringType = IlInstanceBuilder.GetType((fieldInfo.ReflectedType ?? fieldInfo.DeclaringType)!);
    public bool IsStatic => _fieldInfo.IsStatic;
    public readonly IlType Type = IlInstanceBuilder.GetType(fieldInfo.FieldType);
    public new string Name => _fieldInfo.Name;
    public int Offset { get; private set; }
    public int ModuleToken => _fieldInfo.Module.MetadataToken;
    public int MetadataToken => _fieldInfo.MetadataToken;
    public object? GetValue(object? value) => _fieldInfo.GetValue(value);
    public List<IlAttribute> Attributes { get; private set; }
    public new bool IsConstructed = false;

    public override void Construct()
    {
        Offset = LayoutUtils.CalculateOffsetOf(_fieldInfo);
        Attributes = _fieldInfo.CustomAttributes.Select(IlInstanceBuilder.GetAttribute).ToList();
        DeclaringType.EnsureFieldAttached(this);
        IsConstructed = true;
    }

    public override string ToString()
    {
        return $"{Type} {Name}";
    }

    public override bool Equals(object? obj)
    {
        return obj is IlField other && other._fieldInfo == _fieldInfo;
    }

    public override int GetHashCode()
    {
        return _fieldInfo.GetHashCode();
    }
}