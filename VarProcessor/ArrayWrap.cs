using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

public class ArrayWrap
{
    public readonly Type ElementType;
    public readonly TypeDB.IntType? ElementIntType;
    public readonly int Length;
    readonly object?[] _values;

    public ArrayWrap(TypeSyntax valueTypeSyntax, int size)
    {
        ElementType = TypeDB.ToSystemType(valueTypeSyntax) ?? typeof(UnknownValue);
        Length = size;
        _values = new object?[size];
        if (ElementType.IsValueType)
        {
            for (int i = 0; i < size; i++)
            {
                _values[i] = Activator.CreateInstance(ElementType);
            }
        }

        ElementIntType = TypeDB.TryFind(ElementType);
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

    public ArrayWrap Cast(TypeSyntax toTypeSyntax)
    {
        if (toTypeSyntax is not ArrayTypeSyntax arrayTypeSyntax)
            throw new ArgumentException("Cannot cast to non-array type.", nameof(toTypeSyntax));

        if (ElementIntType is null)
            throw new InvalidOperationException($"Cannot cast array of type {ElementType.Name} to {arrayTypeSyntax}");

        if (ElementIntType.ByteSize != TypeDB.SizeOf(arrayTypeSyntax.ElementType))
            throw new InvalidOperationException($"Cannot cast array of type {ElementType.Name} to {arrayTypeSyntax}");

        return this;
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
        ElementType == other.ElementType &&
        _values.SequenceEqual(other._values);

    public override string ToString()
    {
        if (Length <= 5)
            return $"ArrayWrap<{ElementType.Name}>[{Length}] {{ {string.Join(", ", _values.Select(v => v?.ToString() ?? "null"))} }}";

        return $"ArrayWrap<{ElementType.Name}>[{Length}] {{ {string.Join(", ", _values.Take(5).Select(v => v?.ToString() ?? "null"))}, … }}";
    }
}
