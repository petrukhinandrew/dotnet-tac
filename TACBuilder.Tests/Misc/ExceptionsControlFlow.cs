using System.Diagnostics;

#pragma warning disable CS0162

namespace IntegrationTests;

public class ExceptionsControlFlow
{
    public static void Simple()
    {
        int a = 1;
        try
        {
            a++;
        }
        catch (Exception ex)
        {
            a += 2;
        }
        finally
        {
            a += 3;
        }
    }
    public static int TestWithHandlers(int x, int y)
    {
        int addition = 1;
        try
        {
            return x / y;
        }
        catch (OverflowException)
        {
            return addition + 100500;
        }
        catch (DivideByZeroException) when (x == 100)
        {
            return addition + 90;
        }
        finally
        {
            addition++;
        }

        return checked(x + y);
    }

    public static int TestWithHandlers1()
    {
        var res = 1;
        try
        {
            res += 1;
        }
        catch (DivideByZeroException)
        {
            res += 11;
        }
        catch (ArgumentException)
        {
            res += 12;
        }
        catch (NullReferenceException)
        {
            res += 10;
        }
        finally
        {
            res++;
        }

        return res;
    }

    public static int CatchRuntimeException(int x, int y)
    {
        try
        {
            return x / y;
        }
        catch (DivideByZeroException)
        {
            return -42;
        }
    }

    public static int SimpleFilterBlock(int x, int y)
    {
        try
        {
            return x / y;
        }
        catch (DivideByZeroException) when (x == 42)
        {
            return -42;
        }
    }

    private static bool ThrowNullReference()
    {
        throw new NullReferenceException();
    }

    public static int ExceptionInsideFilter(int x, int y)
    {
        var a = 0;
        try
        {
            return x / y;
        }
        catch (DivideByZeroException e) when (ThrowNullReference())
        {
            a = 3;
        }
        finally
        {
            a = 5;
        }

        return a;
    }

    public static int ReturnMinWithAssert(int x, int y)
    {
        Debug.Assert(x <= y);
        return x;
    }

    public static int TestWithAssert(int x, int y)
    {
        if (x < 0)
        {
            Debug.Assert(x <= y);
        }
        else
        {
            x = y;
            Debug.Assert(x > y);
        }

        return x;
    }

    public static int TestWithNestedFinallyHandlers(int x, int y)
    {
        int addition = 1;
        try
        {
        }
        finally
        {
            try
            {
            }
            finally
            {
                addition += 10;
            }

            addition += 100;
        }

        return addition;
    }

    public static void AnotherNestedFinally(int a)
    {
        try
        {
            a += 10;
        }
        finally
        {
            try
            {
                a += 100;
            }
            finally
            {
            }
        }
    }

    public static void FilterOrder(int x)
    {
        bool ThrowException()
        {
            throw new NullReferenceException();
        }

        var a = 0;
        try
        {
            try
            {
                a = 1;
                throw new Exception();
            }
            catch (Exception) when (a == 1 && (x & 0b0001) == 0)
            {
                a = 3;
            }
            catch (Exception) when (a == 1 && (x & 0b0010) == 0)
            {
                ThrowException();
            }
        }
        catch (Exception) when ((x & 0b0100) == 0 && ThrowException())
        {
            a = 4;
        }
        catch (Exception) when (a == 3 && (x & 0b1000) == 0)
        {
            ThrowException();
        }
    }


    public static void TwoFilters(int x)
    {
        bool ThrowException()
        {
            throw new NullReferenceException();
        }

        var a = 0;

        try
        {
            a = 1;
            throw new Exception();
        }
        catch (Exception) when (a == 1 && (x & 0b0001) == 0)
        {
            a = 3;
        }
        catch (Exception) when (a == 1 && (x & 0b0010) == 0)
        {
            ThrowException();
        }
    }

    public static int TryWith2Leaves(bool f)
    {
        int res = 0;
        try
        {
            if (f)
                return 100;
        }
        finally
        {
            res = 42;
        }

        res++;
        return res;
    }

    private static int Always42() => 42;
    private static int Always84() => Always42() * 2;

    public static int FilterInsideFinally(bool f)
    {
        int globalMemory = 0;
        try
        {
            globalMemory++;
        }
        finally
        {
            try
            {
                globalMemory += 10;
                throw new Exception();
            }
            catch (Exception) when ((globalMemory += 100) > 50 && f && Always42() == 42)
            {
                globalMemory += 1000;
            }

            globalMemory += 10000;
        }

        globalMemory += 100000;
        return globalMemory;
    }


