public class UnknownValueRange : UnknownValueRangeBase
{
    public readonly LongRange Range; // immutable!

    public UnknownValueRange(TypeDB.IntType type, LongRange? range = null) : base(type)
    {
        Range = range ?? this.type.Range;
    }

    public UnknownValueRange(TypeDB.IntType type, long min, long max) :
        this(type, new LongRange(min, max))
    {
        if (!type.CanFit(min) || !type.CanFit(max))
            throw new ArgumentOutOfRangeException($"Range [{min}, {max}] is out of bounds for type {type}");
    }

    public UnknownValueRange(UnknownTypedValue parent, TypeDB.IntType type, LongRange range) :
        this(type, range)
    {
        _var_id = parent._var_id;
    }

    public override UnknownValueRange WithTag(string key, object? value) => HasTag(key, value) ? this : new(type, Range) { _tags = add_tag(key, value) };
    public override UnknownValueRange WithVarID(int id) => Equals(_var_id, id) ? this : new(type, Range) { _var_id = id };

    // TODO: type conversion?
    public override UnknownTypedValue WithType(TypeDB.IntType type) => new UnknownValueRange(type, Range) { _var_id = _var_id };

    public override bool Equals(object? obj)
    {
        switch (obj)
        {
            case UnknownValueRange r:
                return type == r.type && Range.Equals(r.Range);
            case UnknownValueBits b:
                return CanConvertTo(b) && b.CanConvertTo(this) && b.BitSpan().Equals(BitSpan());
            default:
                return false;
        }
    }

    public override long Min() => Range.Min;
    public override long Max() => Range.Max;
    public override bool IsFullRange() => Range.Equals(type.Range);
    public override BitSpan BitSpan() => Range.BitSpan();

    public override bool CanConvertTo<T>()
    {
        if (typeof(UnknownValueBitsBase).IsAssignableFrom(typeof(T)))
        {
            BitSpan bs = BitSpan();
            if (IsFullRange()
                    || (bs.Min == (ulong)Min() && bs.Max == (ulong)Max())
                    || (Range.Min == 0 && (Range.Max & (Range.Max + 1)) == 0) // [0, ..fff]
                    || (type.signed && Range.Max == -(Range.Min + 1))
               )
            {
                if (typeof(T) == typeof(UnknownValueBitTracker))
                    return _var_id is not null; // UnknownValueBitTracker requires a variable ID
                return true; // UnknownValueBits can be created
            }
        }

        return base.CanConvertTo<T>();
    }

    public int RequiredSignedBits()
    {
        if (IsFullRange())
            return type.nbits;

        for (int bits = 1; bits <= 64; bits++)
        {
            long smin = -(1L << (bits - 1));
            long smax = (1L << (bits - 1)) - 1;

            if (Range.Min >= smin && Range.Max <= smax)
                return bits;
        }

        throw new ArgumentOutOfRangeException("Range too large");
    }

    public int RequiredUnsignedBits() => IsFullRange() ? type.nbits : Math.Max(1, (int)Math.Ceiling(Math.Log2(Range.Max)));
    public int RequiredBits() => (type.signed && IsNegative(Min())) ? RequiredSignedBits() : RequiredUnsignedBits();

    public override object TypedCast(TypeDB.IntType toType)
    {
        if (toType == TypeDB.ULong)
            throw new NotImplementedException("Cast to ULong is not implemented for UnknownValueRange.");

        if (type == TypeDB.UInt && toType == TypeDB.Int) // uint -> int
        {
            if (IsFullRange())
                return new UnknownValueRange(this, toType, toType.Range);
            if (Range.Min >= 0 && Range.Max <= int.MaxValue)
                return new UnknownValueRange(this, toType, Range);
            if (Range.Min > int.MaxValue)
                return new UnknownValueRange(this, toType, new LongRange((int)Range.Min, (int)Range.Max));
            return new UnknownValueRange(TypeDB.Int);
        }

        if (type == TypeDB.Int && toType == TypeDB.UInt) // int -> uint
        {
            if (IsFullRange())
                return new UnknownValueRange(this, toType, toType.Range);
            if (Range.Min >= 0 && Range.Max >= 0)
                return new UnknownValueRange(this, toType, Range);
            if (Range.Max < 0)
                return new UnknownValueRange(this, toType, new LongRange((uint)Range.Min, (uint)Range.Max));
            return new UnknownValueRange(TypeDB.UInt);
        }

        if (toType.ByteSize < type.ByteSize && Range.Min >= 0)
        {
            return new UnknownValueRange(this, toType, new LongRange(Range.Min & toType.Mask, Range.Max & toType.Mask));
        }

        return this;
    }

