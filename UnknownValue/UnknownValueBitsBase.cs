using System.Numerics;

public abstract class UnknownValueBitsBase : UnknownTypedValue
{
    protected readonly BitSpan _bitspan;
    public override BitSpan BitSpan() => _bitspan;

    public UnknownValueBitsBase(TypeDB.IntType type, BitSpan bitspan) : base(type)
    {
        _bitspan = bitspan & type.Mask;
    }

    public abstract UnknownValueBitsBase Create(BitSpan bitspan);

    public bool IsOneBit(int idx) => (_bitspan.Min & (1UL << idx)) != 0;
    public bool IsZeroBit(int idx) => (_bitspan.Max & (1UL << idx)) == 0;
    public bool IsAnyBit(int idx) => (_bitspan.Min & (1UL << idx)) == 0 && (_bitspan.Max & (1UL << idx)) != 0;
    public override bool IsFullRange() => _bitspan.Equals(type.BitSpan);

    // keep only zeroes
    public virtual UnknownValueBase TypedBitwiseAnd(object right) => new UnknownValueBits(type, new BitSpan(0, _bitspan.Max));

    // keep only ones
    public virtual UnknownValueBase TypedBitwiseOr(object right) => new UnknownValueBits(type, new BitSpan(_bitspan.Min, type.BitSpan.Max));

    protected List<T> span2bits<T>(IEnumerable<T> bits, BitSpan bitspan, T zero, T one)
    {
        ulong v = 1;
        int i = 0;
        var newBits = new List<T>(bits);
        foreach (var bit in bits)
        {
            newBits[i] = (bitspan.Min & v, bitspan.Max & v) switch
            {
                (0, 0) => zero,    // both bits are 0
                (0, _) => bit,     // any, keep the existing value
                (_, 0) => throw new ArgumentException($"Invalid bitwise range: min={bitspan.Min & v}, max={bitspan.Max & v}"),
                (_, _) => one      // both bits are 1
            };
            v <<= 1;
            i++;
        }
        return newBits;
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
            return Create(new BitSpan(newMin, newMax));
        }

        return UnknownTypedValue.Create(type);
    }

    // retain top known zeroes
    public override UnknownTypedValue TypedDiv(object right)
    {
        int nz = CountHead(IsZeroBit);
        if (nz == type.nbits)
            return Zero(type);
        if (nz > type.nbits)
            throw new ArgumentException($"Invalid division by {right} for type {type}");

        return new UnknownValueBits(type, new BitSpan(0, (1UL << (type.nbits - nz) - 1))); // always returns UnknownValueBits bc no known bits are kept
    }

    // A / B:
    //  - all top zero bits of (either A or B) remain zero
    public override UnknownValueBase TypedMod(object right)
    {
        if (type.signed && IsAnyBit(type.nbits - 1))
        {
            if (TryConvertToLong(right, out long l0))
                return new UnknownValueRange(type, -l0 + 1, l0 - 1);
            return UnknownTypedValue.Create(type); // TODO: narrow
        }

        int nzL = CountHead(IsZeroBit);
        ulong maskL = (1UL << (type.nbits - nzL)) - 1;

        if (TryConvertToLong(right, out long l))
        {
            if (l < 0)
                l = -l; // in C# always x%y == x % (-y)

            if (IsPowerOfTwo(l))
                l--;

            int nzR = BitOperations.LeadingZeroCount((ulong)l);
            ulong maskR = (1UL << (64 - nzR)) - 1;
            return new UnknownValueBits(type, new BitSpan(0, maskL & maskR));
        }

        if (right is UnknownValueBitsBase otherBits)
        {
            // drop sign of otherBits if it is set
            if (otherBits.type.signed && !otherBits.IsZeroBit(otherBits.type.nbits - 1))
            {
                if (otherBits.IsOneBit(otherBits.type.nbits - 1))
                    return Mod(otherBits.Negate());
                else
                    return UnknownTypedValue.Create(type); // TODO: narrow, i.e. Mod(otherBits.Merge(otherBits.Negate()))
            }

            int nzR = otherBits.CountHead(otherBits.IsZeroBit);
            ulong maskR = (1UL << (type.nbits - nzR)) - 1;
            return new UnknownValueBits(type, new BitSpan(0, maskL & maskR));
        }

        if (right is UnknownTypedValue typedRight)
        {
            return UnknownTypedValue.Create(type);
        }

        throw new ArgumentException($"Invalid right operand for TypedMod: ({right?.GetType()}) {right}");
    }

    public override UnknownValueBase Merge(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            if (Contains(l))
                return this; // no change, already contains the value
            return Create(_bitspan.Merge(l));
        }

        return base.Merge(right);
    }
}