    public static int NestedTryCatchFinally()
    {
        int globalMemory = 0;
        try
        {
            try
            {
                throw new Exception();
            }
            catch (Exception)
            {
                globalMemory = 42;
            }
            finally
            {
                globalMemory++;
            }
        }
        catch (Exception)
        {
            globalMemory = 12;
        }
        finally
        {
            globalMemory++;
        }

        return globalMemory;
    }

    public static int ConcreteThrow()
    {
        throw new NullReferenceException("Null reference!");
    }

    public static int ConcreteThrowInCall()
    {
        try
        {
            ConcreteThrow();
        }
        catch (Exception)
        {
            return 1;
        }

        return 2;
    }

    public static int CallInsideFinally(bool f)
    {
        int res = 0;
        try
        {
            res += Always42();
        }
        finally
        {
            if (f)
            {
                try
                {
                    res += Always42();
                }
                finally
                {
                    res += Always84();
                }
            }
        }

        return res;
    }

    private class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public static int NestedTryBlocks()
    {
        var res = 1;
        try
        {
            try
            {
                try
                {
                    using (var a = new Disposable())
                    {
                        throw null;
                    }
                }
                catch (NullReferenceException e)
                {
                    res += e.HResult;
                }
                catch
                {
                    res += 1;
                }
                finally
                {
                    res *= 2;
                }
            }
            catch
            {
                res += 2;
            }
            finally
            {
                res *= 3;
            }
        }
        catch
        {
            res += 3;
        }
        finally
        {
            res *= 4;
        }

        return res;
    }

    public static int NestedTryBlocks1(bool f)
    {
        var res = 1;
        try
        {
            try
            {
                try
                {
                    try
                    {
                        try
                        {
                            try
                            {
                                try
                                {
                                    using (var a = new Disposable())
                                    {
                                        throw null;
                                    }
                                }
                                catch (NullReferenceException e)
                                {
                                    res += e.HResult;
                                    if (f) throw null;
                                }
                                catch
                                {
                                    res += 1;
                                }
                                finally
                                {
                                    res *= 2;
                                }
                            }
                            catch
                            {
                                if (f) throw null;
                                res += 2;
                            }
                        }
                        catch
                        {
                            res += 3;
                        }
                    }
                    catch
                    {
                        res += 4;
                    }
                }
                finally
                {
                    res *= 6;
                }
            }
            finally
            {
                res *= 5;
                if (f) throw null;
            }
        }
        finally
        {
            res *= 4;
            if (f) throw null;
        }

        return res;
    }


    public static int NestedTryBlocks2()
    {
        var res = 1;
        try
        {
            try
            {
                try
                {
                    try
                    {
                        try
                        {
                            try
                            {
                                try
                                {
                                    using (var a = new Disposable())
                                    {
                                        throw null;
                                    }
                                }
                                catch (DivideByZeroException)
                                {
                                    res += 1;
                                }
                            }
                            catch (DivideByZeroException)
                            {
                                res += 2;
                            }
                        }
                        catch (NullReferenceException) when (ThrowNullReference())
                        {
                            res += 2;
                        }
                    }
                    catch (DivideByZeroException)
                    {
                        res += 3;
                    }
                }
                catch (DivideByZeroException)
                {
                    res += 3;
                }
            }
            catch (DivideByZeroException)
            {
                res += 4;
            }
        }
        catch (NullReferenceException)
        {
            res *= 100;
        }

        return res;
    }

    public static int NestedFinally()
    {
        var res = 0;
        try
        {
            throw null;
        }
        finally
        {
            try
            {
                res += 1;
            }
            finally
            {
                res += 2;
                try
                {
                    res += 3;
                }
                finally
                {
                    throw null;
                }
            }
        }

        return res;
    }

    public static int NestedFilter(int a)
    {
        var res = 0;
        try
        {
            throw null;
        }
        catch (NullReferenceException e) when (a > 0)
        {
            res += 1;
        }
        finally
        {
            res *= 2;
        }

        return res;
    }

    public static int NestedFilter1(int a)
    {
        var res = 0;
        try
        {
            throw null;
        }
        catch (NullReferenceException e) when (NestedFilter(a) > 0)
        {
            res += 1;
        }
        finally
        {
            res *= 2;
        }

        return res;
    }

    public static int NestedTryFinally()
    {
        var res = 0;
        try
        {
            try
            {
                ThrowNullReference();
            }
            finally
            {
                res += 3;
            }
        }
        catch (NullReferenceException)
        {
            res += 1;
        }
        finally
        {
            res *= 2;
        }

        return res;
    }

    public static void ForcedFault()
    {
        using var d = new Disposable();
        throw null;
    }
}