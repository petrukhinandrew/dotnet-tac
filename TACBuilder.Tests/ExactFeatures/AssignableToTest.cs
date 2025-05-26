namespace TACBuilder.Tests.ExactFeatures;

public interface IRelated;
public class Simple
{
    public class Base: IRelated;
}

public class UnrelatedType;

public interface IRelated2;

public class MultiFaceType : IRelated, IRelated2;
