using System.Numerics;

public readonly record struct CardInfo
{
    public readonly ulong ulValue;
    public readonly double dValue;

    public static readonly CardInfo Zero = new(0, 0);

    public CardInfo(double d, ulong u)
    {
        dValue = d;
        if (d >= ulong.MaxValue)
            ulValue = ulong.MaxValue;
        else if (d > u)
            ulValue = Math.Ceiling(d) >= ulong.MaxValue ? ulong.MaxValue : (ulong)Math.Ceiling(d);
        else
            ulValue = u;
    }

    public CardInfo(int i) : this(i, (ulong)i) { }
    public CardInfo(ulong u) : this(u, u) { }

    public bool IsOne() => dValue == 1.0d;
    public bool IsZero() => dValue == 0.0d;

    public static CardInfo operator +(CardInfo a, CardInfo b) =>
        new CardInfo(a.dValue + b.dValue, a.ulValue + b.ulValue);

    public static CardInfo operator *(CardInfo a, CardInfo b) =>
        new CardInfo(a.dValue * b.dValue, a.ulValue * b.ulValue);

    public static bool operator <(CardInfo a, CardInfo b) => a.dValue < b.dValue;
    public static bool operator <=(CardInfo a, CardInfo b) => a.dValue <= b.dValue;
    public static bool operator >(CardInfo a, CardInfo b) => a.dValue > b.dValue;
    public static bool operator >=(CardInfo a, CardInfo b) => a.dValue >= b.dValue;

    public static bool operator <(CardInfo a, int i) => a.dValue < i;
    public static bool operator >(CardInfo a, int i) => a.dValue > i;

    public static CardInfo FromBits(int nbits) => new CardInfo(Math.Pow(2, nbits), (1UL << nbits));

    public static CardInfo FromMinMaxInclusive(long min, long max)
    {
        if (min > max)
            throw new ArgumentException($"Min({min}) cannot be greater than Max({max})");

        double dCount = 1.0 + max - min;
        ulong uCount = (ulong)(max - min + 1);
        return new CardInfo(dCount, uCount);
    }
}
