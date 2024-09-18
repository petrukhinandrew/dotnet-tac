namespace TACBuilder.ILMeta;

public class Assembly
{
    // new gets either path or name <= resolving in constructor 
    private List<Type> types;
    private string path;
    private string name;
}