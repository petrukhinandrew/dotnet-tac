namespace Usvm.IL.Parser;
public static class Logger
{
    public static void PrintSeparator()
    {
        Console.WriteLine("==========");
    }
    public static string LocalVarName(int idx)
    {
        return string.Format("local${0}", idx);
    }
    public static string TempVarName(int idx)
    {
        return string.Format("temp${0}", idx);
    }
    public static string ArgVarName(int idx)
    {
        return string.Format("arg${0}", idx);
    }
    public static string ErrVarName(int idx)
    {
        return string.Format("err${0}", idx);
    }
    public static int NameToIndex(string name)
    {
        return int.Parse(name.Split("$")[1]);
    }
}