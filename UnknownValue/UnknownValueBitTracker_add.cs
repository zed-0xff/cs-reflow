using BitType = int;

public partial class UnknownValueBitTracker
{
    UnknownValueBitTracker add(long l)
    {
        List<BitType> newBits = new(_bits);
        BitType carry = ZERO;
        for (int i = 0; i < type.nbits; i++, l >>>= 1)
        {
            bool add_bit = ((l & 1) != 0);

            switch (newBits[i], add_bit, carry)
            {
                case (_, false, ZERO):  // no change
                case (_, true, ONE): // move carry along, still no change
                    break;
                case (ZERO, true, ZERO): // safe set bit
                    newBits[i] = ONE;
                    break;
                case (ONE, true, ZERO): // clear bit, carry
                    newBits[i] = ZERO;
                    carry = ONE;
                    break;
                case (ZERO, false, _): // safe unload any carry, including ANY
                    newBits[i] = carry;
                    carry = ZERO;
                    break;
                case (ZERO, true, BitType c) when c != ZERO && c != ONE:
                    // 0 + 1 + 1 => b=!c, c=1
                    // 0 + 1 + 0 => b=!c, c=0
                    newBits[i] = c == ANY ? ANY : -c;
                    // carry is kept
                    break;
                case (ANY, _, _):      // either or both of adds are nonzero
                    carry = ANY;
                    break;
                case (BitType b1, true, ZERO) when b1.IsPrivateBit():
                case (BitType b2, false, ONE) when b2.IsPrivateBit():
                    carry = newBits[i];
                    newBits[i] = -newBits[i]; // invert the bit
                    break;
                case (BitType b, _, ANY):
                    newBits[i] = ANY;
                    carry = ANY;
                    break;
                case (BitType a, bool b, BitType c) when a.IsPrivateBit() && c.IsPrivateBit():
                    (newBits[i], carry) = a.AddRegular3(b ? ONE : ZERO, c);
                    break;
                default:
                    throw new NotImplementedException($"Cannot add: ({newBits[i]}, {add_bit}, {carry})");
            }
        }
        return new UnknownValueBitTracker(this, newBits);
    }

    UnknownValueBitTracker add(UnknownValueBitTracker other)
    {
        List<BitType> newBits = new(_bits);
        BitType carry = ZERO;
        for (int i = 0; i < type.nbits; i++)
        {
            BitType add_bit = other._bits[i];

            switch (newBits[i], add_bit, carry)
            {
                case (_, ZERO, ZERO):  // no change
                case (_, ONE, ONE): // move carry along, still no change
                    break;
                case (ZERO, ONE, ZERO): // safe set bit
                    newBits[i] = ONE;
                    break;
                case (ONE, ONE, ZERO): // clear bit, carry
                    newBits[i] = ZERO;
                    carry = ONE;
                    break;
                case (ZERO, ZERO, _): // safe unload any carry, including ANY
                    newBits[i] = carry;
                    carry = ZERO;
                    break;
                case (ZERO, ONE, BitType c) when c != ZERO && c != ONE:
                    // 0 + 1 + 1 => b=!c, c=1
                    // 0 + 1 + 0 => b=!c, c=0
                    newBits[i] = c == ANY ? ANY : -c;
                    // carry is kept
                    break;
                case (ANY, _, _):      // either or both of adds are nonzero
                    carry = ANY;
                    break;
                case (BitType b1, ONE, ZERO) when b1.IsPrivateBit():
                case (BitType b2, ZERO, ONE) when b2.IsPrivateBit():
                    carry = newBits[i];
                    newBits[i] = newBits[i].Invert();
                    break;
                case (BitType, _, ANY):
                case (BitType, ANY, _):
                    newBits[i] = ANY;
                    carry = ANY;
                    break;
                case (BitType a, BitType b, BitType c) when a != ANY && b != ANY && c != ANY:
                    (newBits[i], carry) = a.AddRegular3(b, c);
                    break;
                default:
                    throw new NotImplementedException($"Cannot add: ({newBits[i]}, {add_bit}, {carry})");
            }
        }
        return new UnknownValueBitTracker(this, newBits);
    }
}
