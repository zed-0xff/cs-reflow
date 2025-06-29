public abstract class UnknownValueBitsBase : UnknownTypedValue
{
    protected readonly BitSpan _bitspan;
    public override BitSpan BitSpan() => _bitspan;

    public UnknownValueBitsBase(TypeDB.IntType type, BitSpan bitspan) : base(type)
    {
        _bitspan = bitspan & type.Mask;
    }

    public override ulong Cardinality() => _bitspan.Cardinality();
    public override bool Contains(long value) => _bitspan.Contains(value);
    public override IEnumerable<long> Values() => type.signed ? _bitspan.Values().Select(SignExtend) : _bitspan.Values();

    public bool IsOneBit(int idx) => (_bitspan.Min & (1UL << idx)) != 0;
    public bool IsZeroBit(int idx) => (_bitspan.Max & (1UL << idx)) == 0;
    public bool IsAnyBit(int idx) => (_bitspan.Min & (1UL << idx)) == 0 && (_bitspan.Max & (1UL << idx)) != 0;
    public override bool IsFullRange() => _bitspan.Equals(type.BitSpan);

    public abstract UnknownValueBase TypedBitwiseAnd(object right);
    public abstract UnknownValueBase TypedBitwiseOr(object right);

    protected List<T> span2bits<T>(List<T> bits, BitSpan bitspan, T zero, T one)
    {
        ulong v = 1;
        for (int i = 0; i < type.nbits; i++, v <<= 1)
        {
            bits[i] = (bitspan.Min & v, bitspan.Max & v) switch
            {
                (0, 0) => zero,    // both bits are 0
                (0, _) => bits[i], // any, keep the existing value
                (_, 0) => throw new ArgumentException($"Invalid bitwise range: min={bitspan.Min & v}, max={bitspan.Max & v}"),
                (_, _) => one      // both bits are 1
            };
        }
        return bits;
    }

    public override long Min()
    {
        (long min, long max) = _bitspan;
        if (IsNegative(max))
            min |= ~(type.Mask >> 1); // always set the sign bit, either it was set or it is unknown
        return min;
    }

    public override long Max()
    {
        (long min, long max) = _bitspan;
        if (IsPositive(min) && IsNegative(max))
            max &= ~type.SignMask; // reset the sign bit if it was set, and could be zero
        else
            max = SignExtend(max); // extend the sign bit if it was set, and cannot be zero
        return max;
    }

    public int CountHead(Func<int, bool> isBit)
    {
        for (int i = type.nbits - 1; i >= 0; i--)
        {
            if (!isBit(i))
                return type.nbits - 1 - i;
        }
        return type.nbits; // isBit() is true for all bits
    }

    public int CountTail(Func<int, bool> isBit)
    {
        for (int i = 0; i < type.nbits; i++)
        {
            if (!isBit(i))
                return i;
        }
        return type.nbits; // isBit() is true for all bits
    }

    public override bool CanConvertTo<T>()
    {
        if (typeof(T) == typeof(UnknownValueRange))
        {
            int tailAny = CountTail(IsAnyBit);
            int headFixed = CountHead(i => !IsAnyBit(i));
            return headFixed + tailAny == type.nbits;
        }

        return base.CanConvertTo<T>();
    }

    public UnknownTypedValue TypedMul(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            int i = 0;
            while ((l & 1) == 0 && i < type.nbits)
            {
                l >>>= 1; i++;
            }
            var result = TypedShiftLeft(i);
            l >>>= 1; i++;
            for (; l != 0; i++, l >>>= 1)
            {
                if ((l & 1) != 0)
                {
                    result = result.TypedAdd(TypedShiftLeft(i));
                }
            }
            return result;
        }

        if (right is UnknownValueBitsBase otherBits)
        {
            var spanA = this.BitSpan();
            var spanB = otherBits.BitSpan();
            long newMin = 0;
            long newMax = type.Mask;
            int tailZeroesA = CountTail(IsZeroBit);
            int tailZeroesB = otherBits.CountTail(IsZeroBit);
            long v = 1;
            for (int i = 0; i < type.nbits && i < tailZeroesA + tailZeroesB; i++, v <<= 1)
            {
                newMax &= ~v; // set bit to zero
            }
            if (this.IsOneBit(tailZeroesA) && otherBits.IsOneBit(tailZeroesB))
            {
                newMin |= v; // set bit to one
            }
            return new UnknownValueBits(type, new BitSpan(newMin, newMax));
        }

        return UnknownTypedValue.Create(type);
    }

    // retain top known zeroes
    public override UnknownValueBitsBase TypedDiv(object right)
    {
        ulong v = 1UL << (type.nbits - 1);
        ulong max = type.BitSpan.Max;
        while ((_bitspan.Min & v) == 0 && (_bitspan.Max & v) == 0)
        {
            v >>= 1;
            max >>= 1;
        }
        return new UnknownValueBits(type, new BitSpan(0, max));
    }
}
