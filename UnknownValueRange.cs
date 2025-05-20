public class UnknownValueRange : UnknownTypedValue
{
    public LongRange Range { get; private set; }

    public UnknownValueRange(string type) : base(type)
    {
        Range = Type2Range(Type);
    }

    public UnknownValueRange(string type, LongRange range) : base(type)
    {
        Range = range;
    }

    public static LongRange Type2Range(string type)
    {
        return type switch
        {
            "bool" => new LongRange(0, 1),
            "byte" => new LongRange(byte.MinValue, byte.MaxValue),
            "int" => new LongRange(int.MinValue, int.MaxValue),
            "sbyte" => new LongRange(sbyte.MinValue, sbyte.MaxValue),
            "uint" => new LongRange(uint.MinValue, uint.MaxValue),
            _ => throw new NotImplementedException($"UnknownValueRange: {type} not implemented."),
        };
    }

    public override bool Equals(object obj)
    {
        if (obj is UnknownValueRange r)
        {
            return Type == r.Type && Range.Equals(r.Range);
        }
        return false;
    }

    public override long Min() => Range.Min;
    public override long Max() => Range.Max;

    public bool IsFullRange()
    {
        return Range.Equals(Type2Range(Type));
    }

    public override UnknownValueRange Cast(string toType)
    {
        toType = ShortType(toType);
        if (Type == "uint" && toType == "int" && Range.Max <= int.MaxValue)
        {
            return new UnknownValueRange("int", new LongRange(0, Range.Max));
        }
        return new(toType);
    }

    public override UnknownValueRange Div(object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValueRange(Type, Range / l)
            : new UnknownValueRange(Type);
    }

    public override UnknownValueRange Add(object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValueRange(Type, Range + l)
            : new UnknownValueRange(Type);
    }

    public override UnknownValueRange Sub(object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValueRange(Type, Range - l)
            : new UnknownValueRange(Type);
    }

    public override UnknownValueBase Mul(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(Type);

        if (Cardinality() > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(Type);

        // TODO: apply mask
        return new UnknownValueList(Type, Values().Select(v => v * l).OrderBy(x => x).ToList());
    }

    public override UnknownValueRange Mod(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return new(Type);

        if (l == 0)
            throw new DivideByZeroException();

        // TODO: case when left is not full range
        if (l > 0)
            return new UnknownValueRange(Type, new LongRange(0, l - 1));
        else
            return new UnknownValueRange(Type, new LongRange(l + 1, 0));

    }

    public override UnknownValueBase Xor(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(Type);

        if (Cardinality() > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(Type);

        return new UnknownValueList(Type, Values().Select(v => v ^ l).OrderBy(x => x).ToList());
    }

    public override IEnumerable<long> Values()
    {
        return Range.Values();
    }

    public override ulong Cardinality()
    {
        return (ulong)(Range.Max - Range.Min + 1); // because both are inclusive
    }

    public override string ToString()
    {
        if (IsFullRange())
            return $"UnknownValue<{Type}>";
        else
            return $"UnknownValue<{Type}>{Range}";
    }

    public override bool Contains(long value) => Range.Contains(value);

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}

