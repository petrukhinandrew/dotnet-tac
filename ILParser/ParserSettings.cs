namespace Usvm.IL.Parser;
class ParserSettings
{
    public string DllPath;
    public List<string> Methods;
    public ParserSettings(string dllPath, List<string> methods)
    {
        DllPath = dllPath;
        Methods = methods;
    }
}