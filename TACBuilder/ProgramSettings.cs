namespace Usvm.IL.Parser;
class ProgramSettings
{
    public string DllPath;
    public List<string> Methods;
    public ProgramSettings(string dllPath, List<string> methods)
    {
        // if (dllPath.StartsWith('/'))
            // DllPath = dllPath;
        // else 
        DllPath =  "/home/andrew/Documents/dotnet-tac/TACBuilder.Tests/bin/Release/net8.0/linux-x64/publish/TACBuilder.Tests.dll";//Environment.CurrentDirectory + Path.DirectorySeparatorChar + dllPath;
        Methods = methods;
    }
}