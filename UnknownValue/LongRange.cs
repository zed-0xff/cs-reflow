// both min and max are inclusive
public class LongRange
{
    public readonly long Min;
    public readonly long Max;

    public LongRange(long min, long max)
    {
        if (min > max)
            throw new ArgumentException($"Min({min}) cannot be greater than Max({max})");

        Min = min;
        Max = max;
    }

    public LongRange(int nbits, bool signed)
    {
        if (nbits < 1 || nbits > 64)
            throw new ArgumentException($"Invalid number of bits: {nbits}");

        if (signed)
        {
            if (nbits < 2)
                throw new ArgumentException($"Invalid number of bits: {nbits}");

            Min = -(1L << (nbits - 1));
            Max = (1L << (nbits - 1)) - 1;
        }
        else
        {
            Min = 0;
            Max = (1L << nbits) - 1;
        }
    }

    public BitSpan BitSpan()
    {
        long diff = Min ^ Max;

        // Create a mask of the shared high bits (i.e., where start == end)
        long mask = ~0L;
        while ((diff & mask) != 0)
            mask <<= 1;

        long min = Min & mask;
        long max = Max | ~mask;

        return (min, max);
    }

    public override string ToString()
    {
        return $"[{Min}..{Max}]";
    }

    // both Min and Max are inclusive
    public CardInfo Cardinality() => CardInfo.FromMinMaxInclusive(Min, Max);

    public IEnumerable<long> Values()
    {
        for (long i = Min; i <= Max; i++)
            yield return i;
    }

    public bool Contains(long value)
    {
        return value >= Min && value <= Max;
    }

    public override bool Equals(object? obj)
    {
        if (obj is LongRange other)
            return Min == other.Min && Max == other.Max;

        return false;
    }

    static public LongRange operator /(LongRange left, long right)
    {
        if (right == 0)
            throw new DivideByZeroException();

        return new LongRange(left.Min / right, left.Max / right);
    }

    static public LongRange operator *(LongRange left, long right)
    {
        return new LongRange(left.Min * right, left.Max * right);
    }

    static public LongRange operator +(LongRange left, long right)
    {
        return new LongRange(left.Min + right, left.Max + right);
    }

    static public LongRange operator -(LongRange left, long right)
    {
        return new LongRange(left.Min - right, left.Max - right);
    }

    static public LongRange operator >>(LongRange left, int right)
    {
        if (right < 0)
            throw new ArgumentException($"Shift count cannot be negative: {right}");

        return new LongRange(left.Min >> right, left.Max >> right);
    }

    // XXX >>> moves the sign bit
    //  -2147483648 >>> 1 = 9223372035781033984
    //    static public LongRange operator >>>(LongRange left, int right)
    //    {
    //        if (right < 0)
    //            throw new ArgumentException($"Shift count cannot be negative: {right}");
    //
    //        return new LongRange(left.Min >>> right, left.Max >>> right);
    //    }

    public override int GetHashCode() => HashCode.Combine(Min, Max);

    public bool IntersectsWith(LongRange other)
    {
        return Min <= other.Max && Max >= other.Min;
    }
}
