using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace TACBuilder.Tests.Misc;

static class TestUtils
{
    public static bool ConstFalse() => false;
    public static bool ConstTrue() => true;
    public static bool Throw() => throw new Exception("indirect throw");
}

public class LogUtilsFixture(ITestOutputHelper testOutputHelper)
{
    public void LogTry([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
        [System.Runtime.CompilerServices.CallerMemberName]
        string methodName = "", string message = "")
    {
        testOutputHelper.WriteLine($"try at {lineNumber} of {methodName}: {message}");
    }

    public void LogCatch([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
        [System.Runtime.CompilerServices.CallerMemberName]
        string methodName = "", string message = "")
    {
        testOutputHelper.WriteLine($"catch at {lineNumber} of {methodName}: {message}");
    }

    public void LogFilter([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
        [System.Runtime.CompilerServices.CallerMemberName]
        string methodName = "", string message = "")
    {
        testOutputHelper.WriteLine($"filter at {lineNumber} of {methodName}: {message}");
    }

    public void LogFinally([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
        [System.Runtime.CompilerServices.CallerMemberName]
        string methodName = "", string message = "")
    {
        testOutputHelper.WriteLine($"finally at {lineNumber} of {methodName}: {message}");
    }
}

public class Finally(ITestOutputHelper testOutputHelper)
{
    private LogUtilsFixture logUtils = new(testOutputHelper);

    [Fact]
    public void Workaround()
    {
        try
        {
            try
            {
                throw null;
            }
            catch
            {
                logUtils.LogCatch();
            }
            finally
            {
                logUtils.LogFinally();
            }

            return;
        }
        catch (Exception ex) when (TestUtils.ConstTrue())
        {
            logUtils.LogCatch();
        }
        finally
        {
            logUtils.LogFinally();
        }
    }

    [Fact]
    public void WrapThrowingHandled()
    {
        try
        {
            InnerThrowingHandled();
        }
        catch (Exception ex) when (TestUtils.ConstTrue())
        {
            logUtils.LogFilter();
        }
        catch
        {
            logUtils.LogCatch();
        }
        finally
        {
            logUtils.LogFinally();
        }
    }

    private void InnerThrowingHandled()
    {
        try
        {
            throw null;
        }
        catch
        {
            logUtils.LogCatch();
            throw;
        }
        finally
        {
            logUtils.LogFinally();
        }
    }

    [Fact]
    public void WrapThrowingUnhandled()
    {
        try
        {
            InnerThrowingUnhandled();
        }
        catch (Exception ex) when (TestUtils.ConstTrue())
        {
            logUtils.LogFilter();
        }
        catch (Exception e)
        {
            logUtils.LogCatch();
        }
        finally
        {
            logUtils.LogFinally();
        }
    }

    private void InnerThrowingUnhandled()
    {
        throw null;
    }
}

public class ThrowingFilter(ITestOutputHelper testOutputHelper)
{
    LogUtilsFixture logUtils = new(testOutputHelper);

    [Fact]
    public void TestCase()
    {
        try
        {
            try
            {
                throw null;
            }
            catch (Exception e) when (TestUtils.ConstTrue())
            {
                logUtils.LogFilter();
                throw null;
            }
            finally
            {
                logUtils.LogFinally();
            }
        }
        catch (Exception e) when (TestUtils.ConstTrue())
        {
            logUtils.LogFilter();
        }
        finally
        {
            logUtils.LogFinally();
        }
    }

    [Fact]
    private void WrapperCatching()
    {
        try
        {
            WrapperNotCatching();
        }
        catch (Exception e) when (TestUtils.ConstTrue())
        {
            logUtils.LogFilter();
        }
        finally
        {
            logUtils.LogFinally();
        }
    }

    private void WrapperNotCatching()
    {
        try
        {
            InnerThrowing();
        }
        catch (Exception e) when (TestUtils.ConstFalse())
        {
            logUtils.LogFilter();
        }
        finally
        {
            logUtils.LogFinally();
        }
    }

    private void InnerThrowing()
    {
        try
        {
            throw null;
        }
        finally
        {
            logUtils.LogFinally();
        }
    }

    [Fact]
    public void TestCase2()
    {
        try
        {
            try
            {
                try
                {
                    throw null;
                }
                catch (Exception e) when (TestUtils.Throw())
                {
                    logUtils.LogFilter(message: e.Message);
                }

                finally
                {
                    logUtils.LogFinally();
                }
            }
            catch (Exception e) when (TestUtils.ConstFalse())
            {
                logUtils.LogCatch(message: e.Message);
            }
            finally
            {
                logUtils.LogFinally();
            }
        }
        catch (Exception e) when (TestUtils.ConstTrue())
        {
            logUtils.LogFilter();
        }
        catch
        {
            logUtils.LogCatch();
        }
    }
}

public class CatchOrdering(ITestOutputHelper testOutputHelper)
{
    private LogUtilsFixture logUtils = new(testOutputHelper);

    [Fact]
    void Kek()
    {
        try
        {
            throw null;
        }
        catch (NullReferenceException e)
        {
            logUtils.LogCatch();
            throw;
        }
        catch (Exception e)
        {
            logUtils.LogCatch();
        }
        finally
        {
            logUtils.LogFinally();
        }
    }
}