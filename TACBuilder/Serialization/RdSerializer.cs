using System.Net.Mail;
using org.jacodb.api.net.generated.models;
using TACBuilder.BodyBuilder;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;

// ReSharper disable UnusedType.Global

namespace TACBuilder.Serialization;

public static class RdSerializer
{
    public static List<IlDto> Serialize(List<IlCacheable> instances)
    {
        var res = new List<IlDto>();
        foreach (var (idx, instance) in instances.Select((v, i) => (i, v)))
        {
            if (instance is not IlType type) continue;
            Console.WriteLine($"handling {idx}/{instances.Count}");
            var fields = type.Fields.Select(field =>
                new IlFieldDto(fieldType: field.Type!.GetTypeId(),
                    name: field.Name,
                    isStatic: field.IsStatic,
                    attrs: field.Attributes.Select(a => a.SerializeAttr()).ToList())).ToList();
            List<IlMethodDto> methods = type.Methods.Select(SerializeMethod).ToList();
            var attrs = type.Attributes.Select(SerializeAttr).ToList();
            IlDto dto = type switch
            {
                IlPointerType pointerType => new IlPointerTypeDto(
                    asmName: pointerType.AsmName,
                    namespaceName: pointerType.Namespace,
                    name: pointerType.Name,
                    declType: pointerType.DeclaringType?.GetTypeId(),
                    targetType: pointerType.TargetType.GetTypeId(),
                    genericArgs: pointerType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isGenericParam: pointerType.IsGenericParameter,
                    isManaged: pointerType.IsManaged,
                    isValueType: false,
                    attrs: attrs,
                    fields: fields,
                    methods: methods
                ),
                IlStructType structType => new IlStructTypeDto(
                    asmName: structType.AsmName,
                    namespaceName: structType.Namespace,
                    name: structType.Name,
                    declType: structType.DeclaringType?.GetTypeId(),
                    genericArgs: structType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isGenericParam: structType.IsGenericParameter,
                    isManaged: structType.IsManaged,
                    isValueType: structType.IsValueType,
                    attrs: attrs,
                    fields: fields,
                    methods: methods
                ),
                IlEnumType enumType => new IlEnumTypeDto(
                    asmName: enumType.AsmName,
                    namespaceName: enumType.Namespace,
                    underlyingType: enumType.UnderlyingType.GetTypeId(),
                    name: enumType.Name,
                    declType: enumType.DeclaringType?.GetTypeId(),
                    genericArgs: enumType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isGenericParam: enumType.IsGenericParameter,
                    isManaged: enumType.IsManaged,
                    isValueType: enumType.IsValueType,
                    attrs: attrs,
                    fields: fields,
                    methods: methods,
                    names: enumType.NameToValueMapping.Keys.ToList(),
                    values: enumType.NameToValueMapping.Values.Select(v => v.SerializeConst()).ToList()
                ),
                IlPrimitiveType primitiveType => new IlPrimitiveTypeDto(
                    asmName: primitiveType.AsmName,
                    namespaceName: primitiveType.Namespace,
                    name: primitiveType.Name,
                    declType: primitiveType.DeclaringType?.GetTypeId(),
                    genericArgs: primitiveType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isGenericParam: primitiveType.IsGenericParameter,
                    isManaged: primitiveType.IsManaged,
                    isValueType: primitiveType.IsValueType,
                    attrs: attrs,
                    fields: fields,
                    methods: methods
                ),
                IlClassType classType => new IlClassTypeDto(
                    asmName: classType.AsmName,
                    namespaceName: classType.Namespace,
                    name: classType.Name,
                    declType: classType.DeclaringType?.GetTypeId(),
                    genericArgs: classType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isGenericParam: classType.IsGenericParameter,
                    isManaged: classType.IsManaged,
                    isValueType: classType.IsValueType,
                    attrs: attrs,
                    fields: fields,
                    methods: methods
                ),
                IlArrayType arrayType => new IlArrayTypeDto(
                    asmName: arrayType.AsmName,
                    namespaceName: arrayType.Namespace,
                    name: arrayType.Name,
                    elementType: arrayType.ElementType.GetTypeId(),
                    declType: arrayType.DeclaringType?.GetTypeId(),
                    genericArgs: arrayType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isGenericParam: arrayType.IsGenericParameter,
                    isManaged: arrayType.IsManaged,
                    isValueType: arrayType.IsValueType,
                    attrs: attrs,
                    fields: fields,
                    methods: methods
                ),
                _ => throw new NotImplementedException(),
            };
            res.Add(dto);
        }

        return res;
    }

