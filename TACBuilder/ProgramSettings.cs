namespace Usvm.IL.Parser;
class ProgramSettings
{
    public string DllPath;
    public List<string> Methods;
    public ProgramSettings(string dllPath, List<string> methods)
    {
        if (dllPath.StartsWith('/'))
            DllPath = dllPath;
        else 
            DllPath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + dllPath;
        Methods = methods;
    }
}