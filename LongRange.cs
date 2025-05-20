// both min and max are inclusive
public class LongRange
{
    public long Min { get; }
    public long Max { get; }

    public LongRange(long min, long max)
    {
        Min = min;
        Max = max;
    }

    public override string ToString()
    {
        return $"[{Min}..{Max}]";
    }

    public IEnumerable<long> Values()
    {
        for (long i = Min; i <= Max; i++)
            yield return i;
    }

    public bool Contains(long value)
    {
        return value >= Min && value <= Max;
    }

    public override bool Equals(object obj)
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

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
