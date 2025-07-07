using System.Collections.ObjectModel;
using System.Text;

using BitType = System.SByte;

public class UnknownValueBits : UnknownValueBitsBase
{
    public const sbyte ANY = -1;
    public const sbyte ONE = 1;
    public const sbyte ZERO = 0;

    public UnknownValueBits(TypeDB.IntType type, BitSpan bitspan) : base(type, bitspan)
    {
    }

    public UnknownValueBits(TypeDB.IntType type, IEnumerable<BitType>? bits = null) :
        this(type, bits2span(type, bits))
    { }

    public override UnknownValueBits Create(BitSpan bitspan) => new(this.type, bitspan);

    protected static BitSpan bits2span(TypeDB.IntType type, IEnumerable<BitType>? bits)
    {
        if (bits is null)
            return type.BitSpan;

        ulong min = 0, max = 0;
        ulong v = 1;

        int i = 0;
        using var enumerator = bits.GetEnumerator();
        while (i < type.nbits)
        {
            if (!enumerator.MoveNext())
                throw new ArgumentException($"bits is shorter than expected: expected {type.nbits}, got {i}");

            var bit = enumerator.Current;
            switch (bit)
            {
                case ZERO:
                    break;
                case ONE:
                    min |= v;
                    max |= v;
                    break;
                case ANY:
                    max |= v;
                    break;
                default:
                    throw new ArgumentException($"Invalid BitType value at index {i}: {bit}");
            }

            v <<= 1;
            i++;
        }

        if (enumerator.MoveNext())
            throw new ArgumentException($"bits is longer than expected: expected {type.nbits}");

        return new BitSpan(min, max);
    }

    public override UnknownValueBase WithTag(string key, object? value) => HasTag(key, value) ? this : new UnknownValueBits(type, _bitspan) { _tags = add_tag(key, value) };
    public override UnknownValueBase WithVarID(int id) => _var_id == id ? this : new UnknownValueBits(type, _bitspan) { _var_id = id };
    public override UnknownTypedValue WithType(TypeDB.IntType type) => new UnknownValueBits(type, _bitspan); // TODO: check

    public static UnknownValueBits CreateFromAnd(TypeDB.IntType type, long mask)
    {
        return new UnknownValueBits(type, type.BitSpan & mask);
    }

    public static UnknownValueBits CreateFromOr(TypeDB.IntType type, long mask)
    {
        return new UnknownValueBits(type, type.BitSpan | mask);
    }

    List<BitType> init(IEnumerable<BitType>? bits)
    {
        if (bits is null)
            bits = Enumerable.Repeat<BitType>(ANY, type.nbits);

        if (bits.Count() != type.nbits)
            throw new ArgumentException($"Expected {type.nbits} bits, but got {bits.Count()} bits.");

        return new List<BitType>(bits);
    }

    public IEnumerable<BitType> GetBits()
    {
        ulong v = 1;
        for (int i = 0; i < type.nbits; i++, v <<= 1)
        {
            yield return (_bitspan & v) switch
            {
                (0, 0) => ZERO,
                (0, _) => ANY,
                (_, 0) => throw new ArgumentException($"Invalid bitwise range"),
                (_, _) => ONE
            };
        }
    }

    IEnumerable<BitType> GetReverseBits()
    {
        ulong v = 1UL << (type.nbits - 1);
        for (int i = 0; i < type.nbits; i++, v >>= 1)
        {
            yield return (_bitspan & v) switch
            {
                (0, 0) => ZERO,
                (0, _) => ANY,
                (_, 0) => throw new ArgumentException($"Invalid bitwise range"),
                (_, _) => ONE
            };
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"UnknownValueBits<{type}>[");

        bool seenNonAny = false;
        bool ellipsisAppeared = false;

        foreach (var bit in GetReverseBits())
        {
            if (bit == ANY)
            {
                if (seenNonAny)
                    sb.Append('_');
                else if (!ellipsisAppeared)
                {
                    sb.Append('…');
                    ellipsisAppeared = true; // only append '?' once
                }
            }
            else
            {
                seenNonAny = true;
                sb.Append(bit.ToString());
            }
        }

        sb.Append(']');
        return sb.ToString();
    }

    // returns new UnknownValueBits with the bit at idx set to value
    public UnknownValueBits SetBit(int idx, BitType value)
    {
        if (idx < 0 || idx >= type.nbits)
            throw new ArgumentOutOfRangeException($"Index {idx} out of range for {type.nbits} bits.");

        ulong v = 1UL << idx;
        return value switch
        {
            ZERO => new UnknownValueBits(type, _bitspan & ~v),
            ONE => new UnknownValueBits(type, _bitspan | v),
            ANY => new UnknownValueBits(type, new BitSpan(_bitspan.Min & ~v, _bitspan.Max | v)),
            _ => throw new ArgumentException($"Invalid value {value} for bit at index {idx}.")
        };
    }

    public override object Cast(TypeDB.IntType toType) => (toType.nbits == type.nbits) ? new UnknownValueBits(toType, _bitspan) : base.Cast(toType);

