// using TACBuilder.ILReflection;
//
// namespace TACBuilder.ILTAC.TypeSystem;
//
// public abstract class ILPointer(Type reflectedType, ILType targetType) : ILType
// {
//     public ILType TargetType = targetType;
//     public Type ReflectedType => reflectedType;
//     public abstract override string ToString();
// }
//
// class ILManagedPointer(Type reflectedType, ILType targetType) : ILPointer(reflectedType, targetType)
// {
//     public override string ToString() => TargetType.ToString() + "&";
//
//     public override bool Equals(object? obj)
//     {
//         return obj is ILManagedPointer pt && TargetType == pt.TargetType;
//     }
//
//     public override int GetHashCode()
//     {
//         return ToString().GetHashCode();
//     }
// }
//
// public class ILUnmanagedPointer(Type reflectedType, ILType targetType) : ILPointer(reflectedType, targetType)
// {
//     public override string ToString() => TargetType.ToString() + "*";
//
//     public override bool Equals(object? obj)
//     {
//         return obj is ILUnmanagedPointer pt && TargetType == pt.TargetType;
//     }
//
//     public override int GetHashCode()
//     {
//         return ToString().GetHashCode();
//     }
// }
