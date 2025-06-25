public class UnknownValueRange : UnknownValueRangeBase
{
    public readonly LongRange Range; // immutable!

    public UnknownValueRange(TypeDB.IntInfo type, LongRange? range = null) : base(type)
    {
        Range = range ?? this.type.Range;
    }

    public UnknownValueRange(TypeDB.IntInfo type, long min, long max) : base(type)
    {
        Range = new LongRange(min, max);
    }

    public override UnknownValueBase WithTag(object? tag) => Equals(_tag, tag) ? this : new(type, Range) { _tag = tag };
    public override UnknownValueBase WithVarID(int id) => Equals(_var_id, id) ? this : new(type, Range) { _var_id = id };

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
    public override bool IsFullRange() => Range.Equals(type.Range);
    public override BitSpan BitSpan() => Range.BitSpan();

    public override object Cast(TypeDB.IntInfo toType)
    {
        if (type == TypeDB.UInt && toType == TypeDB.Int) // uint -> int
        {
            if (Range.Min >= 0 && Range.Max <= int.MaxValue)
                return new UnknownValueRange(TypeDB.Int, Range);
            if (Range.Min > int.MaxValue)
                return new UnknownValueRange(TypeDB.Int, new LongRange((int)Range.Min, (int)Range.Max));
            return new UnknownValueRange(TypeDB.Int);
        }

        if (type == TypeDB.Int && toType == TypeDB.UInt) // int -> uint
        {
            if (Range.Min >= 0 && Range.Max >= 0)
                return new UnknownValueRange(TypeDB.UInt, Range);
            if (Range.Max < 0)
                return new UnknownValueRange(TypeDB.UInt, new LongRange((uint)Range.Min, (uint)Range.Max));
            return new UnknownValueRange(TypeDB.UInt);
        }

        return base.Cast(toType);
    }

    public override UnknownTypedValue TypedDiv(object right) =>
        TryConvertToLong(right, out long l)
        ? new UnknownValueRange(type, Range / l)
        : new UnknownValueRange(type);

    public override UnknownTypedValue TypedAdd(object right)
    {
        if (IsFullRange())
            return new UnknownValueRange(type);

        if (!TryConvertToLong(right, out long l))
            return new UnknownValueRange(type);

        var newMin = MaskWithSign(Range.Min + l);
        var newMax = MaskWithSign(Range.Max + l);

        if (newMin <= newMax)
        {
            return new UnknownValueRange(type, newMin, newMax);
        }
        else
        {
            if (type == TypeDB.ULong)
                throw new NotImplementedException();

            return new UnknownValueRanges(type,
                    new List<LongRange>(){
                        new LongRange(type.MinValue, newMax),
                        new LongRange(newMin, type.MaxSignedValue)
                    }
                );
        }
    }

    public override UnknownValueBase TypedSub(object right)
    {
        if (IsFullRange())
            return new UnknownValueRange(type);

        if (!TryConvertToLong(right, out long l))
            return new UnknownValueRange(type);

        var newMin = MaskWithSign(Range.Min - l);
        var newMax = MaskWithSign(Range.Max - l);

        if (newMin <= newMax)
        {
            return new UnknownValueRange(type, newMin, newMax);
        }
        else
        {
            if (type == TypeDB.ULong)
                throw new NotImplementedException();

            return new UnknownValueRanges(type,
                    new List<LongRange>(){
                        new LongRange(type.MinValue, newMax),
                        new LongRange(newMin, type.MaxSignedValue)
                    }
                );
        }

        return new UnknownValueRange(type, MaskWithSign(Range.Min - l), MaskWithSign(Range.Max - l));
    }

    public override UnknownTypedValue TypedShiftLeft(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return new UnknownValueRange(type);

        if (l >= type.nbits)
            throw new ArgumentOutOfRangeException($"Shift left {l} is out of range for {type}");

        ulong shiftedCardinality = 1UL << (type.nbits - (int)l);
        if (shiftedCardinality > MAX_DISCRETE_CARDINALITY || (_var_id != null && IsFullRange())) // TODO: not full range can also be converted to bits
            return ToBits().TypedShiftLeft(l);

        if (Cardinality() < MAX_DISCRETE_CARDINALITY)
        {
            int iShift = (int)l;
            return new UnknownValueSet(type, Values().Select(v => MaskWithSign(v << iShift)));
        }

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

        return new UnknownValueSet(type, values);
    }

    public override UnknownValueRange TypedSignedShiftRight(object right) // '>>'
    {
        if (!TryConvertToLong(right, out long l))
            return new(type);

        return new UnknownValueRange(type, Range >> (int)l);
    }

    public override UnknownValueRange TypedUnsignedShiftRight(object right) // '>>>'
    {
        if (!TryConvertToLong(right, out long l))
            return new(type);

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

    public override UnknownTypedValue TypedMod(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        // TODO: case when left is not full range
        if (l > 0)
            return new UnknownValueRange(type, new LongRange(0, l - 1));
        else
            return new UnknownValueRange(type, new LongRange(l + 1, 0));

    }

    public override UnknownValueBase TypedXor(object right)
    {
        if (!TryConvertToLong(right, out long l) || Cardinality() > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(type);

        return new UnknownValueSet(type, Values().Select(v => v ^ l));
    }

    public override UnknownValueBase Negate()
    {
        if (IsFullRange())
            return new UnknownValueRange(type);

        if (type.signed)
        {
            if (Contains(type.MinValue))
                return new UnknownValueRanges(type,
                        new List<LongRange>(){
                            new LongRange(type.MinValue, type.MinValue), // edge case: -128 for sbyte, -32768 for short, etc.
                            new LongRange(-Range.Max, Math.Min(-Range.Min, type.MaxSignedValue))
                        });
            return new UnknownValueRange(type, new LongRange(-Range.Max, -Range.Min));
        }
        else
        {
            if (type == TypeDB.ULong)
                throw new NotImplementedException("Negate() for ULong is not implemented.");

            var newMin = MaskNoSign(-Range.Max);
            var newMax = MaskNoSign(-Range.Min);
            if (newMin <= newMax)
                return new UnknownValueRange(type, newMin, newMax);
            else
                return new UnknownValueRanges(type, // [0..100] => [4294967196, 4294967295] U [0, 0]
                        new List<LongRange>(){
                            new LongRange(type.MinValue, newMax),
                            new LongRange(newMin, type.MaxSignedValue)
                        });
        }
    }

    public override IEnumerable<long> Values() => Range.Values();
    public override ulong Cardinality() => Range.Count;
    public override bool Contains(long value) => Range.Contains(value);
    public override int GetHashCode() => HashCode.Combine(type, Range);

    public override string ToString() => $"UnknownValue<{type}>" + (IsFullRange() ? "" : Range.ToString()) + TagStr() + VarIDStr();

    public override bool Typed_IntersectsWith(UnknownTypedValue right)
    {
        return right switch
        {
            UnknownValueBits b => b.IntersectsWith(this), // TODO: test
            UnknownValueSet l => l.Values().Any(v => Range.Contains(v)),
            UnknownValueRange r => Range.IntersectsWith(r.Range),
            UnknownValueRanges rr => rr.IntersectsWith(this),
            _ => throw new NotImplementedException($"{ToString()}.Typed_IntersectsWith({right}): not implemented.")
        };
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

