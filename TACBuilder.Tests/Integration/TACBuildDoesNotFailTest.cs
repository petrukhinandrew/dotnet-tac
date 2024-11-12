using Xunit.Abstractions;

namespace TACBuilder.Tests;

public class TACBuildDoesNotFailTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TACBuildDoesNotFailTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private string getPublishPathFor(string asmFileName)
    {
        string cur = Environment.CurrentDirectory;
        // string leaveDebugExtra = Path.Combine("..", "..", "..");
        // cur = Path.Combine(cur, leaveDebugExtra);
        // string publishPath = Path.Combine("Release", "net8.0", "publish");
        // cur = Path.Combine(cur, publishPath);
        // string platformSpecificPath = Environment.OSVersion.Platform == PlatformID.Unix ? "linux-x64" : "windows";
        cur = Path.Combine(cur, asmFileName);
        return cur;
    }

    [Fact]
    public void WithTestsAsm()
    {
        try
        {
            var path = getPublishPathFor("TACBuilder.Tests.dll");
            AppTacBuilder.IncludeRootAsm(path);
            _testOutputHelper.WriteLine(path);
            _testOutputHelper.WriteLine(Environment.CurrentDirectory);
            var appTacBuilder =
                new AppTacBuilder();
        }
        catch (Exception ex)
        {
            Assert.True(false, ex.Message);
        }
    }

    [Fact]
    public void WithVector128()
    {
        try
        {
            var path = getPublishPathFor("TACBuilder.Tests.dll");
            AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "Vector128");
            var appTacBuilder =
                new AppTacBuilder();
        }
        catch (Exception ex)
        {
            Assert.True(false, ex.Message);
        }
        Assert.True(true);
    }
}
