using System.Diagnostics;
using TACBuilder.ILMeta;

namespace TACBuilder;

public class AppTacBuilder
{
    public AppTacBuilder(string rootAssemblyPath, Stream? serializationStream = null)
    {
        Debug.Assert(File.Exists(rootAssemblyPath));
        // FilterMethodsFromSingleMSCoreLibType(rootAssemblyPath, "FileSystemEntry");
        // FilterMethodsFromSingleMSCoreLibType(rootAssemblyPath, "AhoCorasick");
        // FilterMethodsFromSingleMSCoreLibType(rootAssemblyPath, "Int32");
        FilterMethodsFromRootAsm(rootAssemblyPath);
        MetaBuilder.AddMethodFilter(method => method.Name == "LeaveFromTry");
        // MetaBuilder.AddTypeFilter(type => type.Name == "CustomAttrUsage");
        var rootAssemblyMeta = MetaBuilder.BuildFrom(rootAssemblyPath);

        foreach (var asm in MetaBuilder.GetAssemblies())
        {
            var tacAssembly = new AssemblyTacBuilder(asm).Build();
            if (serializationStream != null)
                tacAssembly.SerializeTo(serializationStream);
        }
    }

    public static void FilterMethodsFromRootAsm(string rootAssemblyPath)
    {
        MetaBuilder.AddAssemblyFilter(assembly => assembly.Location == rootAssemblyPath);
        MetaBuilder.AddTypeFilter(type =>
            type.Assembly.Location == rootAssemblyPath);
        MetaBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath);
    }

    public static void FilterSingleMethodFromRootAsm(string rootAssemblyPath, string methodName)
    {
        MetaBuilder.AddAssemblyFilter(assembly => assembly.Location == rootAssemblyPath);
        MetaBuilder.AddTypeFilter(type =>
            type.Assembly.Location == rootAssemblyPath);
        MetaBuilder.AddMethodFilter(method =>
            (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath);
        MetaBuilder.AddTypeFilter(method => method.Name.StartsWith(methodName));
    }

    public static void FilterMethodsFromSingleMSCoreLibType(string rootAssemblyPath, string typeNamePart)
    {
        MetaBuilder.AddAssemblyFilter(assembly =>
            assembly.GetName().FullName.StartsWith("System.Private.CoreLib") || assembly.Location == rootAssemblyPath);
        MetaBuilder.AddTypeFilter(type =>
            type.Assembly.GetName().FullName.StartsWith("System.Private.CoreLib") ||
            type.Assembly.Location == rootAssemblyPath);
        MetaBuilder.AddTypeFilter(type => type.Name.StartsWith(typeNamePart));
        // MetaBuilder.AddMethodFilter(method =>
        //     (method.ReflectedType ?? method.DeclaringType)!.Assembly.Location == rootAssemblyPath);
        MetaBuilder.AddMethodFilter(method => method.DeclaringType!.Name.StartsWith(typeNamePart));
    }
}
