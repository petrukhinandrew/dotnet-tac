namespace TACBuilder.Tests.Approximations;

[AttributeUsage(AttributeTargets.Class)]
public class ApproximationAttribute : Attribute
{
    public string OriginalType { get; set; }
}

[Approximation(OriginalType = "TACBuilder.Tests.Approximations.Approximated")]
class TestClassApproximaiton
{
    public string OriginalStringField;

    private int IntMethod()
    {
        return 2;
    }
}