using org.jacodb.api.net.generated.models;
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
                dto = new IlTypeDto(type.GetCacheKey(), type.Name,
                    type.GenericArgs.Select(a => a.GetCacheKey()).ToList(), type.IsGenericParameter, type.IsValueType,
                    type.IsManaged);
            }
            else if (instance is IlField field)
            {
                dto = new IlFieldDto(field.GetCacheKey(), field.DeclaringType.GetCacheKey(), field.Type.GetCacheKey(),
                    field.IsStatic, field.Name);
            }
            else if (instance is IlMethod method)
            {
                dto = new IlMethodDto(
                    id: method.GetCacheKey(),
                    declType: method.DeclaringType?.GetCacheKey(),
                    returnType: method.ReturnType?.GetCacheKey(),
                    name: method.Name,
                    parameters: method.Parameters
                        .Select(p => new IlParameterDto(p.Position, p.Type.GetCacheKey(), p.Name, null)).ToList(),
                    resolved: method.IsConstructed,
                    locals: method.LocalVars.Select(v =>
                        new IlLocalVarDto(type: v.Type.GetCacheKey(), index: v.Index, isPinned: v.IsPinned)).ToList(),
                    temps: method.Temps.Values.Select(v => new IlTempVarDto(index: v.Index, type: v.Type.GetCacheKey()))
                        .ToList(),
                    errs: method.Errs.Select(v =>
                        new IlErrVarDto(type: v.Type.GetCacheKey(), index: v.Index)).ToList(),
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
        {
            return
                new IlCallStmtDto(
                    (IlCallDto)callStmt.Call.SerializeExpr()); // itll be always like this, should be decided by cache
        }

        if (stmt is IlReturnStmt returnStmt)
        {
            return new IlReturnStmtDto(returnStmt.RetVal?.SerializeExpr());
        }

        if (stmt is IlGotoStmt gotoStmt)
        {
            return new IlGotoStmtDto(gotoStmt.Target);
        }

        if (stmt is IlIfStmt ifStmt)
        {
            return new IlIfStmtDto(ifStmt.Target, ifStmt.Condition.SerializeExpr());
        }

        if (stmt is ILEHStmt)
        {
            return new IlEhStmtDto();
        }

        throw new Exception($"{stmt} stmt serialization not yet supported");
    }

    private static IlExprDto SerializeExpr(this IlExpr expr)
    {
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
        if (expr is IlByteConst byteConst)
            return new IlByteConstDto(value: byteConst.Value, type: byteConst.Type.GetCacheKey());
        if (expr is IlIntConst intConst)
            return new IlIntConstDto(value: intConst.Value, type: intConst.Type.GetCacheKey());
        if (expr is IlLongConst longConst)
            return new IlLongConstDto(value: longConst.Value, type: longConst.Type.GetCacheKey());
        if (expr is IlFloatConst floatConst)
            return new IlFloatConstDto(value: floatConst.Value, type: floatConst.Type.GetCacheKey());
        if (expr is IlDoubleConst doubleConst)
            return new IlDoubleConstDto(value: doubleConst.Value, type: doubleConst.Type.GetCacheKey());
        if (expr is IlStringConst stringConst)
            return new IlStringConstDto(value: stringConst.Value, type: stringConst.Type.GetCacheKey());
        if (expr is IlNullConst nullConst)
            return new IlNullDto(nullConst.Type.GetCacheKey());
        if (expr is IlBoolConst boolConst)
            return new IlBoolConstDto(value: boolConst.Value, type: boolConst.Type.GetCacheKey());
        if (expr is IlTypeRef typeRef)
            return new IlTypeRefDto(referencedType: typeRef.ReferencedType.GetCacheKey(),
                type: typeRef.Type.GetCacheKey());
        if (expr is IlFieldRef fieldRef)
            return new IlFieldRefDto(field: fieldRef.Field.GetCacheKey(), type: fieldRef.Type.GetCacheKey());
        if (expr is IlMethodRef methodRef)
            return new IlMethodRefDto(method: methodRef.Method.GetCacheKey(), type: methodRef.Type.GetCacheKey());
        if (expr is IlUnaryOperation unaryOp)
            return new IlUnaryOpDto(type: unaryOp.Type.GetCacheKey(), operand: unaryOp.Operand.SerializeExpr());
        if (expr is IlBinaryOperation binaryOp)
            return new IlBinaryOpDto(type: binaryOp.Type.GetCacheKey(), lhs: binaryOp.Lhs.SerializeExpr(),
                rhs: binaryOp.Rhs.SerializeExpr());
        if (expr is IlInitExpr initExpr)
            return new IlInitExprDto(initExpr.Type.GetCacheKey());
        if (expr is IlNewExpr ctorExpr)
            return new IlNewExprDto(ctorExpr.Args.Select(SerializeExpr).ToList(),
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
        if (expr is IlConvExpr convExpr)
            return new IlConvExprDto(convExpr.Type.GetCacheKey(), convExpr.Target.SerializeExpr(),
                convExpr.Type.GetCacheKey());
        if (expr is IlBoxExpr boxExpr)
            return new IlBoxExprDto(boxExpr.Type.GetCacheKey(), boxExpr.Target.SerializeExpr(),
                boxExpr.Type.GetCacheKey());
        if (expr is IlUnboxExpr unboxExpr)
            return new IlBoxExprDto(unboxExpr.Type.GetCacheKey(), unboxExpr.Target.SerializeExpr(),
                unboxExpr.Type.GetCacheKey());
        if (expr is IlCastClassExpr castClassExpr)
            return new IlCastClassExprDto(castClassExpr.Type.GetCacheKey(), castClassExpr.Target.SerializeExpr(),
                castClassExpr.Type.GetCacheKey());
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