    public override bool Typed_IntersectsWith(UnknownTypedValue other)
    {
        if (other is UnknownValueBitTracker otherTracker)
            return otherTracker.Typed_IntersectsWith(this);

        if (other is UnknownValueBits otherBits)
        {
            BitSpan a = this._bitspan;
            BitSpan b = otherBits._bitspan;

            // Intersect if there's at least one value that satisfies both BitSpans.
            // That happens when: (a.Min & b.Max) <= (a.Max & b.Min)
            // But we can check a more intuitive condition:
            ulong mask = ~(a.Min ^ a.Max | b.Min ^ b.Max); // bits that are the same in both
            return (a.Min & mask) == (b.Min & mask);
        }

        if (other is UnknownValueRange otherRange)
        {
            // UnknownValueRange is easier to convert to UnknownValueBits than vice versa?
            // if (otherRange.ToMinBitSpan().IntersectsWith(this)) // TODO
            //     return true;
        }

        if (other.Cardinality() > 10_000 && this.Cardinality() > 10_000)
            Logger.warn($"Large intersection check: {this} with {other}.");

        if (other.Cardinality() < this.Cardinality())
            return other.Values().Any(v => Contains(v));
        else
            return Values().Any(v => other.Contains(v));
    }

    public override UnknownTypedValue TypedShiftLeft(object right)
    {
        if (TryConvertToLong(right, out long l))
            return new UnknownValueBits(type, _bitspan << l);

        return UnknownTypedValue.Create(type);
    }

    public BitType GetSign()
    {
        if (!type.signed)
            return ZERO;

        return (_bitspan & type.SignMask) switch
        {
            (0, 0) => ZERO,
            (0, _) => ANY,
            (_, 0) => throw new ArgumentException("Invalid BitSpan"),
            (_, _) => ONE
        };
    }

    public override UnknownTypedValue TypedSignedShiftRight(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        if (!type.signed)
            return TypedUnsignedShiftRight(l);

        return new UnknownValueBits(type, _bitspan.SignedShiftRight(l, type.nbits));
    }

    public override UnknownTypedValue TypedUnsignedShiftRight(object right)
    {
        if (TryConvertToLong(right, out long l))
            return new UnknownValueBits(type, _bitspan >>> l);

        return UnknownTypedValue.Create(type);
    }

    private UnknownValueBase bitwise_op(object right, Func<BitSpan, BitSpan, BitSpan> op, Func<BitSpan, long, BitSpan> opWithLong)
    {
        if (right is UnknownValueBits otherBits && otherBits.type.nbits == type.nbits)
            return new UnknownValueBits(type, op(_bitspan, otherBits._bitspan));

        if (TryConvertToLong(right, out long l))
            return new UnknownValueBits(type, opWithLong(_bitspan, l));

        return UnknownTypedValue.Create(type);
    }

    public override UnknownValueBase TypedBitwiseAnd(object right) => bitwise_op(right, (a, b) => a & b, (a, b) => a & b);
    public override UnknownValueBase TypedBitwiseOr(object right) => bitwise_op(right, (a, b) => a | b, (a, b) => a | b);
    public override UnknownValueBase TypedXor(object right) => bitwise_op(right, (a, b) => a ^ b, (a, b) => a ^ b);

    public override UnknownValueBits BitwiseNot() => new(type, ~_bitspan);
    public override UnknownValueBase Negate() => BitwiseNot().Add(1);

    UnknownTypedValue add(long l)
    {
        List<BitType> newBits = GetBits().ToList();
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
                case (ZERO, true, ANY):
                    newBits[i] = ANY;
                    // carry is kept
                    break;
                case (ONE, true, ANY):
                    newBits[i] = ANY;
                    carry = ONE;
                    break;
                case (ANY, _, _):      // either or both of adds are nonzero
                    carry = ANY;
                    break;
                case (ONE, false, ONE):
                    newBits[i] = ZERO; // clear bit, keep carry
                    break;
                case (ONE, false, ANY):
                    newBits[i] = ANY; // carry is kept, but bit is set
                    break;
                default:
                    throw new NotImplementedException($"Cannot add: ({newBits[i]}, {add_bit}, {carry})");
            }
        }
        return new UnknownValueBits(type, newBits);
    }

    UnknownTypedValue add(UnknownValueBits other)
    {
        List<BitType> newBits = GetBits().ToList();
        List<BitType> otherBits = other.GetBits().ToList();
        BitType carry = ZERO;
        for (int i = 0; i < type.nbits; i++)
        {
            BitType add_bit = otherBits[i];

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
                case (ZERO, ANY, ZERO):
                case (ZERO, ONE, ANY):
                case (ZERO, ANY, ANY):
                    newBits[i] = ANY;
                    // carry is kept, if any
                    break;
                case (ANY, _, _):      // either or both of adds are nonzero
                    carry = ANY;
                    break;
                default:
                    throw new NotImplementedException($"Cannot add: ({newBits[i]}, {add_bit}, {carry})");
            }
        }
        return new UnknownValueBits(type, newBits);
    }

    public override UnknownTypedValue TypedAdd(object right)
    {
        if (TryConvertToLong(right, out long l))
            return add(l);

        if (right is UnknownValueBits otherBits)
            return add(otherBits);

        if (right is UnknownValueBitTracker otherTracker)
            return otherTracker.TypedAdd(this);

        return UnknownTypedValue.Create(type);
    }

    public override UnknownValueBase TypedSub(object right)
    {
        if (TryConvertToLong(right, out long l))
            return Add(-l); // XXX what if 'this' is unsigned?

        if (right is UnknownTypedValue otherTyped)
            return Add(otherTyped.Negate());

        return UnknownTypedValue.Create(type);
    }

    public override bool Equals(object? obj) => (obj is UnknownValueBits other) && type.Equals(other.type) && _bitspan.Equals(other._bitspan);

    public override UnknownValueBase Merge(object other)
    {
        return other switch
        {
            // TODO: narrower range with new class, that contains a list of UnknownValueBase
            UnknownValueBits otherBits => new UnknownValueBits(type, _bitspan.Merge(otherBits._bitspan)),
            _ => base.Merge(other)
        };
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