    private static List<IlStmtDto> SerializeMethodBody(IlMethod method)
    {
        if (method.Body == null || !method.HasMethodBody) return [];
        List<IlStmtDto> res = new List<IlStmtDto>();
        return method.Body!.Lines.Select(SerializeStmt).ToList();
    }

    private static IlStmtDto SerializeStmt(IlStmt stmt)
    {
        return stmt switch
        {
            ILAssignStmt assignStmt => new IlAssignStmtDto(lhs: (IlValueDto)SerializeExpr(assignStmt.Lhs),
                rhs: SerializeExpr(assignStmt.Rhs)),
            IlCallStmt callStmt => new IlCallStmtDto((IlCallDto)callStmt.Call.SerializeExpr()),
            IlCalliStmt calliStmt => new IlCalliStmtDto((IlCalliDto)calliStmt.Call.SerializeExpr()),
            IlReturnStmt returnStmt => new IlReturnStmtDto(returnStmt.RetVal?.SerializeExpr()),
            IlGotoStmt gotoStmt => new IlGotoStmtDto(gotoStmt.Target),
            IlIfStmt ifStmt => new IlIfStmtDto(target: ifStmt.Target, cond: ifStmt.Condition.SerializeExpr()),
            IlThrowStmt throwStmt => new IlThrowStmtDto(throwStmt.Value.SerializeExpr()),
            IlRethrowStmt => new IlRethrowStmtDto(),
            IlEndFinallyStmt => new IlEndFinallyStmtDto(),
            IlEndFaultStmt => new IlEndFaultStmtDto(),
            IlEndFilterStmt endFilterStmt => new IlEndFilterStmtDto(endFilterStmt.Value.SerializeExpr()),
            _ => throw new Exception($"{stmt} stmt serialization not yet supported")
        };
    }

    private static IlExprDto SerializeExpr(this IlExpr expr)
    {
        return expr switch
        {
            IlConstant constant => constant.SerializeConst(),
            IlLocalVar localVar => new IlVarAccessDto(kind: IlVarKind.local, index: localVar.Index,
                type: localVar.Type.GetTypeId()),
            IlTempVar tmp => new IlVarAccessDto(kind: IlVarKind.temp, index: tmp.Index, type: tmp.Type.GetTypeId()),
            IlMerged merged => new IlVarAccessDto(kind: IlVarKind.temp, index: merged.Index,
                type: merged.Type.GetTypeId()),
            IlErrVar err => new IlVarAccessDto(kind: IlVarKind.err, index: err.Index, type: err.Type.GetTypeId()),
            IlCall.Argument arg => new IlArgAccessDto(index: arg.Index, type: arg.Type.GetTypeId()),
            IlCall call => new IlCallDto(method: call.Method.GetRefId(), args: call.Args.Select(SerializeExpr).ToList(),
                type: new TypeId("", "")),
            IlCallIndirect calli => new IlCalliDto(signature: SerializeSignature(calli.Signature),
                type: new TypeId("", ""),
                ftn: calli.Callee.SerializeExpr(), args: calli.Arguments.Select(SerializeExpr).ToList()),
            IlUnaryOperation unaryOp => new IlUnaryOpDto(type: unaryOp.Type.GetTypeId(),
                operand: unaryOp.Operand.SerializeExpr()),
            IlBinaryOperation binaryOp => new IlBinaryOpDto(type: binaryOp.Type.GetTypeId(),
                lhs: binaryOp.Lhs.SerializeExpr(), rhs: binaryOp.Rhs.SerializeExpr()),
            IlNewExpr newExpr => new IlNewExprDto(newExpr.Type.GetTypeId()),
            IlSizeOfExpr sizeOfExpr => new IlSizeOfExprDto(type: sizeOfExpr.Type.GetTypeId(),
                targetType: sizeOfExpr.Arg.GetTypeId()),
            IlNewArrayExpr newArrayExpr => new IlNewArrayExprDto(size: newArrayExpr.Size.SerializeExpr(),
                type: newArrayExpr.Type.GetTypeId()),
            IlFieldAccess fieldAccess => new IlFieldAccessDto(instance: fieldAccess.Receiver?.SerializeExpr(),
                field: fieldAccess.Field.GetRefId(), type: fieldAccess.Type.GetTypeId()),
            IlArrayAccess arrayAccess => new IlArrayAccessDto(array: arrayAccess.Array.SerializeExpr(),
                index: arrayAccess.Index.SerializeExpr(), arrayAccess.Type.GetTypeId()),
            IlArrayLength lengthExpr => new IlArrayLengthExprDto(lengthExpr.Array.SerializeExpr(),
                lengthExpr.Type.GetTypeId()),
            IlStackAlloc stackAlloc => new IlStackAllocExprDto(size: stackAlloc.Size.SerializeExpr(),
                type: stackAlloc.Type.GetTypeId()),
            IlConvCastExpr convExpr => new IlConvExprDto(convExpr.Type.GetTypeId(), convExpr.Target.SerializeExpr(),
                convExpr.Type.GetTypeId()),
            IlBoxExpr boxExpr => new IlBoxExprDto(boxExpr.Type.GetTypeId(), boxExpr.Target.SerializeExpr(),
                boxExpr.Type.GetRefId()),
            IlUnboxExpr unboxExpr => new IlBoxExprDto(unboxExpr.Type.GetRefId(), unboxExpr.Target.SerializeExpr(),
                unboxExpr.Type.GetRefId()),
            IlIsInstExpr isInstExpr => new IlIsInstExprDto(isInstExpr.Type.GetRefId(),
                isInstExpr.Target.SerializeExpr(), isInstExpr.Type.GetRefId()),
            IlManagedRef managedRef => new IlManagedRefExprDto(value: managedRef.Value.SerializeExpr(),
                type: managedRef.Type.GetRefId()),
            IlManagedDeref managedDeref => new IlManagedDerefExprDto(value: managedDeref.Value.SerializeExpr(),
                type: managedDeref.Type.GetRefId()),
            IlUnmanagedRef unmanagedRef => new IlUnmanagedRefExprDto(value: unmanagedRef.Value.SerializeExpr(),
                type: unmanagedRef.Type.GetRefId()),
            IlUnmanagedDeref unmanagedDeref => new IlUnmanagedDerefExprDto(value: unmanagedDeref.Value.SerializeExpr(),
                type: unmanagedDeref.Type.GetRefId()),
            _ => throw new Exception($"{expr} expr of type {expr.GetType()} serialization not yet supported")
        };
    }

