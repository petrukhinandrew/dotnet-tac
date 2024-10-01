namespace TACBuilder.Utils;

public static class NamingUtil
{
    public static string LocalVar(int idx)
    {
        return $"local${idx}";
    }

    public static string TempVar(int idx)
    {
        return $"temp${idx}";
    }

    public static string ArgVar(int idx)
    {
        return $"arg${idx}";
    }

    public static string ErrVar(int idx)
    {
        return $"err${idx}";
    }

    public static string MergedVar(int idx)
    {
        return $"merged${idx}";
    }

    public static int TakeIndexFrom(string name)
    {
        return int.Parse(name.Split("$")[1]);
    }
}