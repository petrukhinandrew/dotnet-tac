namespace TACBuilder.Tests.Approximations;

public class Approximated
{
    private string OriginalStringField;
    private static int OriginalIntField;

    public int IntMethod()
    {
        return 1 + 1;
    }

    public static string StringMethod()
    {
        return OriginalIntField.ToString();
    }
}