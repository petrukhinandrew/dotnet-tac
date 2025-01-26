using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit.Abstractions;
using Xunit.Sdk;

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
    public void InsertAsAdd()
    {
        var l = new List<int>();
        l.Insert(0, 0);
        l.Insert(1, 1);
    }

    public void SimpleTryCatchFinally()
    {
        try
        {
            logUtils.LogTry();
        }
        catch (Exception e)
        {
            logUtils.LogCatch(message: e.Message);
        }
        // target
        finally
        {
            logUtils.LogFinally();
        }
    }

    public void MultipleEndFinally()
    {
        try
        {
        }
        finally
        {
            var a = Math.Ceiling(1.0 + 4.0);
            if (a < 5.0)
            {
                a -= 1;
            }
            else
            {
                a += 1;
            }
        }
    }

    [Fact]
    public void ThrowInFinally()
    {
        int x = 1;
        try
        {
            try
            {
                throw new NullReferenceException("A");
            }
            finally
            {
                logUtils.LogFinally(message: "B not thrown Yet");
                throw new Exception("B");
            }
        }
        catch (NullReferenceException ex)
        {
            logUtils.LogCatch(message: ex.Message);
        }
        catch (Exception ex)
        {
            logUtils.LogCatch(message: ex.Message);
        }
    }

    [Fact]
    public void ThrowInTryUnhandled()
    {
        Assert.ThrowsAny<NullReferenceException>(() =>
        {
            try
            {
                throw null;
            }
            finally
            {
                logUtils.LogFinally();
            }
        });
    }

    [Fact]
    public void ThrowInTryCaught()
    {
        try
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
        catch
        {
            logUtils.LogCatch();
        }
    }

    [Fact]
    public void ReturnInTry()
    {
        try
        {
            logUtils.LogTry();
            return;
        }
        catch
        {
            logUtils.LogCatch();
        }
        finally
        {
            logUtils.LogFinally();
        }

        logUtils.LogCatch();
    }

    public void NotNested()
    {
        try
        {
            logUtils.LogTry();
        }
        finally
        {
            logUtils.LogFinally();
        }
    }

    public void NestedInTry()
    {
        try
        {
            logUtils.LogTry();
            try
            {
                logUtils.LogTry();
            }
            finally
            {
                logUtils.LogFinally();
            }

            logUtils.LogTry();
        }
        finally
        {
            logUtils.LogFinally();
        }
    }

    public void NestedInFinally()
    {
        try
        {
            logUtils.LogTry();
            // leave
        }
        finally
        {
            logUtils.LogFinally();
            try
            {
                logUtils.LogTry();
                // leave X
            }
            finally
            {
                logUtils.LogFinally();
            }

            logUtils.LogFinally();
        }
        // finaly
        // { }
        // X
    }

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
            try
            {
                throw null;
            }
            catch (Exception e)
            {
                logUtils.LogCatch();
            }
            finally
            {
                // logUtils.LogFinally();
            }
        }
        finally
        {
            logUtils.LogFinally();
        }
    }
}

public class FinallyMultipleExit(ITestOutputHelper testOutputHelper)
{
    private LogUtilsFixture logUtils = new(testOutputHelper);

    [Fact]
    public void Simple()
    {
        try
        {
            if (TestUtils.ConstTrue())
            {
                logUtils.LogTry();
                return;
            }
            else if (TestUtils.ConstFalse())
            {
                logUtils.LogTry();
                return;
            }
            else
            {
                return;
            }
        }
        finally
        {
            if (TestUtils.ConstTrue())
            {
                throw null;
            }
            else
            {
                logUtils.LogFinally();
            }
        }
    }
}