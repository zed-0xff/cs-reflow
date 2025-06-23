using BitType = int;

public partial class UnknownValueBitTracker
{
    UnknownValueBitTracker xor(UnknownValueBitTracker other)
    {
        if (type != other.type)
            throw new NotImplementedException($"Cannot XOR {type} with {other.type}");

        List<BitType> newBits = new(_bits);
        for (int i = 0; i < type.nbits; i++)
        {
            newBits[i] = (newBits[i], other._bits[i]) switch
            {
                (ANY, _) => ANY,
                (_, ANY) => ANY,

                (ONE, ONE) => ZERO,
                (ZERO, ZERO) => ZERO,

                (ONE, ZERO) => ONE,
                (ZERO, ONE) => ONE,

                (ONE, BitType x) => -x,
                (BitType x, ONE) => -x,

                (ZERO, BitType x) => x,
                (BitType x, ZERO) => x,

                (BitType x, BitType y) =>
                    x == y ? ZERO :
                    x == -y ? ONE :
                    ANY,
            };
        }
        return new UnknownValueBitTracker(this, newBits);
    }

    UnknownValueBitTracker xor(UnknownValueBits other)
    {
        if (type != other.type)
            throw new NotImplementedException($"Cannot XOR {type} with {other.type}");

        List<BitType> newBits = new(_bits);
        var otherBits = other.Bits;
        for (int i = 0; i < type.nbits; i++)
        {
            if (_bits[i] == ANY || otherBits[i] == UnknownValueBits.ANY)
                newBits[i] = ANY;
            else
                newBits[i] = (newBits[i], otherBits[i]) switch
                {
                    (ONE, UnknownValueBits.ONE) => ZERO,
                    (ZERO, UnknownValueBits.ZERO) => ZERO,

                    (ONE, UnknownValueBits.ZERO) => ONE,
                    (ZERO, UnknownValueBits.ONE) => ONE,

                    (BitType x, UnknownValueBits.ONE) => -x,
                    (BitType x, UnknownValueBits.ZERO) => x,

                    _ => throw new NotImplementedException($"Cannot XOR {type} with {other.type} at bit {i}")
                };
        }
        return new UnknownValueBitTracker(this, newBits);
    }

}
