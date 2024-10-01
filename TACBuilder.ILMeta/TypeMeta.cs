using System.Drawing;
using System.Reflection;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

public class TypeMeta : MemberMeta
{
    public AssemblyMeta DeclaringAssembly { get; }
    private Type _type;
    public int MetadataToken => _type.MetadataToken;
    public Type Type => _type;

    private const BindingFlags BindingFlags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
        System.Reflection.BindingFlags.DeclaredOnly;

    public TypeMeta(AssemblyMeta declaringAssembly, Type type) : base(type)
    {
        DeclaringAssembly = declaringAssembly;
        var cache = declaringAssembly.GetCorrespondingModuleCache(type.Module.MetadataToken);
        _type = type;
        Fields = type.GetFields().Select(fieldInfo =>
                cache.PutField(new FieldMeta(fieldInfo)))
            .ToList();
        Methods = type.GetMethods(BindingFlags).Where(methodInfo => !methodInfo.IsGenericMethod || methodInfo.IsGenericMethodDefinition)
            .Select(methodInfo => cache.PutMethod(new MethodMeta(this, methodInfo)))
            .Concat(
                type.GetConstructors(BindingFlags).Where(ctor => !ctor.IsGenericMethod || ctor.IsGenericMethodDefinition).Select(ctor => cache.PutMethod(new MethodMeta(this, ctor)))
            ).ToList();
    }

    public List<MethodMeta> Methods { get; }
    public List<FieldMeta> Fields { get; }
    public string Name => _type.Name;
    public string Namespace => _type.Namespace ?? "";

    public void Resolve()
    {
        foreach (var method in Methods)
        {
            method.Resolve();
        }
    }
}

public class FieldMeta(FieldInfo fieldInfo) : MemberMeta(fieldInfo)
{
    private FieldInfo _fieldInfo = fieldInfo;
    public Type FieldType => _fieldInfo.FieldType;
    public string Name => _fieldInfo.Name;
    public string DeclaringTypeName => _fieldInfo.DeclaringType?.FullName ?? "";
    public int MetadataToken => _fieldInfo.MetadataToken;
}
