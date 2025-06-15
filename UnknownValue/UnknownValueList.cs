public class UnknownValueList : UnknownTypedValue
{
    public HashSet<long> values = new();

    public UnknownValueList(TypeDB.IntInfo type, List<long> values = null) : base(type)
    {
        if (values != null)
            this.values = new(values);
    }

    public UnknownValueList(TypeDB.IntInfo type, IEnumerable<long> values) : base(type)
    {
        this.values = new(values);
    }

    public override UnknownValueBase Add(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => MaskWithSign(v + l)))
            : new UnknownValueList(type);

    public override UnknownValueBase Sub(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => MaskWithSign(v - l)))
            : base.Sub(right);

    public override UnknownValueBase Div(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => v / l))
            : new UnknownValueList(type);

    public override UnknownValueBase Mod(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => v % l))
            : new UnknownValueList(type);

    public override UnknownValueBase Xor(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => v ^ l))
            : new UnknownValueList(type);

    public override UnknownValueBase ShiftLeft(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => MaskWithSign(v << (int)l)))
            : new UnknownValueList(type);

    public override UnknownValueBase SignedShiftRight(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => MaskWithSign(v >> (int)l)))
            : new UnknownValueList(type);

    public override UnknownValueBase UnsignedShiftRight(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(type, values.Select(v => MaskWithSign(v >>> (int)l)))
            : new UnknownValueList(type);

    public override UnknownValueBase Negate() => new UnknownValueList(type, values.Select(v => MaskWithSign(-v)));
    public override UnknownValueBase BitwiseNot() => new UnknownValueList(type, values.Select(v => MaskWithSign(~v)));

    public override object Cast(TypeDB.IntInfo toType)
    {
        if (type == TypeDB.UInt && toType == TypeDB.Int) // uint -> int
        {
            return new UnknownValueList(TypeDB.Int, Values().Select(v => (long)unchecked((int)(uint)v)));
        }
        else if (type == TypeDB.Int && toType == TypeDB.UInt) // int -> uint
        {
            return new UnknownValueList(TypeDB.UInt, Values().Select(v => (long)unchecked((uint)v)));
        }
        return base.Cast(toType);
    }

    public override string ToString()
    {
        if (values.Count <= 5)
            return $"UnknownValueList<{type}>{{{string.Join(", ", values)}}}";
        else
            return $"UnknownValueList<{type}>[{values.Count}]";
    }

    public override long Cardinality() => values.Count;
    public override IEnumerable<long> Values() => values;
    public override bool Contains(long value) => values.Contains(value);
    public override long Min() => values.Min();
    public override long Max() => values.Max();

    public override bool IntersectsWith(UnknownTypedValue other)
    {
        return values.Any(v => other.Contains(v));
    }

    public override bool Equals(object obj) => obj is UnknownValueList other && type == other.type && values.SequenceEqual(other.values);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(type);
        foreach (var v in values)
            hash.Add(v);
        return hash.ToHashCode();
    }

    public override UnknownValueBase Merge(object other)
    {
        return other switch
        {
            UnknownValueList l => new UnknownValueList(type, values.Union(l.values)),
            byte b => Contains(b) ? this : new UnknownValueList(type, values.Concat(new[] { (long)b })),
            sbyte sb => Contains(sb) ? this : new UnknownValueList(type, values.Concat(new[] { (long)sb })),
            short s => Contains(s) ? this : new UnknownValueList(type, values.Concat(new[] { (long)s })),
            ushort us => Contains(us) ? this : new UnknownValueList(type, values.Concat(new[] { (long)us })),
            int i => Contains(i) ? this : new UnknownValueList(type, values.Concat(new[] { (long)i })),
            uint ui => Contains(ui) ? this : new UnknownValueList(type, values.Concat(new[] { (long)ui })),
            long l => Contains(l) ? this : new UnknownValueList(type, values.Concat(new[] { (long)l })),
            // TODO: ulong
            _ => base.Merge(other)
        };
    }
}
