using System;

public class ArrayWrap
{
    public readonly Type ValueType;
    public readonly int Length;
    readonly object?[] _values;

    public ArrayWrap(Type valueType, int size)
    {
        ValueType = valueType;
        Length = size;
        _values = new object?[size];
        if (valueType.IsValueType)
        {
            for (int i = 0; i < size; i++)
            {
                _values[i] = Activator.CreateInstance(valueType);
            }
        }
    }

    public object? this[int index]
    {
        get
        {
            if (index < 0 || index >= Length) throw new IndexOutOfRangeException($"Index {index} is out of bounds for array of size {Length}.");
            return _values[index];
        }
        set
        {
            if (index < 0 || index >= Length) throw new IndexOutOfRangeException($"Index {index} is out of bounds for array of size {Length}.");
            _values[index] = value;
        }
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var value in _values)
        {
            hash = hash * 31 + (value?.GetHashCode() ?? 0);
        }
        return hash;
    }

    public override bool Equals(object? obj) =>
        obj is ArrayWrap other &&
        ValueType == other.ValueType &&
        _values.SequenceEqual(other._values);
}