    private static IlConstDto SerializeConst(this IlConstant constant)
    {
        return constant switch
        {
            IlUint8Const ui8 => new IlUint8ConstDto(value: ui8.Value, type: ui8.Type.GetRefId()),
            // TODO rd works bad with unsigned
            IlInt8Const i8 => new IlInt8ConstDto(value: (byte)i8.Value, type: i8.Type.GetRefId()),
            IlInt16Const i16 => new IlInt16ConstDto(value: i16.Value, type: i16.Type.GetRefId()),
            IlUint16Const ui16 => new IlUint16ConstDto(value: ui16.Value, type: ui16.Type.GetRefId()),
            IlInt32Const i32 => new IlInt32ConstDto(value: i32.Value, type: i32.Type.GetRefId()),
            IlUint32Const ui32 => new IlUint32ConstDto(value: ui32.Value, type: ui32.Type.GetRefId()),
            IlInt64Const i64 => new IlInt64ConstDto(value: i64.Value, type: i64.Type.GetRefId()),
            IlUint64Const ui64 => new IlUint64ConstDto(value: ui64.Value, type: ui64.Type.GetRefId()),
            IlFloatConst floatConst => new IlFloatConstDto(value: floatConst.Value, type: floatConst.Type.GetRefId()),
            IlDoubleConst doubleConst => new IlDoubleConstDto(value: doubleConst.Value,
                type: doubleConst.Type.GetTypeId()),
            IlCharConst charConst => new IlCharConstDto(value: charConst.Value, type: charConst.Type.GetTypeId()),
            IlStringConst stringConst => new IlStringConstDto(value: stringConst.Value,
                type: stringConst.Type.GetTypeId()),
            IlNullConst nullConst => new IlNullDto(nullConst.Type.GetTypeId()),
            IlBoolConst boolConst => new IlBoolConstDto(value: boolConst.Value, type: boolConst.Type.GetTypeId()),
            IlTypeRef typeRef => new IlTypeRefDto(referencedType: typeRef.ReferencedType.GetTypeId(),
                type: typeRef.Type.GetTypeId()),
            IlFieldRef fieldRef => new IlFieldRefDto(field: fieldRef.Field.GetRefId(), type: fieldRef.Type.GetTypeId()),
            IlMethodRef methodRef => new IlMethodRefDto(method: methodRef.Method.GetRefId(),
                type: methodRef.Type.GetTypeId()),
            IlEnumConst enumConst => new IlEnumConstDto(type: enumConst.Type.GetTypeId(), underlyingType: (enumConst
                .Type as IlEnumType)!.UnderlyingType.GetTypeId(), underlyingValue: enumConst.Value.SerializeConst()),
            IlArrayConst arrayConst => new IlArrayConstDto(type: arrayConst.Type.GetTypeId(),
                values: arrayConst.Values.Select(v => v.SerializeConst()).ToList()),
            _ => throw new Exception($"{constant} const of type {constant.GetType()} serialization not yet supported")
        };
    }

