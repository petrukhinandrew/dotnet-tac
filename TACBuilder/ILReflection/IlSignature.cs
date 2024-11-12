using System.Collections.Immutable;
using System.Numerics;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace TACBuilder.ILReflection;

public class IlSignature : IlCacheable
{
    private MethodSignature<Type> _signature;
    public bool HasExplicitThis { get; private set; }
    public bool IsGeneric { get; private set; }
    public bool IsInstance { get; private set; }

    // TODO seems like may be null
    public IlType? ReturnType { get; private set; }
    public List<IlType> ParameterTypes { get; } = new();
    public new bool IsConstructed = true;


    public unsafe IlSignature(byte[] rawBytes, MethodBase src)
    {
        src.Module.Assembly.TryGetRawMetadata(out var blob, out var length);
        MetadataReader reader = new MetadataReader(blob, length);
        var decoder =
            new SignatureDecoder<Type, Type[]>(new SigTypeProdiver(src), reader, src.GetGenericArguments());
        fixed (byte* firstByte = &rawBytes[0])
        {
            var blobReader = new BlobReader(firstByte, rawBytes.Length);
            _signature = decoder.DecodeMethodSignature(ref blobReader);
        }

        HasExplicitThis = _signature.Header.HasExplicitThis;
        IsGeneric = _signature.Header.IsGeneric;
        IsInstance = _signature.Header.IsInstance;
        ReturnType = _signature.ReturnType == null ? null : IlInstanceBuilder.GetType(_signature.ReturnType);
        ParameterTypes.AddRange(_signature.ParameterTypes.Select(IlInstanceBuilder.GetType));
        IsConstructed = true;
    }

    public override void Construct()
    {
    }
}

internal class SigTypeProdiver(MethodBase src) : ISignatureTypeProvider<Type, Type[]>
{
    private readonly MethodBase _method = src;
    public Type GetSZArrayType(Type elementType)
    {
        return elementType.MakeArrayType();
    }

    public Type GetArrayType(Type elementType, ArrayShape shape)
    {
        return elementType.MakeArrayType(shape.Rank);
    }

    public Type GetByReferenceType(Type elementType)
    {
        return elementType.MakeByRefType();
    }

    public Type GetGenericInstantiation(Type genericType, ImmutableArray<Type> typeArguments)
    {
        return genericType.MakeGenericType(typeArguments.ToArray());
    }

    public Type GetPointerType(Type elementType)
    {
        return elementType.MakePointerType();
    }

    public Type GetPrimitiveType(PrimitiveTypeCode typeCode)
    {
        return typeCode switch
        {
            PrimitiveTypeCode.Void => typeof(void),
            PrimitiveTypeCode.Boolean => typeof(bool),
            PrimitiveTypeCode.Char => typeof(char),
            PrimitiveTypeCode.SByte => typeof(sbyte),
            PrimitiveTypeCode.Byte => typeof(byte),
            PrimitiveTypeCode.Int16 => typeof(short),
            PrimitiveTypeCode.UInt16 => typeof(ushort),
            PrimitiveTypeCode.Int32 => typeof(int),
            PrimitiveTypeCode.UInt32 => typeof(uint),
            PrimitiveTypeCode.Int64 => typeof(long),
            PrimitiveTypeCode.UInt64 => typeof(ulong),
            PrimitiveTypeCode.Single => typeof(float),
            PrimitiveTypeCode.Double => typeof(double),
            PrimitiveTypeCode.String => typeof(string),
            PrimitiveTypeCode.TypedReference => typeof(TypedReference),
            PrimitiveTypeCode.IntPtr => typeof(IntPtr),
            PrimitiveTypeCode.UIntPtr => typeof(UIntPtr),
            PrimitiveTypeCode.Object => typeof(object),
            _ => throw new ArgumentOutOfRangeException(nameof(typeCode), typeCode, null)
        };
    }

    public Type GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var token = reader.GetToken(handle);
        return _method.Module.ResolveType(token);
    }

    public Type GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var token = reader.GetToken(handle);
        return _method.Module.ResolveType(token);
    }

    public Type GetFunctionPointerType(MethodSignature<Type> signature)
    {
        throw new NotImplementedException();
    }

    public Type GetGenericMethodParameter(Type[] genericContext, int index)
    {
        return genericContext[index];
    }

    public Type GetGenericTypeParameter(Type[] genericContext, int index)
    {
        return genericContext[index];
    }

    public Type GetModifiedType(Type modifier, Type unmodifiedType, bool isRequired)
    {
        throw new NotImplementedException();
    }

    public Type GetPinnedType(Type elementType)
    {
        // var instance = GCHandle.Alloc(Activator.CreateInstance(elementType), GCHandleType.Pinned);
        // try
        // {
        //     return instance.GetType();
        // }
        // finally
        // {
        //     instance.Free();
        // }
        throw new NotImplementedException();
    }

    public Type GetTypeFromSpecification(MetadataReader reader, Type[] genericContext, TypeSpecificationHandle handle,
        byte rawTypeKind)
    {
        var typeSpec = reader.GetTypeSpecification(handle);
        return typeSpec.DecodeSignature(this, genericContext);
    }
}
