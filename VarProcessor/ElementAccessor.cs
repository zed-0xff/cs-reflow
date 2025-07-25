abstract class ElementAccessorBase
{
    protected readonly ArrayWrap _array;

    protected ElementAccessorBase(ArrayWrap array)
    {
        _array = array ?? throw new ArgumentNullException(nameof(array), "array cannot be null");
    }

    abstract public object? GetValue();
    abstract public object? SetValue(object? value);

    public UnknownValueBase CreateUnknownElement() => UnknownValue.Create(_array.ElementIntType);
}

class ElementAccessor : ElementAccessorBase
{
    public readonly int Index;

    public ElementAccessor(ArrayWrap array, int index) : base(array)
    {
        Index = index;
        if (index < 0 || index >= array.Length)
            throw new IndexOutOfRangeException($"Index {index} is out of bounds for array of size {array.Length}.");
    }

    public override object? GetValue() => _array[Index];
    public override object? SetValue(object? value)
    {
        Logger.debug(() => $"_array={_array.GetType()}, Index={Index}, Value=({value?.GetType()}) {value}", "ElementAccessor.SetValue");

        var elType = _array.ElementType;
        if (elType is not null)
        {
            var intType = TypeDB.TryFind(elType);
            if (intType is not null)
                value = value switch
                {
                    IntConstExpr ice => ice.Materialize(intType),
                    _ => value
                };
        }

        value = value switch
        {
            IntConstExpr ice => ice.Materialize(),
            _ => value
        };

        _array[Index] = value;
        return GetValue();
    }

    public override string ToString() => $"ElementAccessor(Index={Index}, Array={_array})";
}

// index of element is unknown
class UnknownElementAccessor : ElementAccessorBase
{
    public UnknownElementAccessor(ArrayWrap array) : base(array)
    {
    }

    public override object? GetValue() => CreateUnknownElement(); // TODO: return all array values merged
    public override object? SetValue(object? value)
    {
        // index is not known, so spoil all elements of the array
        for (int i = 0; i < _array.Length; i++)
        {
            if (Equals(_array[i], value))
                continue;
            if (_array[i] is null || value is null)
                throw new NotSupportedException($"Cannot merge null with non-null value in array of type {_array.ElementType.Name} at index {i}.");

            // TODO: merging null with non-null
            var elValue = VarProcessor.MergeVar("_", _array[i]!, value);
            if (elValue is UnknownTypedValue utv && _array.ElementIntType is not null && utv.IntType != _array.ElementIntType)
                elValue = utv.Cast(_array.ElementIntType);
            _array[i] = elValue;
        }
        return GetValue();
    }
}
