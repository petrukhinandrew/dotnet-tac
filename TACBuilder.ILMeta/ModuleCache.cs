using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TACBuilder.ILMeta.ILBodyParser;

namespace TACBuilder.ILMeta;

using MemberToken = int;

public class ModuleCache(AssemblyMeta assemblyMeta, Module module)
{
    private Dictionary<MemberToken, CacheableMeta> _cache = new();
    private Module _module = module;

    private bool IsMemberSpec(int token)
    {
        int typeSpecMask = 0x1B;
        int methodSpecMask = 0x2B;
        int target = token >> 24;
        return (target & typeSpecMask) == typeSpecMask || (target & methodSpecMask) == methodSpecMask;
    }

    public TypeMeta PutType(TypeMeta typeMeta)
    {
        _cache.TryAdd(typeMeta.MetadataToken, typeMeta);
        return (TypeMeta)_cache[typeMeta.MetadataToken];
    }

    public FieldMeta PutField(FieldMeta fieldMeta)
    {
        _cache.TryAdd(fieldMeta.MetadataToken, fieldMeta);
        return (FieldMeta)_cache[fieldMeta.MetadataToken];
    }

    public MethodMeta PutMethod(MethodMeta methodMeta)
    {
        if (_cache.ContainsKey(methodMeta.MetadataToken))
        {
            Debug.Assert(false,
                "method meta already exist for " + methodMeta.Name + " as " +
                GetResolvedMethod(methodMeta.MetadataToken).Name);
        }

        _cache.TryAdd(methodMeta.MetadataToken, methodMeta);
        return (MethodMeta)_cache[methodMeta.MetadataToken];
    }

    public MemberMeta PutMember(MemberMeta memberMeta)
    {
        _cache.Add(memberMeta.MetadataToken, memberMeta);
        return (MemberMeta)_cache[memberMeta.MetadataToken];
    }

    private TypeMeta GetResolvedType(MemberToken token)
    {
        return (TypeMeta)_cache[token];
    }

    private FieldMeta GetResolvedField(MemberToken token)
    {
        return (FieldMeta)_cache[token];
    }

    private MethodMeta GetResolvedMethod(MemberToken token)
    {
        return (MethodMeta)_cache[token];
    }

    private MemberMeta GetResolvedMember(MemberToken token)
    {
        return (MemberMeta)_cache[token];
    }

    public TypeMeta GetType(MemberToken token, MethodBase source)
    {
        if (_cache.TryGetValue(token, out var defMeta)) return (TypeMeta)defMeta;

        var type = TokenResolver.ResolveType(token, source);
        // if (IsMemberSpec(token))
        // {
        //     _cache.Add(token, new TypeMeta(assemblyMeta, type));
        //     return (TypeMeta)_cache[token];
        // }
        if ((type.DeclaringType ?? type.ReflectedType) == null && type.IsPointer)
        {
            return GetType(type.GetElementType()!.MetadataToken, source);
        }

        var refCache = AssemblyMeta.FromName(type.Module.Assembly.GetName())
            .GetCorrespondingModuleCache(type.Module.MetadataToken);

        if (!refCache._cache.ContainsKey(type.MetadataToken))
            Debug.Assert(false);
        // if (refCache != this) _cache.Add(type.MetadataToken, refCache.GetResolvedType(type.MetadataToken));

        // return (TypeMeta)_cache[type.MetadataToken];
        return (TypeMeta)refCache.GetResolvedType(type.MetadataToken);
    }


    public FieldMeta GetField(MemberToken token, MethodBase source)
    {
        // may be generic type field
        var field = TokenResolver.ResolveField(token, source);

        if (_cache.TryGetValue(field.MetadataToken, out var value)) return (FieldMeta)value;

        var refCache = AssemblyMeta.FromName(field.Module.Assembly.GetName())
            .GetCorrespondingModuleCache(field.Module.MetadataToken);

        if (!refCache._cache.ContainsKey(field.MetadataToken))
        {
            Debug.Assert(false);
        }

        var expectedFieldMeta = refCache.GetResolvedField(field.MetadataToken);
        return expectedFieldMeta;
        // _cache.Add(field.MetadataToken, expectedTypeMeta);
        //
        // return (FieldMeta)_cache[field.MetadataToken];
    }


    public MethodMeta GetMethod(MemberToken token, MethodBase source)
    {
        if (_cache.TryGetValue(token, out var defMeta)) return (MethodMeta)defMeta;

        var methodBase = TokenResolver.ResolveMethod(token, source);
        // if (IsMemberSpec(token))
        // {
        //     var typeCache = AssemblyMeta.FromName(methodBase.Module.Assembly.GetName())
        //         .GetCorrespondingModuleCache(methodBase.Module.MetadataToken);
        //     _cache.Add(token,
        //         new MethodMeta(
        //             (TypeMeta)typeCache._cache[(methodBase.ReflectedType ?? methodBase.DeclaringType)!.MetadataToken],
        //             methodBase));
        //     return (MethodMeta)_cache[token];
        // }

        var refCache = AssemblyMeta.FromName(methodBase.Module.Assembly.GetName())
            .GetCorrespondingModuleCache(methodBase.Module.MetadataToken);
        Debug.Assert(refCache._cache.ContainsKey(methodBase.MetadataToken));
        var expectedMethodMeta = refCache.GetResolvedMethod(methodBase.MetadataToken);
        // if (refCache != this) _cache.Add(methodBase.MetadataToken, expectedMethodMeta);
        return expectedMethodMeta;
        // return (MethodMeta)_cache[methodBase.MetadataToken];
    }

    public MemberMeta GetMember(MemberToken token, MethodBase source)
    {
        var member = TokenResolver.ResolveMember(token, source);
        if (!_cache.ContainsKey(member.MetadataToken))
        {
            var refCache = AssemblyMeta.FromName(member.Module.Assembly.GetName())
                .GetCorrespondingModuleCache(member.Module.MetadataToken);
            var expectedMethodMeta = refCache.GetResolvedMember(member.MetadataToken);
            _cache.Add(member.MetadataToken, expectedMethodMeta);
        }

        return (MemberMeta)_cache[member.MetadataToken];
    }

    public SignatureMeta GetSignature(MemberToken token, MethodBase source)
    {
        if (!_cache.ContainsKey(token))
        {
            var signature = TokenResolver.ResolveSignature(token, source);
            _cache.Add(token, new SignatureMeta(signature));
        }

        return (SignatureMeta)_cache[token];
    }

    public StringMeta GetString(MemberToken token, MethodBase source)
    {
        if (!_cache.ContainsKey(token))
        {
            var str = TokenResolver.ResolveString(token, source);
            _cache.Add(token, new StringMeta(str));
        }

        return (StringMeta)_cache[token];
    }
}
