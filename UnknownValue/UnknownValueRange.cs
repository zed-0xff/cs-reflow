public class UnknownValueRange : UnknownTypedValue
{
    public LongRange Range { get; }

    public UnknownValueRange(TypeDB.IntInfo type, LongRange? range = null) : base(type)
    {
        Range = range ?? this.type.Range;
    }

    public UnknownValueRange(TypeDB.IntInfo type, long min, long max) : base(type)
    {
        Range = new LongRange(min, max);
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

    public override object Cast(TypeDB.IntInfo toType)
    {
        if (type == TypeDB.UInt && toType == TypeDB.Int) // uint -> int
        {
            if (Range.Min >= 0 && Range.Max <= int.MaxValue)
                return new UnknownValueRange(TypeDB.Int, Range);
            if (Range.Min > int.MaxValue)
                return new UnknownValueRange(TypeDB.Int, new LongRange(unchecked((int)Range.Min), unchecked((int)Range.Max)));
            return new UnknownValueRange(TypeDB.Int);
        }

        if (type == TypeDB.Int && toType == TypeDB.UInt) // int -> uint
        {
            if (Range.Min >= 0 && Range.Max >= 0)
                return new UnknownValueRange(TypeDB.UInt, Range);
            if (Range.Max < 0)
                return new UnknownValueRange(TypeDB.UInt, new LongRange(unchecked((uint)Range.Min), unchecked((uint)Range.Max)));
            return new UnknownValueRange(TypeDB.UInt);
        }

        return base.Cast(toType);
    }

    public override UnknownValueRange Div(object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValueRange(type, Range / l)
            : new UnknownValueRange(type);
    }

    public override UnknownValueBase Add(object right)
    {
        if (right as UnknownValueRange == this)
            return ShiftLeft(1);

        return TryConvertToLong(right, out long l)
            ? new UnknownValueRange(type, Range + l)
            : new UnknownValueRange(type);
    }

    public override UnknownValueBase Sub(object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValueRange(type, Range - l)
            : base.Sub(right);
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

    public override UnknownValueRange SignedShiftRight(object right) // '>>'
    {
        if (!TryConvertToLong(right, out long l))
            return new(type);

        if (l == 0)
            return this;

        return new UnknownValueRange(type, Range >> (int)l);
    }

    public override UnknownValueRange UnsignedShiftRight(object right) // '>>>'
    {
        if (!TryConvertToLong(right, out long l))
            return new(type);

        if (l == 0)
            return this;

        if (l < 0)
            throw new ArgumentOutOfRangeException($"Shift right {l} is out of range for {type}");

        int shift = (int)l;

        if (Range.Min < 0 && Range.Max >= 0)
            return new UnknownValueRange(type, 0, Range.Max >>> shift);

        long min, max;
        (min, max) = type.Name switch
        {
            "int" => ((long)((int)Range.Min >>> shift), (long)((int)Range.Max >>> shift)),
            "nint" => ((long)((int)Range.Min >>> shift), (long)((int)Range.Max >>> shift)), // TODO: 32/64 bit cmdline switch
            "sbyte" => ((long)((sbyte)Range.Min >>> shift), (long)((sbyte)Range.Max >>> shift)),
            "short" => ((long)((short)Range.Min >>> shift), (long)((short)Range.Max >>> shift)),
            _ => (Range.Min >>> shift, Range.Max >>> shift)
        };

        if (min > max)
            (min, max) = (max, min);

        return new UnknownValueRange(type, min, max);
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

    public override UnknownValueBase Negate() => new UnknownValueRange(type, new LongRange(-Range.Max, -Range.Min));

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

    public override int GetHashCode() => HashCode.Combine(type, Range);

    public override bool IntersectsWith(UnknownTypedValue right)
    {
        return right switch
        {
            UnknownValueRange r => Range.IntersectsWith(r.Range),
            UnknownValueList l => l.Values().Any(v => Range.Contains(v)),
            _ => throw new NotImplementedException($"{ToString()}.IntersectsWith({right}): not implemented.")
        };
    }

    public override UnknownValueBase BitwiseAnd(object right)
    {
        if (right is UnknownValueBits b && IsFullRange())
        {
            return new UnknownValueBits(type, b.Bits);
        }
        return base.BitwiseAnd(right);
    }

    public override UnknownValueBase BitwiseNot()
    {
        long min = MaskWithSign(~Range.Min);
        long max = MaskWithSign(~Range.Max);
        if (max < min)
            (min, max) = (max, min);
        return new UnknownValueRange(type, new LongRange(min, max));
    }

    public override UnknownValueBase Merge(object other)
    {
        return other switch
        {
            UnknownValueRange r =>
                (r.Contains(Range.Min) && r.Contains(Range.Max)) ? r :
                (Contains(r.Range.Min) && Contains(r.Range.Max)) ? this :
                new UnknownValueRange(type),                                // TODO: handle merging ranges more intelligently

            byte b => Contains(b) ? this : new UnknownValueRange(type), // and for all of these
            sbyte sb => Contains(sb) ? this : new UnknownValueRange(type),
            short s => Contains(s) ? this : new UnknownValueRange(type),
            ushort us => Contains(us) ? this : new UnknownValueRange(type),
            int i => Contains(i) ? this : new UnknownValueRange(type),
            uint ui => Contains(ui) ? this : new UnknownValueRange(type),
            long l => Contains(l) ? this : new UnknownValueRange(type),
            ulong ul => (ul <= long.MaxValue && Contains((long)ul)) ? this : new UnknownValue(),

            _ => base.Merge(other)
        };
    }
}

