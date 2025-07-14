using BitType = int;

public static class BitTypeExtensions
{
    const BitType ZERO = UnknownValueBitTracker.ZERO;
    const BitType ONE = UnknownValueBitTracker.ONE;
    const BitType ANY = UnknownValueBitTracker.ANY;

    public static BitType Invert(this BitType value) =>
        value switch
        {
            ZERO => ONE,
            ONE => ZERO,
            ANY => ANY,
            _ => -value // invert the bit id
        };

    public static bool IsOneOrZero(this BitType value) => value == ZERO || value == ONE;

    public static bool IsPrivateBit(this BitType value) =>
        value != ZERO && value != ONE && value != ANY;

    public static (BitType, BitType) AddRegular3(this BitType a, BitType b, BitType c)
    {
        if (a == c)
            return (b, c);
        if (a == c.Invert())
            return (b.Invert(), b);
        if (a == b)
            return (c, b);
        if (a == b.Invert())
            return (c.Invert(), c);

        return (ANY, ANY); // all three bits are different
    }
}

