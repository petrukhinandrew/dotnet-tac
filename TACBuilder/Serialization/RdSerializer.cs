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
        foreach (var instance in instances)
        {
            IlDto? dto = null;
            if (instance is IlAssembly asm)
            {
                dto = new IlAsmDto(new AsmCacheKey(asm.MetadataToken), asm.Location!);
            }
            else if (instance is IlType type)
            {
                dto = new IlTypeDto(id: type.GetCacheKey(), name: type.Name,
                    genericArgs: type.GenericArgs.Select(a => a.GetCacheKey()).ToList(),
                    isGenericParam: type.IsGenericParameter, isValueType: type.IsValueType,
                    isManaged: type.IsManaged,
                    attrs: type.Attributes.Select(a => a.SerializeAttr()).ToList());
            }
            else if (instance is IlField field)
            {
                dto = new IlFieldDto(field.GetCacheKey(), field.DeclaringType.GetCacheKey(), field.Type.GetCacheKey(),
                    field.IsStatic, field.Name, field.Attributes.Select(a => a.SerializeAttr()).ToList());
            }
            else if (instance is IlMethod method)
            {
                dto = new IlMethodDto(
                    id: method.GetCacheKey(),
                    declType: method.DeclaringType?.GetCacheKey(),
                    returnType: method.ReturnType?.GetCacheKey(),
                    attrs: method.Attributes.Select(a => a.SerializeAttr()).ToList(),
                    name: method.Name,
                    parameters: method.Parameters
                        .Select(p => new IlParameterDto(p.Position, p.Type.GetCacheKey(), p.Name, null,
                            attrs: p.Attributes.Select(a => a.SerializeAttr()).ToList())).ToList(),
                    resolved: method.IsConstructed,
                    locals: method.LocalVars.Select(v =>
                        new IlLocalVarDto(type: v.Type.GetCacheKey(), index: v.Index, isPinned: v.IsPinned)).ToList(),
                    temps: method.Temps.Values.Select(v => new IlTempVarDto(index: v.Index, type: v.Type.GetCacheKey()))
                        .ToList(),
                    errs: method.Errs.Select(v =>
                        new IlErrVarDto(type: v.Type.GetCacheKey(), index: v.Index)).ToList(),
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

            if (dto == null) continue;
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
        if (stmt is ILAssignStmt assignStmt)

            return new IlAssignStmtDto(lhs: (IlValueDto)SerializeExpr(assignStmt.Lhs),
                rhs: SerializeExpr(assignStmt.Rhs));

        if (stmt is IlCallStmt callStmt)
            return
                new IlCallStmtDto(
                    (IlCallDto)callStmt.Call.SerializeExpr()); // itll be always like this, should be decided by cache

        if (stmt is IlReturnStmt returnStmt)
            return new IlReturnStmtDto(returnStmt.RetVal?.SerializeExpr());

        if (stmt is IlGotoStmt gotoStmt)
            return new IlGotoStmtDto(gotoStmt.Target);

        if (stmt is IlIfStmt ifStmt)
            return new IlIfStmtDto(target: ifStmt.Target, cond: ifStmt.Condition.SerializeExpr());

        if (stmt is IlThrowStmt throwStmt)
            return new IlThrowStmtDto(throwStmt.Value.SerializeExpr());

        if (stmt is IlRethrowStmt)
            return new IlRethrowStmtDto();

        if (stmt is IlEndFinallyStmt)
            return new IlEndFinallyStmtDto();

        if (stmt is IlEndFaultStmt)
            return new IlEndFaultStmtDto();

        if (stmt is IlEndFilterStmt endFilterStmt)
            return new IlEndFilterStmtDto(endFilterStmt.Value.SerializeExpr());

        throw new Exception($"{stmt} stmt serialization not yet supported");
    }

    private static IlExprDto SerializeExpr(this IlExpr expr)
    {
        if (expr is IlConstant constant)
            return constant.SerializeConst();
        if (expr is IlLocalVar localVar)
            return new IlVarAccessDto(kind: IlVarKind.local, index: localVar.Index, type: localVar.Type.GetCacheKey());
        if (expr is IlTempVar tmp)
            return new IlVarAccessDto(kind: IlVarKind.temp, index: tmp.Index, type: tmp.Type.GetCacheKey());
        if (expr is IlMerged merged)
            return new IlVarAccessDto(kind: IlVarKind.temp, index: merged.Index, type: merged.Type.GetCacheKey());
        if (expr is IlErrVar err)
            return new IlVarAccessDto(kind: IlVarKind.err, index: err.Index, type: err.Type.GetCacheKey());
        if (expr is IlArgument arg)
            return new IlArgAccessDto(index: arg.Index, type: arg.Type.GetCacheKey());

        if (expr is IlCall call)
            return new IlCallDto(method: call.Method.GetCacheKey(), args: call.Args.Select(SerializeExpr).ToList(),
                type: new CacheKey(0, 0, 0));

        if (expr is IlUnaryOperation unaryOp)
            return new IlUnaryOpDto(type: unaryOp.Type.GetCacheKey(), operand: unaryOp.Operand.SerializeExpr());
        if (expr is IlBinaryOperation binaryOp)
            return new IlBinaryOpDto(type: binaryOp.Type.GetCacheKey(), lhs: binaryOp.Lhs.SerializeExpr(),
                rhs: binaryOp.Rhs.SerializeExpr());
        if (expr is IlInitExpr initExpr)
            return new IlInitExprDto(initExpr.Type.GetCacheKey());
        if (expr is IlNewExpr ctorExpr)
            return new IlNewExprDto(ctorExpr.ConstructorCall.Args.Select(SerializeExpr).ToList(),
                ctorExpr.Type.GetCacheKey());
        if (expr is IlSizeOfExpr sizeOfExpr)
            return new IlSizeOfExprDto(type: sizeOfExpr.Type.GetCacheKey(), targetType: sizeOfExpr.Arg.GetCacheKey());
        if (expr is IlNewArrayExpr newArrayExpr)
            return new IlNewArrayExprDto(size: newArrayExpr.Size.SerializeExpr(),
                type: newArrayExpr.Type.GetCacheKey());
        if (expr is IlFieldAccess fieldAccess)
            return new IlFieldAccessDto(instance: fieldAccess.Receiver?.SerializeExpr(),
                field: fieldAccess.Field.GetCacheKey(), type: fieldAccess.Type.GetCacheKey());
        if (expr is IlArrayAccess arrayAccess)
            return new IlArrayAccessDto(array: arrayAccess.Array.SerializeExpr(),
                index: arrayAccess.Index.SerializeExpr(), arrayAccess.Type.GetCacheKey());
        if (expr is IlArrayLength lengthExpr)
            return new IlArrayLengthExprDto(lengthExpr.Array.SerializeExpr(), lengthExpr.Type.GetCacheKey());
        if (expr is IlStackAlloc stackAlloc)
            return new IlStackAllocExprDto(size: stackAlloc.Size.SerializeExpr(), type: stackAlloc.Type.GetCacheKey());
        if (expr is IlConvCastExpr convExpr)
            return new IlConvExprDto(convExpr.Type.GetCacheKey(), convExpr.Target.SerializeExpr(),
                convExpr.Type.GetCacheKey());
        if (expr is IlBoxExpr boxExpr)
            return new IlBoxExprDto(boxExpr.Type.GetCacheKey(), boxExpr.Target.SerializeExpr(),
                boxExpr.Type.GetCacheKey());
        if (expr is IlUnboxExpr unboxExpr)
            return new IlBoxExprDto(unboxExpr.Type.GetCacheKey(), unboxExpr.Target.SerializeExpr(),
                unboxExpr.Type.GetCacheKey());
        if (expr is IlIsInstExpr isInstExpr)
            return new IlIsInstExprDto(isInstExpr.Type.GetCacheKey(), isInstExpr.Target.SerializeExpr(),
                isInstExpr.Type.GetCacheKey());
        if (expr is IlManagedRef managedRef)
            return new IlManagedRefExprDto(value: managedRef.Value.SerializeExpr(),
                type: managedRef.Type.GetCacheKey());
        if (expr is IlManagedDeref managedDeref)
            return new IlManagedDerefExprDto(value: managedDeref.Value.SerializeExpr(),
                type: managedDeref.Type.GetCacheKey());
        if (expr is IlUnmanagedRef unmanagedRef)
            return new IlUnmanagedRefExprDto(value: unmanagedRef.Value.SerializeExpr(),
                type: unmanagedRef.Type.GetCacheKey());
        if (expr is IlUnmanagedDeref unmanagedDeref)
            return new IlUnmanagedDerefExprDto(value: unmanagedDeref.Value.SerializeExpr(),
                type: unmanagedDeref.Type.GetCacheKey());
        throw new Exception($"{expr} expr of type {expr.GetType()} serialization not yet supported");
    }

    private static IlConstDto SerializeConst(this IlConstant constant)
    {
        if (constant is IlUint8Const byteConst)
            return new IlByteConstDto(value: byteConst.Value, type: byteConst.Type.GetCacheKey());
        if (constant is IlInt32Const intConst)
            return new IlIntConstDto(value: intConst.Value, type: intConst.Type.GetCacheKey());
        if (constant is IlInt64Const longConst)
            return new IlLongConstDto(value: longConst.Value, type: longConst.Type.GetCacheKey());
        if (constant is IlFloatConst floatConst)
            return new IlFloatConstDto(value: floatConst.Value, type: floatConst.Type.GetCacheKey());
        if (constant is IlDoubleConst doubleConst)
            return new IlDoubleConstDto(value: doubleConst.Value, type: doubleConst.Type.GetCacheKey());
        if (constant is IlStringConst stringConst)
            return new IlStringConstDto(value: stringConst.Value, type: stringConst.Type.GetCacheKey());
        if (constant is IlNullConst nullConst)
            return new IlNullDto(nullConst.Type.GetCacheKey());
        if (constant is IlBoolConst boolConst)
            return new IlBoolConstDto(value: boolConst.Value, type: boolConst.Type.GetCacheKey());
        if (constant is IlTypeRef typeRef)
            return new IlTypeRefDto(referencedType: typeRef.ReferencedType.GetCacheKey(),
                type: typeRef.Type.GetCacheKey());
        if (constant is IlFieldRef fieldRef)
            return new IlFieldRefDto(field: fieldRef.Field.GetCacheKey(), type: fieldRef.Type.GetCacheKey());
        if (constant is IlMethodRef methodRef)
            return new IlMethodRefDto(method: methodRef.Method.GetCacheKey(), type: methodRef.Type.GetCacheKey());
        if (constant is IlArrayConst arrayConst)
            return new IlArrayConstDto(type: arrayConst.Type.GetCacheKey(),
                values: arrayConst.Values.Select(v => v.SerializeConst()).ToList());
        throw new Exception($"{constant} const of type {constant.GetType()} serialization not yet supported");
    }

    private static IlAttrDto SerializeAttr(this IlAttribute attr)
    {
        var namedFlatten = attr.NamedArguments.ToList();
        return new IlAttrDto(
            attrType: attr.Type.GetCacheKey(),
            ctorArgs: attr.ConstructorArguments.Select(arg => arg.Value.SerializeConst()).ToList(),
            namedArgsNames: namedFlatten.Select(p => p.Key).ToList(),
            namedArgsValues: namedFlatten.Select(p => p.Value.Value.SerializeConst()).ToList()
        );
    }
}

static class CacheKeyBuilder
{
    public static CacheKey GetCacheKey(this IlType type)
    {
        return new CacheKey(type.AsmToken, type.ModuleToken, type.MetadataToken);
    }

    public static CacheKey GetCacheKey(this IlField field)
    {
        return new CacheKey(field.DeclaringType.MetadataToken, field.ModuleToken, field.MetadataToken);
    }

    public static CacheKey GetCacheKey(this IlMethod method)
    {
        return new CacheKey(method.DeclaringType.MetadataToken, method.ModuleToken, method.MetadataToken);
    }
}
