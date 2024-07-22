namespace Usvm.IL.Parser;
class ParserSettings
{
    public string DllPath;
    public List<string> Methods;
    public ParserSettings(string dllPath, List<string> methods)
    {
        if (dllPath.StartsWith('/'))
            DllPath = dllPath;
        else 
            DllPath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + dllPath;
        Methods = methods;
    }
}