    private static IlAttrDto SerializeAttr(this IlAttribute attr)
    {
        var namedFlatten = attr.NamedArguments.ToList();
        return new IlAttrDto(
            attrType: attr.Type!.GetTypeId(),
            ctorArgs: attr.ConstructorArguments.Select(arg => arg.Value.SerializeConst()).ToList(),
            namedArgsNames: namedFlatten.Select(p => p.Key).ToList(),
            namedArgsValues: namedFlatten.Select(p => p.Value.Value.SerializeConst()).ToList()
        );
    }

    private static IlSignatureDto SerializeSignature(this IlSignature signature)
    {
        return new IlSignatureDto(returnType: signature.ReturnType!.GetTypeId(), isInstance: signature.IsInstance,
            genericParamCount: signature.GenericParameterCount,
            parametersTypes: signature.ParameterTypes.Select(t => t.GetTypeId()).ToList());
    }

    private static IlMethodDto SerializeMethod(this IlMethod method)
    {
        return new IlMethodDto(returnType: method.ReturnType?.GetTypeId(),
            attrs: method.Attributes.Select(SerializeAttr).ToList(), name: method.Name,
            parameters: method.Parameters
                .Select(p => new IlParameterDto(p.Position, p.Type.GetTypeId(), p.Name, null,
                    attrs: p.Attributes.Select(a => a.SerializeAttr()).ToList())).ToList(),
            resolved: method.IsConstructed,
            locals: method.LocalVars.Select(v =>
                new IlLocalVarDto(type: v.Type.GetTypeId(), index: v.Index, isPinned: v.IsPinned)).ToList(),
            temps: method.Temps.Values.Select(v => new IlTempVarDto(index: v.Index, type: v.Type.GetTypeId()))
                .ToList(),
            errs: method.Errs.Select(v =>
                new IlErrVarDto(type: v.Type.GetTypeId(), index: v.Index)).ToList(),
            ehScopes: method.Scopes.Select(scope =>
            {
                IlEhScopeDto res = scope switch
                {
                    FilterScope filterScope => new IlFilterScopeDto(tb: filterScope.tacLoc.tb,
                        te: filterScope.tacLoc.te, hb: filterScope.tacLoc.hb, he: filterScope.tacLoc.he,
                        fb: filterScope.fbt),
                    CatchScope catchScope => new IlCatchScopeDto(tb: catchScope.tacLoc.tb,
                        te: catchScope.tacLoc.te, hb: catchScope.tacLoc.hb, he: catchScope.tacLoc.he),

                    FinallyScope finallyScope => new IlFinallyScopeDto(tb: finallyScope.tacLoc.tb,
                        te: finallyScope.tacLoc.te, hb: finallyScope.tacLoc.hb, he: finallyScope.tacLoc.he),
                    FaultScope faultScope => new IlFaultScopeDto(tb: faultScope.tacLoc.tb,
                        te: faultScope.tacLoc.te, hb: faultScope.tacLoc.hb, he: faultScope.tacLoc.he),
                    _ => throw new NotImplementedException(),
                };
                return res;
            }).ToList(),
            body: SerializeMethodBody(method)
        );
    }
}

static class KeyBuilder
{
    public static TypeId GetTypeId(this IlType type)
    {
        if (type is null)
        {
            Console.WriteLine("Null type");
            return new TypeId("", "");
        }

        return new TypeId(asmName: type.AsmName, typeName: type.FullName);
    }

    public static TypeId GetRefId(this IlType type)
    {
        return type.GetTypeId();
    }

    public static InstanceIdRef GetRefId(this IlField field)
    {
        return new InstanceIdRef(type: field.DeclaringType!.GetTypeId(), instanceToken: field.MetadataToken);
    }

    public static InstanceIdRef GetRefId(this IlMethod method)
    {
        return new InstanceIdRef(type: method.DeclaringType!.GetTypeId(), instanceToken: method.MetadataToken);
    }
}

// TODO calli, ilsignature