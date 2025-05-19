public class UnknownValueRange : UnknownTypedValue
{
    public LongRange Range { get; private set; }

    public UnknownValueRange(string type) : base(type)
    {
        init();
    }

    public UnknownValueRange(string type, LongRange range) : base(type)
    {
        Range = range;
    }

    void init()
    {
        switch (Type)
        {
            case "bool":
            case "System.Boolean":
                Range = new LongRange(0, 1);
                break;
            case "byte":
            case "System.Byte":
                Range = new LongRange(byte.MinValue, byte.MaxValue);
                break;
            case "int":
            case "System.Int32":
                Range = new LongRange(int.MinValue, int.MaxValue);
                break;
            case "sbyte":
            case "System.SByte":
                Range = new LongRange(sbyte.MinValue, sbyte.MaxValue);
                break;
            case "uint":
            case "System.UInt32":
                Range = new LongRange(uint.MinValue, uint.MaxValue);
                break;
            default:
                throw new NotImplementedException($"UnknownValueRange: {Type} not implemented.");
        }
    }

    public override UnknownValueRange Cast(string toType)
    {
        if (Type == "uint" && toType == "int" && Range.Max <= 0x7FFFFFFF)
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
        return $"UnknownValue<{Type}>{Range}";
    }
}

