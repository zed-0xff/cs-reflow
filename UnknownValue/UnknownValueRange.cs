public class UnknownValueRange : UnknownTypedValue
{
    public LongRange Range { get; }

    public UnknownValueRange(IntInfo type, LongRange? range = null) : base(type)
    {
        Range = range ?? this.type.Range;
    }

    public UnknownValueRange(string type, LongRange? range = null) : base(type)
    {
        Range = range ?? this.type.Range;
    }

    public override bool Equals(object obj)
    {
        if (obj is UnknownValueRange r)
        {
            return type == r.type && Range.Equals(r.Range);
        }
        return false;
    }

    public override long Min() => Range.Min;
    public override long Max() => Range.Max;

    public bool IsFullRange()
    {
        return Range.Equals(type.Range);
    }

    public override object Cast(string toType)
    {
        toType = ShortType(toType);

        if (type.Name == "uint" && toType == "int") // uint -> int
        {
            if (Range.Min >= 0 && Range.Max <= int.MaxValue)
                return new UnknownValueRange("int", Range);
            if (Range.Min > int.MaxValue)
                return new UnknownValueRange("int", new LongRange(unchecked((int)Range.Min), unchecked((int)Range.Max)));
            return new UnknownValueRange("int");
        }

        if (type.Name == "int" && toType == "uint") // int -> uint
        {
            if (Range.Min >= 0 && Range.Max >= 0)
                return new UnknownValueRange("uint", Range);
            if (Range.Max < 0)
                return new UnknownValueRange("uint", new LongRange(unchecked((uint)Range.Min), unchecked((uint)Range.Max)));
            return new UnknownValueRange("uint");
        }

        return base.Cast(toType);
    }

    public override UnknownValueRange Div(object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValueRange(type, Range / l)
            : new UnknownValueRange(type);
    }

    public override UnknownValueRange Add(object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValueRange(type, Range + l)
            : new UnknownValueRange(type);
    }

    public override UnknownValueRange Sub(object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValueRange(type, Range - l)
            : new UnknownValueRange(type);
    }

    public override UnknownValueBase ShiftLeft(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return new UnknownValueRange(type);

        if (l >= type.nbits)
            throw new ArgumentOutOfRangeException($"Shift left {l} is out of range for {type}");

        long shiftedCardinality = 1L << (type.nbits - (int)l);
        if (shiftedCardinality > MAX_DISCRETE_CARDINALITY)
            return new UnknownValueBits(type).ShiftLeft(l);

        List<long> values = new List<long>((int)shiftedCardinality);

        if (type.signed && type.nbits < 64)
        {
            long signMask = 1L << (type.nbits - 1);
            for (long i = 0; i < (long)shiftedCardinality; i++)
            {
                long value = i << (int)l;
                if ((value & signMask) != 0)
                    value = -((~value) & (signMask - 1)) - 1;
                values.Add(value);
            }
            values.Sort();
        }
        else
        {
            // TODO: check with signed/unsigned 64bit
            for (long i = 0; i < (long)shiftedCardinality; i++)
            {
                long value = i << (int)l;
                values.Add(value);
            }
        }

        return new UnknownValueList(type, values);
    }

    public override UnknownValueRange UnsignedShiftRight(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return new(type);

        return new UnknownValueRange(type, Range >>> (int)l);
    }

    public override UnknownValueRange Mod(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return new(type);

        if (l == 0)
            throw new DivideByZeroException();

        // TODO: case when left is not full range
        if (l > 0)
            return new UnknownValueRange(type, new LongRange(0, l - 1));
        else
            return new UnknownValueRange(type, new LongRange(l + 1, 0));

    }

    public override UnknownValueBase Xor(object right)
    {
        if (!TryConvertToLong(right, out long l) || Cardinality() > (long)MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(type);

        return new UnknownValueList(type, Values().Select(v => v ^ l).OrderBy(x => x).ToList());
    }

    public override IEnumerable<long> Values()
    {
        return Range.Values();
    }

    public override long Cardinality()
    {
        return Range.Max - Range.Min + 1; // because both are inclusive
    }

    public override string ToString()
    {
        if (IsFullRange())
            return $"UnknownValue<{type}>";
        else
            return $"UnknownValue<{type}>{Range}";
    }

    public override bool Contains(long value) => Range.Contains(value);

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public override bool IntersectsWith(UnknownTypedValue right)
    {
        return right switch
        {
            UnknownValueRange r => Range.IntersectsWith(r.Range),
            UnknownValueList l => l.Values().Any(v => Range.Contains(v)),
            _ => throw new NotImplementedException($"{ToString()}.IntersectsWith({right}): not implemented.")
        };
    }
}

