public class UnknownValueList : UnknownTypedValue
{
    public List<long> values = new();

    public UnknownValueList(string type, List<long> values = null) : base(type)
    {
        if (values != null)
            this.values = values;
    }

    public UnknownValueList(IntInfo type, List<long> values = null) : base(type)
    {
        if (values != null)
            this.values = values;
    }

    public override UnknownValueBase Add(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => Mask(v + l)).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(type);

    public override UnknownValueBase Sub(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => Mask(v - l)).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(type);

    public override UnknownValueBase Div(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => v / l).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(type);

    public override UnknownValueBase Mod(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => v % l).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(type);

    public override UnknownValueBase Xor(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => v ^ l).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(type);

    public override UnknownValueBase ShiftLeft(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => Mask(v << (int)l)).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(type);

    public override UnknownValueBase UnsignedShiftRight(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => Mask(v >>> (int)l)).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(type);

    public override UnknownValueBase Negate() => new UnknownValueList(type, values.Select(v => Mask(-v)).OrderBy(x => x).ToList());

    public override object Cast(string toType)
    {
        toType = ShortType(toType);
        if (type.Name == "uint" && toType == "int") // uint -> int
        {
            return new UnknownValueList("int", Values().Select(v => (long)unchecked((int)(uint)v)).ToList());
        }
        return base.Cast(toType);
    }

    public override string ToString()
    {
        return $"UnknownValue<{type}>[{values.Count}]";
    }

    public override long Cardinality() => values.Count;
    public override IEnumerable<long> Values() => values;
    public override bool Contains(long value) => values.Contains(value);
    public override long Min() => values.Min(); // TODO: assume values are sorted
    public override long Max() => values.Max(); // TODO: assume values are sorted

    public override bool IntersectsWith(UnknownTypedValue other)
    {
        return values.Any(v => other.Contains(v));
    }

    public override bool Equals(object obj) => obj is UnknownValueList other && type == other.type && values.SequenceEqual(other.values);

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
