using Usvm.IL.TypeSystem;

namespace Usvm.IL.TACBuilder;

public class EvaluationStack<T>
{
    public EvaluationStack() : this([])
    {
    }

    private List<T> _data;
    private int _virtualStackPtr = -1;

    public EvaluationStack(IEnumerable<T> array)
    {
        _data = array.ToList();
        _virtualStackPtr = array.Count() - 1;
    }

    public int Count => _data.Count;

    public T Pop(bool virtually = false)
    {
        if (virtually) return _data[_virtualStackPtr--];
        T ret = _data.Last();
        _data.RemoveAt(_data.Count - 1);
        _virtualStackPtr--;
        return ret;
    }

    public void Push(T value)
    {
        _data.Add(value);
        _virtualStackPtr++;
    }

    /// <summary>
    /// resets virtual stack pointer
    /// </summary>
    /// <returns> true if ptr chagned</returns>
    public bool ResetVirtualStack()
    {
        if (_virtualStackPtr == _data.Count - 1) return false;
        _virtualStackPtr = _data.Count - 1;
        return true;
    }

    public void Clear()
    {
        _data.Clear();
        _virtualStackPtr = 0;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _data.CopyTo(array, arrayIndex);
    }

    public void CloneFrom(EvaluationStack<T> source)
    {
        _data.AddRange(source._data);
        _virtualStackPtr = _data.Count - 1;
    }
}