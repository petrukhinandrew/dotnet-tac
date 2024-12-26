#define TEST_DUMPSIGNATURES
using System.Diagnostics;
using org.jacodb.api.net.generated.models;
using TACBuilder.BodyBuilder;
using TACBuilder.Exprs;
using TACBuilder.ILReflection;
using TACBuilder.ILTAC.TypeSystem;

// ReSharper disable UnusedType.Global

namespace TACBuilder.Serialization;

public static class RdSerializer
{
    public static List<IlTypeDto> Serialize(List<IlType> instances)
    {
        var res = new List<IlTypeDto>();
        foreach (var (idx, type) in instances.OrderBy(t => (t as IlType)!.Name)
                     .Select((v, i) => (i, (v as IlType)!)))
        {
            // Console.WriteLine($"handling {idx}/{instances.Count} {type.FullName}");
            var fields = type.Fields.Select(field =>
                new IlFieldDto(fieldType: field.Type!.GetTypeId(),
                    name: field.Name,
                    isStatic: field.IsStatic,
                    isConstructed: field.IsConstructed,
                    attrs: field.Attributes.Select(a => a.SerializeAttr()).ToList())).ToList();
            List<IlMethodDto> methods = type.Methods.Select(SerializeMethod).ToList();
            List<TypeId> interfaces = type.Interfaces.Select(t => t.GetTypeId()).ToList();
            var typeToken = type.MetadataToken;
            var moduleToken = type.ModuleToken;
            var attrs = type.Attributes.Select(SerializeAttr).ToList();
            IlTypeDto dto = type switch
            {
                IlPointerType pointerType => new IlPointerTypeDto(
                    asmName: pointerType.AsmName,
                    typeToken: typeToken,
                    moduleToken: moduleToken,
                    namespaceName: pointerType.Namespace,
                    name: pointerType.Name,
                    fullname: pointerType.FullName,
                    declType: pointerType.DeclaringType?.GetTypeId(),
                    baseType: pointerType.BaseType?.GetTypeId(),
                    interfaces: interfaces,
                    targetType: pointerType.TargetType.GetTypeId(),
                    genericArgs: pointerType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isManaged: pointerType.IsManaged,
                    isValueType: false,
                    attrs: attrs,
                    fields: fields,
                    methods: methods,
                    isConstructed: pointerType.IsConstructed,
                    isGenericType: pointerType.IsGenericType,
                    isGenericParam: pointerType.IsGenericParameter,
                    isGenericDefinition: pointerType.IsGenericDefinition,
                    genericDefinition: pointerType.GenericDefinition?.GetTypeId(),
                    isCovariant: pointerType.IsCovariant,
                    isContravariant: pointerType.IsContravariant,
                    hasRefTypeConstraint: pointerType.HasRefTypeConstraint,
                    hasNotNullValueTypeConstraint: pointerType.HasNotNullValueTypeConstraint,
                    hasDefaultCtorConstraint: pointerType.HasDefaultCtorConstraint
                ),
                IlStructType structType => new IlStructTypeDto(
                    asmName: structType.AsmName,
                    typeToken: typeToken,
                    moduleToken: moduleToken,
                    namespaceName: structType.Namespace,
                    name: structType.Name,
                    fullname: structType.FullName,
                    declType: structType.DeclaringType?.GetTypeId(),
                    baseType: structType.BaseType?.GetTypeId(),
                    interfaces: interfaces,
                    genericArgs: structType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isManaged: structType.IsManaged,
                    isValueType: structType.IsValueType,
                    attrs: attrs,
                    fields: fields,
                    methods: methods,
                    isConstructed: structType.IsConstructed,
                    isGenericType: structType.IsGenericType,
                    isGenericParam: structType.IsGenericParameter,
                    isGenericDefinition: structType.IsGenericDefinition,
                    genericDefinition: structType.GenericDefinition?.GetTypeId(),
                    isCovariant: structType.IsCovariant,
                    isContravariant: structType.IsContravariant,
                    hasRefTypeConstraint: structType.HasRefTypeConstraint,
                    hasNotNullValueTypeConstraint: structType.HasNotNullValueTypeConstraint,
                    hasDefaultCtorConstraint: structType.HasDefaultCtorConstraint
                ),
                IlEnumType enumType => new IlEnumTypeDto(
                    asmName: enumType.AsmName,
                    typeToken: typeToken,
                    moduleToken: moduleToken,
                    namespaceName: enumType.Namespace,
                    underlyingType: enumType.UnderlyingType.GetTypeId(),
                    name: enumType.Name,
                    fullname: enumType.FullName,
                    declType: enumType.DeclaringType?.GetTypeId(),
                    baseType: enumType.BaseType?.GetTypeId(),
                    interfaces: interfaces,
                    genericArgs: enumType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isManaged: enumType.IsManaged,
                    isValueType: enumType.IsValueType,
                    attrs: attrs,
                    fields: fields,
                    methods: methods,
                    isConstructed: enumType.IsConstructed,
                    isGenericType: enumType.IsGenericType,
                    isGenericParam: enumType.IsGenericParameter,
                    isGenericDefinition: enumType.IsGenericDefinition,
                    genericDefinition: enumType.GenericDefinition?.GetTypeId(),
                    isCovariant: enumType.IsCovariant,
                    isContravariant: enumType.IsContravariant,
                    hasRefTypeConstraint: enumType.HasRefTypeConstraint,
                    hasNotNullValueTypeConstraint: enumType.HasNotNullValueTypeConstraint,
                    hasDefaultCtorConstraint: enumType.HasDefaultCtorConstraint,
                    names: enumType.NameToValueMapping.Keys.ToList(),
                    values: enumType.NameToValueMapping.Values.Select(v => v.SerializeConst()).ToList()
                ),
                IlPrimitiveType primitiveType => new IlPrimitiveTypeDto(
                    asmName: primitiveType.AsmName,
                    typeToken: typeToken,
                    moduleToken: moduleToken,
                    namespaceName: primitiveType.Namespace,
                    name: primitiveType.Name,
                    fullname: primitiveType.FullName,
                    declType: primitiveType.DeclaringType?.GetTypeId(),
                    baseType: primitiveType.BaseType?.GetTypeId(),
                    interfaces: interfaces,
                    genericArgs: primitiveType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isManaged: primitiveType.IsManaged,
                    isValueType: primitiveType.IsValueType,
                    attrs: attrs,
                    fields: fields,
                    methods: methods,
                    isConstructed: primitiveType.IsConstructed,
                    isGenericType: primitiveType.IsGenericType,
                    isGenericParam: primitiveType.IsGenericParameter,
                    isGenericDefinition: primitiveType.IsGenericDefinition,
                    genericDefinition: primitiveType.GenericDefinition?.GetTypeId(),
                    isCovariant: primitiveType.IsCovariant,
                    isContravariant: primitiveType.IsContravariant,
                    hasRefTypeConstraint: primitiveType.HasRefTypeConstraint,
                    hasNotNullValueTypeConstraint: primitiveType.HasNotNullValueTypeConstraint,
                    hasDefaultCtorConstraint: primitiveType.HasDefaultCtorConstraint
                ),
                IlClassType classType => new IlClassTypeDto(
                    asmName: classType.AsmName,
                    typeToken: typeToken,
                    moduleToken: moduleToken,
                    namespaceName: classType.Namespace,
                    name: classType.Name,
                    fullname: classType.FullName,
                    declType: classType.DeclaringType?.GetTypeId(),
                    baseType: classType.BaseType?.GetTypeId(),
                    interfaces: interfaces,
                    genericArgs: classType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isManaged: classType.IsManaged,
                    isValueType: classType.IsValueType,
                    attrs: attrs,
                    fields: fields,
                    methods: methods,
                    isConstructed: classType.IsConstructed,
                    isGenericType: classType.IsGenericType,
                    isGenericParam: classType.IsGenericParameter,
                    isGenericDefinition: classType.IsGenericDefinition,
                    genericDefinition: classType.GenericDefinition?.GetTypeId(),
                    isCovariant: classType.IsCovariant,
                    isContravariant: classType.IsContravariant,
                    hasRefTypeConstraint: classType.HasRefTypeConstraint,
                    hasNotNullValueTypeConstraint: classType.HasNotNullValueTypeConstraint,
                    hasDefaultCtorConstraint: classType.HasDefaultCtorConstraint
                ),
                IlArrayType arrayType => new IlArrayTypeDto(
                    asmName: arrayType.AsmName,
                    typeToken: typeToken,
                    moduleToken: moduleToken,
                    namespaceName: arrayType.Namespace,
                    name: arrayType.Name,
                    fullname: arrayType.FullName,
                    elementType: arrayType.ElementType.GetTypeId(),
                    declType: arrayType.DeclaringType?.GetTypeId(),
                    baseType: arrayType.BaseType?.GetTypeId(),
                    interfaces: interfaces,
                    genericArgs: arrayType.GenericArgs.Select(a => a.GetTypeId()).ToList(),
                    isManaged: arrayType.IsManaged,
                    isValueType: arrayType.IsValueType,
                    attrs: attrs,
                    fields: fields,
                    methods: methods,
                    isConstructed: arrayType.IsConstructed,
                    isGenericType: arrayType.IsGenericType,
                    isGenericParam: arrayType.IsGenericParameter,
                    isGenericDefinition: arrayType.IsGenericDefinition,
                    genericDefinition: arrayType.GenericDefinition?.GetTypeId(),
                    isCovariant: arrayType.IsCovariant,
                    isContravariant: arrayType.IsContravariant,
                    hasRefTypeConstraint: arrayType.HasRefTypeConstraint,
                    hasNotNullValueTypeConstraint: arrayType.HasNotNullValueTypeConstraint,
                    hasDefaultCtorConstraint: arrayType.HasDefaultCtorConstraint
                ),
                _ => throw new NotImplementedException(),
            };
            res.Add(dto);
        }
#if TEST_DUMPSIGNATURES
        var tmpPath = Path.GetTempFileName();
        Console.WriteLine(tmpPath);
        var tmpFile = File.Create(tmpPath);
        var printer = new StreamWriter(tmpFile);
        foreach (var t in res.Where(r => r is IlTypeDto).Select(r => (r as IlTypeDto)!).OrderBy(t => t.Name))
        {
            printer.WriteLine(t.Name);
            foreach (var field in t.Fields.OrderBy(f => f.Name))
            {
                printer.WriteLine($"{field.FieldType.TypeName} {field.Name}");
            }

            foreach (var method in t.Methods.OrderBy(m => m.Name))
            {
                printer.WriteLine($"{method.ReturnType.TypeName} {method.Name} {method.Parameters.Count}");
            }
        }

        printer.Close();
        tmpFile.Close();
#endif
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
            ILAssignStmt assignStmt => new IlAssignStmtDto(lhv: (IlValueDto)SerializeExpr(assignStmt.Lhs),
                rhv: SerializeExpr(assignStmt.Rhs)),
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

    private static IlExprDto SerializeOp(this IlExpr expr)
    {
        Debug.Assert(expr is IlUnaryOperation || expr is IlBinaryOperation);
        return expr switch
        {
            IlNotOp notOp => new IlNotOpDto(notOp.Operand.SerializeExpr(), notOp.Type.GetTypeId()),
            IlNegOp negOp => new IlNegOpDto(negOp.Operand.SerializeExpr(), negOp.Type.GetTypeId()),
            IlAddOp addOp => new IlAddOpDto(addOp.Lhs.SerializeExpr(), addOp.Rhs.SerializeExpr(), addOp.IsChecked,
                addOp.IsUnsigned, addOp.Type.GetTypeId()),
            IlSubOp subOp => new IlSubOpDto(subOp.Lhs.SerializeExpr(), subOp.Rhs.SerializeExpr(), subOp.IsChecked,
                subOp.IsUnsigned, subOp.Type.GetTypeId()),
            IlMulOp mulOp => new IlMulOpDto(mulOp.Lhs.SerializeExpr(), mulOp.Rhs.SerializeExpr(), mulOp.IsChecked,
                mulOp.IsUnsigned, mulOp.Type.GetTypeId()),
            IlDivOp divOp => new IlDivOpDto(divOp.Lhs.SerializeExpr(), divOp.Rhs.SerializeExpr(), divOp.IsChecked,
                divOp.IsUnsigned, divOp.Type.GetTypeId()),
            IlRemOp remOp => new IlRemOpDto(remOp.Lhs.SerializeExpr(), remOp.Rhs.SerializeExpr(), remOp.IsChecked,
                remOp.IsUnsigned, remOp.Type.GetTypeId()),
            IlAndOp andOp => new IlAndOpDto(andOp.Lhs.SerializeExpr(), andOp.Rhs.SerializeExpr(), andOp.IsChecked,
                andOp.IsUnsigned, andOp.Type.GetTypeId()),
            IlOrOp orOp => new IlOrOpDto(orOp.Lhs.SerializeExpr(), orOp.Rhs.SerializeExpr(), orOp.IsChecked,
                orOp.IsUnsigned, orOp.Type.GetTypeId()),
            IlXorOp xorOp => new IlXorOpDto(xorOp.Lhs.SerializeExpr(), xorOp.Rhs.SerializeExpr(), xorOp.IsChecked,
                xorOp.IsUnsigned, xorOp.Type.GetTypeId()),
            IlShlOp shlOp => new IlShlOpDto(shlOp.Lhs.SerializeExpr(), shlOp.Rhs.SerializeExpr(), shlOp.IsChecked,
                shlOp.IsUnsigned, shlOp.Type.GetTypeId()),
            IlShrOp shrOp => new IlShrOpDto(shrOp.Lhs.SerializeExpr(), shrOp.Rhs.SerializeExpr(), shrOp.IsChecked,
                shrOp.IsUnsigned, shrOp.Type.GetTypeId()),
            IlCeqOp ceqOp => new IlCeqOpDto(ceqOp.Lhs.SerializeExpr(), ceqOp.Rhs.SerializeExpr(), ceqOp.IsChecked,
                ceqOp.IsUnsigned, ceqOp.Type.GetTypeId()),
            IlCneOp cneOp => new IlCneOpDto(cneOp.Lhs.SerializeExpr(), cneOp.Rhs.SerializeExpr(), cneOp.IsChecked,
                cneOp.IsUnsigned, cneOp.Type.GetTypeId()),
            IlCgtOp cgtOp => new IlCgtOpDto(cgtOp.Lhs.SerializeExpr(), cgtOp.Rhs.SerializeExpr(), cgtOp.IsChecked,
                cgtOp.IsUnsigned, cgtOp.Type.GetTypeId()),
            IlCgeOp cgeOp => new IlCgeOpDto(cgeOp.Lhs.SerializeExpr(), cgeOp.Rhs.SerializeExpr(), cgeOp.IsChecked,
                cgeOp.IsUnsigned, cgeOp.Type.GetTypeId()),
            IlCltOp cltOp => new IlCltOpDto(cltOp.Lhs.SerializeExpr(), cltOp.Rhs.SerializeExpr(), cltOp.IsChecked,
                cltOp.IsUnsigned, cltOp.Type.GetTypeId()),
            IlCleOp cleOp => new IlCleOpDto(cleOp.Lhs.SerializeExpr(), cleOp.Rhs.SerializeExpr(), cleOp.IsChecked,
                cleOp.IsUnsigned, cleOp.Type.GetTypeId()),
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
            // TODO check usage of type 
            IlCall call => new IlCallDto(method: call.Method.GetRefId(), args: call.Args.Select(SerializeExpr).ToList(),
                type: new TypeId([], "", "")),
            // TODO check usage of type 
            IlCallIndirect calli => new IlCalliDto(signature: SerializeSignature(calli.Signature),
                type: new TypeId([], "", ""),
                ftn: calli.Callee.SerializeExpr(), args: calli.Arguments.Select(SerializeExpr).ToList()),
            IlUnaryOperation or IlBinaryOperation => expr.SerializeOp(),
            IlNewExpr newExpr => new IlNewExprDto(newExpr.Type.GetTypeId()),
            IlSizeOfExpr sizeOfExpr => new IlSizeOfExprDto(type: sizeOfExpr.Type.GetTypeId(),
                targetType: sizeOfExpr.Arg.GetTypeId()),
            IlNewArrayExpr newArrayExpr => new IlNewArrayExprDto(size: newArrayExpr.Size.SerializeExpr(),
                type: newArrayExpr.Type.GetTypeId(),
                elementType: (newArrayExpr.Type as IlArrayType)!.ElementType.GetTypeId()),
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
            namedArgsValues: namedFlatten.Select(p => p.Value.Value.SerializeConst()).ToList(),
            genericArgs: attr.GenericArgs.Select(arg => arg.GetTypeId()).ToList()
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
        return new IlMethodDto(
            returnType: method.ReturnType?.GetTypeId(),
            attrs: method.Attributes.Select(SerializeAttr).ToList(),
            isStatic: method.IsStatic,
            name: method.Name,
            isGeneric: method.IsGeneric,
            isGenericDefinition: method.IsGenericMethodDefinition,
            signature: method.Signature,
            parameters: method.Parameters
                .Select(p => new IlParameterDto(p.Position, p.Type.GetTypeId(), p.Name, null,
                    attrs: p.Attributes.Select(a => a.SerializeAttr()).ToList())).ToList(),
            genericArgs: method.GenericArgs.Select(arg => arg.GetTypeId()).ToList(),
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
            rawInstList: SerializeMethodBody(method),
            isConstructed: method.IsConstructed
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
            return new TypeId(asmName: "", typeName: "", typeArgs: []);
        }

        return new TypeId(asmName: type.AsmName, typeName: type.FullName,
            typeArgs: type.GenericArgs.Select(TypeIdBase (t) => t.GetTypeId()).ToList());
    }

    public static TypeId GetRefId(this IlType type)
    {
        return type.GetTypeId();
    }

    public static InstanceId GetRefId(this IlField field)
    {
        return new InstanceId(type: field.DeclaringType!.GetTypeId(), name: field.Name);
    }

    public static InstanceId GetRefId(this IlMethod method)
    {
        return new InstanceId(type: method.DeclaringType!.GetTypeId(), name: method.Signature);
    }
}