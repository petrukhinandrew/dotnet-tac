using System.Diagnostics;

namespace TACBuilder.ReflectionUtils;

public static class FullNameResolver
{
    public enum Qualifier
    {
        Pointer, ByRef, Array
    }

    public static (Type, List<Qualifier>) GroundAndQualifiers(this Type type)
    {
        var qualifiers = new List<Qualifier>();
        var tmp = type;
        while (true)
        {
            if (tmp.IsPointer)
            {
                qualifiers.Add(Qualifier.Pointer);
                tmp = tmp.GetElementType()!;
                continue;
            }

            if (tmp.IsByRef) // byRefLike
            {
                qualifiers.Add(Qualifier.ByRef);
                tmp = tmp.GetElementType()!;
                continue;
            }

            if (tmp.IsArray) // SZArray? 
            {
                qualifiers.Add(Qualifier.Array);
                tmp = tmp.GetElementType()!;
                continue;
            }

            qualifiers.Reverse();
            return (tmp, qualifiers);
        }
        
    }

    public static Type AttachQualifiers(this Type type, List<Qualifier> qualifiers)
    {
        var tmp = type;
        foreach (var q in qualifiers)
        {
            switch (q)
            {
                case Qualifier.Pointer:
                {
                    tmp = tmp.MakePointerType();
                    break;
                }
                case Qualifier.ByRef:
                {
                    tmp = tmp.MakeByRefType();
                    break;
                }
                case Qualifier.Array:
                {
                    tmp = tmp.MakeArrayType();
                    break;
                }
            }
        }

        return tmp;
    }

    private static string AttachQualifiersToString(string fullName, List<Qualifier> qualifiers) =>
        qualifiers.Aggregate(fullName, (current, q) => current + q switch
        {
            Qualifier.Pointer => "*",
            Qualifier.ByRef => "&",
            Qualifier.Array => "[]",
            _ => throw new ArgumentException($"Unknown qualifier: {q}")
        });

    public static string ConstructFullName(this Type type)
    {
        var (t, q) = type.GroundAndQualifiers();
        var fullName = "";
        if (t.IsUnmanagedFunctionPointer || t.IsFunctionPointer)
        {
            // Console.WriteLine("kringi");
        }
        if (t.IsGenericType)
        {
            t = t.GetGenericTypeDefinition();
            fullName = AttachQualifiersToString(t.FullName!, q);
            
        } else if (t.IsGenericParameter)
        {
            var defn = t.DeclaringType!;
            if (t.IsGenericTypeParameter)
                defn = defn.GetGenericTypeDefinition();
            
            fullName = AttachQualifiersToString($"{defn.FullName!}!{t.Name}${t.GenericParameterPosition}", q);
        }
        // TODO: function pointer 
        else
        {
            fullName = AttachQualifiersToString(t.FullName ?? "null", q);
        }

        if (string.IsNullOrEmpty(fullName))
        {
            Console.Error.WriteLine($"Unable to resolve full name: {type.FullName}");
        }
        return fullName;
    }
}