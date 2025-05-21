public class UnknownValueList : UnknownTypedValue
{
    public List<long> values = new();

    public UnknownValueList(string type) : base(type)
    {
    }

    public UnknownValueList(string type, List<long> values) : base(type)
    {
        this.values = values;
    }

    public override object Cast(string toType)
    {
        toType = ShortType(toType);
        if (Type == "uint" && toType == "int") // uint -> int
        {
            return new UnknownValueList("int", Values().Select(v => (long)unchecked((int)(uint)v)).ToList());
        }
        return base.Cast(toType);
    }

    public override string ToString()
    {
        return $"UnknownValue<{Type}>[{values.Count}]";
    }

    public override ulong Cardinality() => (ulong)values.Count;
    public override IEnumerable<long> Values() => values;
    public override bool Contains(long value) => values.Contains(value);

    public override UnknownValueBase Add(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => Mask(v + l)).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase Sub(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => Mask(v - l)).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase Div(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => v / l).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase Mod(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => v % l).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase Mul(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => Mask(v * l)).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase Xor(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => v ^ l).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase ShiftLeft(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => Mask(v << (int)l)).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(Type);

    public override bool IntersectsWith(UnknownTypedValue other)
    {
        return values.Any(v => other.Contains(v));
    }
}
