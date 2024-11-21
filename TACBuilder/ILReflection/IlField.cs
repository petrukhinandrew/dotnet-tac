using System.Reflection;

namespace TACBuilder.ILReflection;

public class IlField(FieldInfo fieldInfo) : IlMember(fieldInfo)
{
    // TODO attributes
    private readonly FieldInfo _fieldInfo = fieldInfo;
    public IlType? DeclaringType { get; private set; }
    public bool IsStatic => _fieldInfo.IsStatic;
    public IlType? Type { get; private set; }
    public new string Name => _fieldInfo.Name;
    public int ModuleToken => _fieldInfo.Module.MetadataToken;
    public int MetadataToken => _fieldInfo.MetadataToken;
    public object? GetValue(object? value) => _fieldInfo.GetValue(value);
    public List<IlAttribute> Attributes { get; private set; }
    public new bool IsConstructed = false;

    public override void Construct()
    {
        DeclaringType = IlInstanceBuilder.GetType((_fieldInfo.ReflectedType ?? _fieldInfo.DeclaringType)!);
        Type = IlInstanceBuilder.GetType(_fieldInfo.FieldType);
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