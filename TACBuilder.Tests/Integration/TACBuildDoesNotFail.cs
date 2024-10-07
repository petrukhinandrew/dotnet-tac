namespace TACBuilder.Tests;

public class TACBuildDoesNotFail
{
    [Fact]
    public void WithTestsAsm()
    {
        try
        {
            var path = Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll");
            AppTacBuilder.FilterMethodsFromRootAsm(path);
            var appTacBuilder =
                new AppTacBuilder(path);
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
            var path = Path.Combine(Environment.CurrentDirectory, "TACBuilder.Tests.dll");
            AppTacBuilder.FilterMethodsFromSingleMSCoreLibType(path, "Vector128");
            var appTacBuilder =
                new AppTacBuilder(path);
        }
        catch (Exception ex)
        {
            Assert.True(false, ex.Message);
        }
    }
}
