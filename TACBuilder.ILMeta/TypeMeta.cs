using System.Drawing;
using System.Reflection;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class TypeMeta : MemberMeta
{
    private readonly Type _type;
    public Type BaseType => _type;

    private const BindingFlags BindingFlags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
        System.Reflection.BindingFlags.DeclaredOnly;

    public TypeMeta(Type type) : base(type)
    {
        _type = type;
    }

    public override void Construct()
    {
        DeclaringAssembly = MetaCache.GetAssembly(_type.Assembly);
        Fields = _type.GetFields(BindingFlags).Select(MetaCache.GetField).ToList();
        var constructors = _type.GetConstructors(BindingFlags).Select(method => MetaCache.GetMethod(method));
        Methods = _type.GetMethods(BindingFlags).Select(method => MetaCache.GetMethod(method)).Concat(constructors)
            .ToList();
    }

    public AssemblyMeta DeclaringAssembly { get; private set; }
    public List<MethodMeta> Methods { get; private set; }
    public List<FieldMeta> Fields { get; private set; }
    public string Name => _type.Name;
    public string Namespace => _type.Namespace ?? "";
    public int MetadataToken => _type.MetadataToken;
    public Type Type => _type;
}

public class FieldMeta(FieldInfo fieldInfo) : MemberMeta(fieldInfo)
{
    // TODO must be in Construct instead of init
    public Type FieldType => fieldInfo.FieldType;
    public string Name => fieldInfo.Name;
    public string DeclaringTypeName => fieldInfo.DeclaringType?.FullName ?? "";
    public int MetadataToken => fieldInfo.MetadataToken;

    public override void Construct()
    {
    }
}