    public override UnknownTypedValue TypedDiv(object right) =>
        TryConvertToLong(right, out long l)
        ? new UnknownValueRange(type, Range / l)
        : new UnknownValueRange(type);

    public override UnknownTypedValue TypedAdd(object right)
    {
        if (IsFullRange())
        {
            if (CanConvertTo<UnknownValueBitTracker>())
                return (UnknownTypedValue)ConvertTo<UnknownValueBitTracker>().Add(right);

            return new UnknownValueRange(type);
        }

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
    }

    public override UnknownTypedValue TypedShiftLeft(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return new UnknownValueRange(type);

        if (l >= type.nbits)
            throw new ArgumentOutOfRangeException($"Shift left {l} is out of range for {type}");

        var shiftedCardinality = CardInfo.FromBits(type.nbits - (int)l);
        if (shiftedCardinality > MAX_DISCRETE_CARDINALITY || _var_id is not null || IsFullRange()) // TODO: compare cardinality with set
            return ToBits().TypedShiftLeft(l);

        if (Cardinality() < MAX_DISCRETE_CARDINALITY)
        {
            int iShift = (int)l;
            return new UnknownValueSet(type, Values().Select(v => MaskWithSign(v << iShift)));
        }

        int iCardinality = (int)shiftedCardinality.ulValue; // less than MAX_DISCRETE_CARDINALITY
        List<long> values = new List<long>(iCardinality);

        if (type.signed && type.nbits < 64)
        {
            long signMask = 1L << (type.nbits - 1);
            for (long i = 0; i < (long)iCardinality; i++)
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
            for (long i = 0; i < (long)iCardinality; i++)
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

    public override UnknownTypedValue TypedUnsignedShiftRight(object right) // '>>>'
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        int shift = (int)l;

        if (Range.Min >= 0) // handles all unsigned types + limited ranges of signed types
            return new UnknownValueRange(type, Range >> shift);

        if (Range.Max < 0)
            return new UnknownValueRange(type, new LongRange(Range.Min >>> shift, Range.Max >>> shift));

        // Range.Min < 0 && Range.Max >= 0
        return new UnknownValueRanges(type,
            new List<LongRange>(){
                new LongRange(0, Range.Max >>> shift),
                new LongRange((Range.Min & type.Mask) >>> shift, type.Mask >>> shift)
            });
    }

    public override UnknownTypedValue TypedMod(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        // TODO: case when left is not full range
        return new UnknownValueRange(type, (l > 0) ? new LongRange(0, l - 1) : new LongRange(l + 1, 0));
    }

    public override UnknownValueBase TypedXor(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            if (Cardinality() < MAX_DISCRETE_CARDINALITY)
                return new UnknownValueSet(type, Values().Select(v => v ^ l));

            return ToBits().Xor(l);
        }

        return UnknownValue.Create(type);
    }

    public override UnknownValueBase Negate()
    {
        if (IsFullRange())
        {
            if (CanConvertTo<UnknownValueBitTracker>())
                return ConvertTo<UnknownValueBitTracker>().Negate();
            return new UnknownValueRange(type);
        }

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
    public override CardInfo Cardinality() => Range.Cardinality();
    public override int GetHashCode() => HashCode.Combine(type, Range);
    public override bool TypedContains(long value) => Range.Contains(value);

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

