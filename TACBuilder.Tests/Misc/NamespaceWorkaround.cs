namespace TACBuilder.Tests.Misc
{
    public class NamespaceWorkaround
    {
    }
}

public class NoNamespaceType
{
    public int DeclMethod()
    {
        var local = new
        {
            LocalString = "LocalString"
        };
        return 0;
    }
}