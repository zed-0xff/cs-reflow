using System.Collections.Immutable;

public class UnknownValueSet : UnknownTypedValue
{
    readonly ImmutableHashSet<long> _values = ImmutableHashSet<long>.Empty;

    public UnknownValueSet(TypeDB.IntInfo type, List<long> values = null) : base(type)
    {
        if (values != null)
            _values = ImmutableHashSet.CreateRange(values);
    }

    public UnknownValueSet(TypeDB.IntInfo type, IEnumerable<long> values) : base(type)
    {
        if (values != null)
            _values = ImmutableHashSet.CreateRange(values);
    }

    public override UnknownValueBase WithTag(object? tag) => Equals(_tag, tag) ? this : new(type, _values) { _tag = tag };

    public override UnknownValueBase Add(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueSet(type, _values.Select(v => MaskWithSign(v + l)))
            : new UnknownValueSet(type);

    public override UnknownValueBase Sub(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueSet(type, _values.Select(v => MaskWithSign(v - l)))
            : base.Sub(right);

    public override UnknownValueBase Div(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueSet(type, _values.Select(v => v / l))
            : new UnknownValueSet(type);

    public override UnknownValueBase Mod(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueSet(type, _values.Select(v => v % l))
            : new UnknownValueSet(type);

    public override UnknownValueBase Xor(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueSet(type, _values.Select(v => v ^ l))
            : new UnknownValueSet(type);

    public override UnknownValueBase ShiftLeft(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueSet(type, _values.Select(v => MaskWithSign(v << (int)l)))
            : new UnknownValueSet(type);

    public override UnknownValueBase SignedShiftRight(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueSet(type, _values.Select(v => MaskWithSign(v >> (int)l)))
            : new UnknownValueSet(type);

    public override UnknownValueBase UnsignedShiftRight(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueSet(type, _values.Select(v => MaskWithSign(v >>> (int)l)))
            : new UnknownValueSet(type);

    public override UnknownValueBase Negate() => new UnknownValueSet(type, _values.Select(v => MaskWithSign(-v)));
    public override UnknownValueBase BitwiseNot() => new UnknownValueSet(type, _values.Select(v => MaskWithSign(~v)));

    public override object Cast(TypeDB.IntInfo toType)
    {
        if (type == TypeDB.UInt && toType == TypeDB.Int) // uint -> int
        {
            return new UnknownValueSet(TypeDB.Int, Values().Select(v => (long)unchecked((int)(uint)v)));
        }
        else if (type == TypeDB.Int && toType == TypeDB.UInt) // int -> uint
        {
            return new UnknownValueSet(TypeDB.UInt, Values().Select(v => (long)unchecked((uint)v)));
        }
        return base.Cast(toType);
    }

    public override string ToString()
    {
        if (_values.Count <= 5)
            return $"UnknownValueSet<{type}>{{{string.Join(", ", _values)}}}";
        else
            return $"UnknownValueSet<{type}>[{_values.Count}]";
    }

    public override long Cardinality() => _values.Count;
    public override IEnumerable<long> Values() => _values;
    public override bool Contains(long value) => _values.Contains(value);
    public override long Min() => _values.Min();
    public override long Max() => _values.Max();

    public override bool IntersectsWith(UnknownTypedValue other)
    {
        return _values.Any(v => other.Contains(v));
    }

    public override bool Equals(object obj) => obj is UnknownValueSet other && type == other.type && _values.SequenceEqual(other._values);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(type);
        foreach (var v in _values)
            hash.Add(v);
        return hash.ToHashCode();
    }

    public override UnknownValueBase Merge(object other)
    {
        return other switch
        {
            UnknownValueSet l => new UnknownValueSet(type, _values.Union(l._values)),
            byte b => Contains(b) ? this : new UnknownValueSet(type, _values.Concat(new[] { (long)b })),
            sbyte sb => Contains(sb) ? this : new UnknownValueSet(type, _values.Concat(new[] { (long)sb })),
            short s => Contains(s) ? this : new UnknownValueSet(type, _values.Concat(new[] { (long)s })),
            ushort us => Contains(us) ? this : new UnknownValueSet(type, _values.Concat(new[] { (long)us })),
            int i => Contains(i) ? this : new UnknownValueSet(type, _values.Concat(new[] { (long)i })),
            uint ui => Contains(ui) ? this : new UnknownValueSet(type, _values.Concat(new[] { (long)ui })),
            long l => Contains(l) ? this : new UnknownValueSet(type, _values.Concat(new[] { (long)l })),
            // TODO: ulong
            _ => base.Merge(other)
        };
    }
}
