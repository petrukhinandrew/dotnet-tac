using System.Reflection;
using System.Reflection.Emit;
using TACBuilder.ILReflection;

namespace TACBuilder.Utils;

class CalliDynamicAsmBuilder
{
    public AssemblyBuilder Builder;
    public AsmLoadContext Context = new();
    public ModuleBuilder Module;
    public List<TypeBuilder> Types = new();

    public CalliDynamicAsmBuilder()
    {
        var scope = Context.EnterContextualReflection();
        using (scope)
        {
            Builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.RunAndCollect);
            Module = Builder.DefineDynamicModule("MainModule");
        }
    }

    public Type AddConstructibleType(TypeBuilder typeBuilder)
    {
        // var field = typeBuilder.DefineField("Field", typeof(int), FieldAttributes.Public | FieldAttributes.InitOnly);
        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);
        var ctorIl = ctor.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor([])!);
        // ctorIl.Emit(OpCodes.Ldarg_0);
        // ctorIl.Emit(OpCodes.Ldarg_1);
        // ctorIl.Emit(OpCodes.Stfld, field);
        return typeBuilder.CreateType();
    }

    public Type AddType(TypeBuilder typeBuilder)
    {
        var dummyMethod = typeBuilder.DefineMethod("Dummy", MethodAttributes.Public | MethodAttributes.Static);
        AddDummyMethod(dummyMethod);
        var simpleCalliBuilder =
            typeBuilder.DefineMethod("SimpleMethodCalli", MethodAttributes.Public | MethodAttributes.Static);
        AddMethod(simpleCalliBuilder, dummyMethod);
        var ctorCaller = typeBuilder.DefineMethod("CtorCaller", MethodAttributes.Public);
        AddCtorCallerMethod(ctorCaller);
        return typeBuilder.CreateType();
    }

    private void AddDummyMethod(System.Reflection.Emit.MethodBuilder methodBuilder)
    {
        methodBuilder.SetReturnType(typeof(double));
        methodBuilder.SetParameters(typeof(int));
        var ilBuilder = methodBuilder.GetILGenerator();
        ilBuilder.Emit(OpCodes.Ldarg_0);
        ilBuilder.Emit(OpCodes.Ret);
    }

    private void AddMethod(System.Reflection.Emit.MethodBuilder methodBuilder,
        System.Reflection.Emit.MethodBuilder callTarget)
    {
        var callTargetSig = Module.GetSignatureMetadataToken(
            SignatureHelper.GetMethodSigHelper(Module, typeof(double), [typeof(int)])
        );
        var ilBuilder = methodBuilder.GetILGenerator();
        // ilBuilder.DeclareLocal()
        methodBuilder.SetReturnType(typeof(double));
        ilBuilder.Emit(OpCodes.Ldc_I4_4);
        ilBuilder.Emit(OpCodes.Ldftn, callTarget);
        ilBuilder.Emit(OpCodes.Calli, callTargetSig);
        ilBuilder.Emit(OpCodes.Ret);
    }

    private void AddCtorCallerMethod(System.Reflection.Emit.MethodBuilder methodBuilder)
    {
        var il = methodBuilder.GetILGenerator();
        var ctor = Module.GetType("Constructible")!.GetConstructors().Single();
        var ctorSig = Module.GetSignatureMetadataToken(SignatureHelper.GetMethodSigHelper(Module, null, [typeof(int)]));
        il.DeclareLocal(methodBuilder.Module.GetType("Constructible")!);
        il.Emit(OpCodes.Ldloca, 0);
        il.Emit(OpCodes.Ldftn, ctor);
        il.Emit(OpCodes.Calli, ctorSig);
        il.Emit(OpCodes.Ret);
    }

    public Assembly Build()
    {
        var constructibleType = AddConstructibleType(Module.DefineType("Constructible", TypeAttributes.Public));
        AddType(Module.DefineType("type1"));
        return Builder;
    }
}
