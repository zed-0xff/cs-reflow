public class UnknownValueRange : UnknownTypedValue
{
    public LongRange Range { get; }

    public UnknownValueRange(string type) : base(type)
    {
        Range = Type2Range(Type);
    }

    public UnknownValueRange(string type, LongRange range) : base(type)
    {
        Range = range;
    }

    public UnknownValueRange(string type, long min, long max) : base(type)
    {
        Range = new LongRange(min, max);
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

    public override object Cast(string toType)
    {
        toType = ShortType(toType);

        if (Type == "uint" && toType == "int") // uint -> int
        {
            if (Range.Min >= 0 && Range.Max <= int.MaxValue)
                return new UnknownValueRange("int", Range);
            if (Range.Min > int.MaxValue)
                return new UnknownValueRange("int", unchecked((int)Range.Min), unchecked((int)Range.Max));
            return new UnknownValueRange("int");
        }

        if (Type == "int" && toType == "uint") // int -> uint
        {
            if (Range.Min >= 0 && Range.Max >= 0)
                return new UnknownValueRange("uint", Range);
            if (Range.Max < 0)
                return new UnknownValueRange("uint", unchecked((uint)Range.Min), unchecked((uint)Range.Max));
            return new UnknownValueRange("uint");
        }

        return base.Cast(toType);
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

    public override UnknownValueBase ShiftLeft(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return new UnknownValueRange(Type);

        if (l >= nbits)
            throw new ArgumentOutOfRangeException($"Shift left {l} is out of range for {Type}");

        ulong shiftedCardinality = 1UL << (nbits - (int)l);
        if (shiftedCardinality > MAX_DISCRETE_CARDINALITY)
            return new UnknownValueRange(Type);

        List<long> values = new List<long>((int)shiftedCardinality);

        if (signed && nbits < 64)
        {
            long signMask = 1L << (nbits - 1);
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

        return new UnknownValueList(Type, values);
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
        if (!TryConvertToLong(right, out long l) || Cardinality() > MAX_DISCRETE_CARDINALITY)
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

