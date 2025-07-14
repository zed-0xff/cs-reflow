using System.Numerics;

// bitwise min/max
public readonly record struct BitSpan
{
    public readonly ulong Min; // bitwise min
    public readonly ulong Max; // bitwise max

    public BitSpan(ulong min, ulong max)
    {
        if (min > max)
            throw new ArgumentException($"Min({min}) cannot be greater than Max({max})");
        Min = min;
        Max = max;
    }

    public BitSpan(long min, long max) : this((ulong)min, (ulong)max) { }

    public bool Contains(ulong uvalue) => (uvalue & ~Max) == 0 && (uvalue & Min) == Min;
    public bool Contains(long value) => Contains((ulong)value);

    public CardInfo Cardinality() => CardInfo.FromBits(BitOperations.PopCount(Max ^ Min));

    public BitSpan Merge(BitSpan other) => new(Min & other.Min, Max | other.Max);
    public BitSpan Merge(ulong l) => new(Min & l, Max | l);
    public BitSpan Merge(long l) => Merge((ulong)l);

    public static BitSpan operator &(BitSpan a, BitSpan b) => new BitSpan(a.Min & b.Min, a.Max & b.Max);
    public static BitSpan operator &(BitSpan a, ulong u) => new BitSpan(a.Min & u, a.Max & u);
    public static BitSpan operator &(BitSpan a, long l) => a & (ulong)l;

    public static BitSpan operator |(BitSpan a, BitSpan b) => new BitSpan(a.Min | b.Min, a.Max | b.Max);
    public static BitSpan operator |(BitSpan a, ulong u) => new BitSpan(a.Min | u, a.Max | u);
    public static BitSpan operator |(BitSpan a, long l) => a | (ulong)l;

    public static BitSpan operator ~(BitSpan value) => new BitSpan(~value.Max, ~value.Min);

    // XOR with a constant does not change the uncertainty of a bit — it just flips known bits.
    public static BitSpan operator ^(BitSpan a, long b)
    {
        ulong ub = (ulong)b;
        // Bits known to be 0 or 1 get flipped if ub bit is 1
        ulong knownMask = ~(a.Min ^ a.Max); // bits that are known
        ulong newMin = (a.Min ^ ub) & knownMask;
        ulong newMax = newMin | ~knownMask; // unknown bits become (min=0, max=1)
        return new BitSpan(newMin, newMax);
    }

    public static BitSpan operator ^(BitSpan a, BitSpan b)
    {
        // Known bits are where min == max
        ulong a_known = ~(a.Min ^ a.Max);
        ulong b_known = ~(b.Min ^ b.Max);
        ulong known_mask = a_known & b_known;

        // XOR only the known bits
        ulong xor = (a.Min ^ b.Min) & known_mask;

        ulong min = xor;
        ulong max = xor | ~known_mask; // unknown bits get 1s in max

        return new BitSpan(min, max);
    }

    static ulong SignExtend(ulong value, int type_nbits)
    {
        ulong sign = value & (1UL << (type_nbits - 1));
        if (sign != 0)
            value = value | ~((1UL << type_nbits) - 1);
        return value;
    }

    public BitSpan SignedShiftRight(long shift, int type_nbits)
    {
        if (shift < 0)
            throw new ArgumentOutOfRangeException(nameof(shift), "Shift must be non-negative");

        long min = (long)SignExtend(Min, type_nbits);
        long max = (long)SignExtend(Max, type_nbits);
        long mask = (1L << type_nbits) - 1;
        return new BitSpan((min >> (int)shift) & mask, (max >> (int)shift) & mask);
    }

    public static BitSpan operator >>>(BitSpan value, long shift)
    {
        if (shift < 0)
            throw new ArgumentOutOfRangeException(nameof(shift), "Shift must be non-negative");

        if (shift >= 64)
            return new BitSpan(0, 0); // shifting out all bits

        int ishift = (int)shift;
        return new BitSpan(value.Min >>> ishift, value.Max >>> ishift);
    }

    public static BitSpan operator <<(BitSpan value, long shift)
    {
        if (shift < 0)
            throw new ArgumentOutOfRangeException(nameof(shift), "Shift must be non-negative");

        if (shift >= 64)
            return new BitSpan(0, 0); // shifting out all bits

        int ishift = (int)shift;
        return new BitSpan(value.Min << ishift, value.Max << ishift);
    }

    public IEnumerable<long> Values()
    {
        ulong fixedBits = Min;
        ulong floatingMask = Max & ~Min; // bits that can vary
        int floatingCount = BitOperations.PopCount(floatingMask);

        if (floatingCount > 20)
            throw new InvalidOperationException($"{this}.Values(): Too many values to enumerate.");

        // Get positions of floating bits
        int[] floatingBitPositions = new int[floatingCount];
        int index = 0;
        for (int i = 0; i < 64; i++)
        {
            if (((floatingMask >> i) & 1) != 0)
                floatingBitPositions[index++] = i;
        }

        int total = 1 << floatingCount;
        for (int i = 0; i < total; i++)
        {
            ulong value = fixedBits;
            for (int b = 0; b < floatingCount; b++)
            {
                if (((i >> b) & 1) != 0)
                    value |= 1UL << floatingBitPositions[b];
            }
            yield return (long)value;
        }
    }

    // transparently construct from (long min, long max) tuple
    public static implicit operator BitSpan((long min, long max) tuple) =>
        new BitSpan(tuple.min, tuple.max);

    public void Deconstruct(out long min, out long max)
    {
        min = (long)Min;
        max = (long)Max;
    }

    public override string ToString()
    {
        string max = $"{Max:X}";
        string min = $"{Min:X}".PadLeft(max.Length, '0');
        return $"[{min}..{max}]";
    }
}
