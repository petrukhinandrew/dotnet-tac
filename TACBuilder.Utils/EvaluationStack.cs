namespace TACBuilder.Utils;

public class EvaluationStack<T>(IEnumerable<T> collection) where T : notnull
{
    private Stack<T> _stack = new(collection);
    public int Count => _stack.Count;

    public EvaluationStack() : this([])
    {
    }

    public static EvaluationStack<T> CopyOf(EvaluationStack<T> stack)
    {
        T[] copy = new T[stack.Count];
        stack.CopyTo(copy, 0);
        return new EvaluationStack<T>(copy);
    }

    private void CopyTo(T[] array, int index)
    {
        _stack.CopyTo(array, index);
    }

    public void Push(T item)
    {
        _stack.Push(item);
    }

    public T Pop()
    {
        return _stack.Pop();
    }

    public void Clear()
    {
        _stack.Clear();
    }

    public bool SequenceEqual(EvaluationStack<T> other)
    {
        return _stack.SequenceEqual(other._stack);
    }
}