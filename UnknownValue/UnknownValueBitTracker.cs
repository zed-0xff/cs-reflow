using System.Collections.ObjectModel;

using BitType = int;

public partial class UnknownValueBitTracker : UnknownValueBitsBase
{
    public const BitType ANY = -1;
    public const BitType ONE = 1;
    public const BitType ZERO = 0;

    // rough visual representation of the _bits
    // requirements:
    //  - at least 32 unique chars (better 64, but for now most ints are 32-bit)
    //  - each char should have upper/lower (on/off) visually distinct representation
    //  - ideally a prime number of chars for better distribution
    private static readonly string BITMAP = "abcdefghijklmnopqrstuvwxyz" + "абвгдеёжзийклмнопрстуфхцчшщъыьэюя"; // 59 chars

    // every bit is an int, with possible values:
    //   -1  any bit (unknown)
    //    0  zero bit
    //    1  one bit
    //    2+ unique bit id (2**30 values, so 16M vars if vars are 64-bit)
    //  <-2  inverted bit id
    readonly ReadOnlyCollection<BitType> _bits;

    public ReadOnlyCollection<BitType> Bits => _bits; // for tests only?

    public UnknownValueBitTracker(UnknownTypedValue parent, IEnumerable<BitType>? bits = null) : base(parent.type)
    {
        if (parent._var_id is null)
            throw new ArgumentException("Parent must have a var_id set.", nameof(parent));

        _var_id = parent._var_id;
        _bits = new(init(bits));
    }

    public UnknownValueBitTracker(TypeDB.IntInfo type, int var_id, IEnumerable<BitType>? bits = null) : base(type)
    {
        _var_id = var_id;
        _bits = new(init(bits));
    }

    public UnknownValueBitTracker(TypeDB.IntInfo type, int var_id, long val, long mask) : base(type) // mask represents the known _bits
    {
        _var_id = var_id;
        var bits = init(null);
        for (int i = 0; i < type.nbits; i++)
        {
            if ((mask & (1L << i)) != 0)
            {
                bits[i] = (sbyte)((val & (1L << i)) != 0 ? ONE : ZERO);
            }
        }
        _bits = new(bits);
    }

    public override UnknownValueBase WithTag(object? tag) => Equals(_tag, tag) ? this : new UnknownValueBitTracker(this, _bits) { _tag = tag };
    public override UnknownValueBase WithVarID(int id) => _var_id == id ? this : new UnknownValueBitTracker(type, id, _bits);

    public override bool IsFullRange() => _bits.All(b => !b.IsOneOrZero());

    // expects that _var_id is already set
    List<BitType> init(IEnumerable<BitType>? bits)
    {
        if (bits == null)
        {
            return Enumerable.Range(2 + _var_id.Value * type.nbits, type.nbits).Reverse().ToList();
        }

        if (bits.Count() != type.nbits)
            throw new ArgumentException($"Expected {type.nbits} bits, but got {bits.Count()} bits.");

        return new List<BitType>(bits);
    }

    // returns new UnknownValueBitTracker with the bit at idx set to value
    public UnknownValueBitTracker SetBit(int idx, BitType value)
    {
        if (idx < 0 || idx >= type.nbits)
            throw new ArgumentOutOfRangeException($"Index {idx} out of range for {type.nbits} _bits.");

        return new UnknownValueBitTracker(this, _bits.Select((b, i) => i == idx ? value : b));
    }

    public override (long, long) MaskVal(bool minimal = false)
    {
        long mask = 0;
        long val = 0;
        for (int i = 0; i < type.nbits; i++)
        {
            if (_bits[i] == ONE || _bits[i] == ZERO)
            {
                mask |= 1L << i;
                val |= (_bits[i] == ONE ? 1L : 0L) << i;
            }
            else if (minimal)
            {
                // all further _bits will be uncertain, so no need to decode them
                return (mask, val);
            }
        }
        return (mask, extend_sign(val));
    }

    public override object Cast(TypeDB.IntInfo toType)
    {
        if (toType.nbits == type.nbits)
            return new UnknownValueBitTracker(toType, _var_id.Value, _bits);

        return base.Cast(toType);
    }

    public override bool Contains(long value)
    {
        var (mask, val) = MaskVal();
        return ((value & mask) == val);
    }

    public override ulong Cardinality()
    {
        ulong cardinality = 1;
        for (int i = 0; i < type.nbits; i++)
            if (!_bits[i].IsOneOrZero())
                cardinality <<= 1;
        return cardinality;
    }

    public override IEnumerable<long> Values()
    {
        var (mask, val) = MaskVal();

        // Determine the bit positions that are "any" (i.e., mask bit = 1)
        List<int> floatingBits = new();
        for (int i = 0; i < _bits.Count; i++)
        {
            if (!_bits[i].IsOneOrZero())
                floatingBits.Add(i);
        }

        int floatingCount = floatingBits.Count;
        long combinations = 1L << floatingCount; // cardinality

        for (long i = 0; i < combinations; i++)
        {
            long dynamicPart = 0;
            for (int j = 0; j < floatingCount; j++)
            {
                if (((i >> j) & 1) != 0)
                    dynamicPart |= (1L << floatingBits[j]);
            }

            yield return val | dynamicPart;
        }
    }

    public override long Min()
    {
        var (mask, val) = MaskVal();
        if (type.signed && !_bits[type.nbits - 1].IsOneOrZero())
        {
            long sign_bit = (1L << (type.nbits - 1));
            return extend_sign(val | sign_bit);
        }
        else
        {
            return extend_sign(val);
        }
    }

    public override long Max()
    {
        var (mask, val) = MaskVal();
        if (type.signed && !_bits[type.nbits - 1].IsOneOrZero())
        {
            long sign_bit = (1L << (type.nbits - 1));
            long type_mask_no_sign = sign_bit - 1;
            return (~mask & type_mask_no_sign) | val;
        }
        else
        {
            return extend_sign((~mask & type.Mask) | val);
        }
    }

    public override bool Typed_IntersectsWith(UnknownTypedValue other)
    {
        if (other is UnknownValueBitTracker otherBT)
        {
            if (type.nbits != otherBT.type.nbits)
                return false;

            for (int i = 0; i < type.nbits; i++)
            {
                if (_bits[i] == otherBT._bits[i] || _bits[i] == ANY || otherBT._bits[i] == ANY)
                    continue;

                return false;
            }
            return true;
        }

        if (other is UnknownValueBits otherBits)
        {
            if (type.nbits != otherBits.type.nbits)
                return false;

            for (int i = 0; i < type.nbits; i++)
            {
                if (_bits[i] == otherBits.Bits[i] || _bits[i] == ANY || otherBits.Bits[i] == UnknownValueBits.ANY)
                    continue;

                return false;
            }
            return true;
        }

        throw new NotImplementedException($"Cannot check intersection with {other.GetType()}");
    }

    public override UnknownTypedValue TypedShiftLeft(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<BitType> newBits = new(_bits);
        for (int i = 0; i < l; i++)
        {
            newBits.RemoveAt(_bits.Count - 1);
            newBits.Insert(0, ZERO); // should be after remove!
        }
        return new UnknownValueBitTracker(this, newBits);
    }

    public override UnknownTypedValue TypedSignedShiftRight(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        BitType sign = type.signed ? _bits[type.nbits - 1] : ZERO;
        List<BitType> newBits = new(_bits);
        for (int i = 0; i < l; i++)
        {
            newBits.RemoveAt(0);
            newBits.Add(sign);
        }
        return new UnknownValueBitTracker(this, newBits);
    }

    public override UnknownTypedValue TypedUnsignedShiftRight(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<BitType> newBits = new(_bits);
        for (int i = 0; i < l; i++)
        {
            newBits.RemoveAt(0);
            newBits.Add(ZERO);
        }
        return new UnknownValueBitTracker(this, newBits);
    }

    public override UnknownValueBase TypedBitwiseAnd(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            // AND with a constant value
            List<BitType> newBits = new(_bits);
            long m = 1;
            for (int i = 0; i < type.nbits; i++, m <<= 1)
            {
                if ((l & m) == 0)
                    newBits[i] = ZERO;
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        if (right is UnknownValueBitTracker otherBT)
        {
            if (type != otherBT.type)
                throw new NotImplementedException($"Cannot AND {type} with {otherBT.type}");

            List<BitType> newBits = new(_bits);
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBT._bits[i]) switch
                {
                    (ZERO, _) => ZERO,
                    (_, ZERO) => ZERO,

                    (ANY, _) => ANY,
                    (_, ANY) => ANY,

                    (ONE, ONE) => ONE,

                    (ONE, BitType x) => x,
                    (BitType x, ONE) => x,

                    (BitType x, BitType y) =>
                        x == y ? x :
                        x == -y ? ZERO :
                        ANY,
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        if (right is UnknownValueBits otherBits)
        {
            if (type != otherBits.type)
                throw new NotImplementedException($"Cannot AND {type} with {otherBits.type}");

            List<BitType> newBits = new(_bits);
            var otherBitsList = otherBits.Bits;
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBitsList[i]) switch
                {
                    (ZERO, _) => ZERO,
                    (_, UnknownValueBits.ZERO) => ZERO,

                    (ANY, _) => ANY,
                    (_, UnknownValueBits.ANY) => ANY,

                    (_, UnknownValueBits.ONE) => newBits[i],

                    _ => throw new NotImplementedException($"Cannot AND {type} with {otherBits.type} at bit {i}")
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        return UnknownTypedValue.Create(type);
    }

    public override UnknownValueBase TypedBitwiseOr(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            // OR with a constant value
            List<BitType> newBits = new(_bits);
            long m = 1;
            for (int i = 0; i < type.nbits; i++, m <<= 1)
            {
                if ((l & m) != 0)
                    newBits[i] = ONE;
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        if (right is UnknownValueBitTracker otherBT)
        {
            if (type != otherBT.type)
                throw new NotImplementedException($"Cannot OR {type} with {otherBT.type}");

            List<BitType> newBits = new(_bits);
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBT._bits[i]) switch
                {
                    (ONE, _) => ONE,
                    (_, ONE) => ONE,

                    (ANY, _) => ANY,
                    (_, ANY) => ANY,

                    (ZERO, ZERO) => ZERO,

                    (ZERO, BitType x) => x,
                    (BitType x, ZERO) => x,

                    (BitType x, BitType y) =>
                        x == y ? x :
                        x == -y ? ONE :
                        ANY,
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        if (right is UnknownValueBits otherBits)
        {
            if (type != otherBits.type)
                throw new NotImplementedException($"Cannot OR {type} with {otherBits.type}");

            List<BitType> newBits = new(_bits);
            var otherBitsList = otherBits.Bits;
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBitsList[i]) switch
                {
                    (ONE, _) => ONE,
                    (_, UnknownValueBits.ONE) => ONE,

                    (ANY, _) => ANY,
                    (_, UnknownValueBits.ANY) => ANY,

                    (_, UnknownValueBits.ZERO) => newBits[i],

                    _ => throw new NotImplementedException($"Cannot AND {type} with {otherBits.type} at bit {i}")
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        return UnknownTypedValue.Create(type);
    }

    public override UnknownValueBitTracker BitwiseNot() =>
        new UnknownValueBitTracker(this, _bits.Select(b => b.Invert()));

    public override UnknownValueBase Negate() => BitwiseNot().Add(1);

    public override UnknownTypedValue TypedAdd(object right)
    {
        if (object.Equals(right, this)) // works because all bits have unique ids
            return TypedShiftLeft(1);

        if (TryConvertToLong(right, out long l))
            return add(l);

        if (right is UnknownValueBitTracker otherBT)
        {
            if (type != otherBT.type)
                throw new NotImplementedException($"Cannot add {type} with {otherBT.type}");

            return add(otherBT);
        }

        throw new NotImplementedException();
    }

    public override UnknownValueBase TypedXor(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            // XOR with a constant value
            List<BitType> newBits = new(_bits);
            long m = 1;
            for (int i = 0; i < type.nbits; i++, m <<= 1)
            {
                bool b = (l & m) != 0;
                newBits[i] = newBits[i] switch
                {
                    ANY => ANY,
                    ONE => b ? ZERO : ONE,
                    ZERO => b ? ONE : ZERO,
                    _ => b ? -newBits[i] : newBits[i]
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        if (right is UnknownTypedValue otherTyped)
        {
            if (otherTyped.CanConvertTo(this))
                otherTyped = otherTyped.ToBits();

            if (otherTyped is UnknownValueBitTracker otherBT)
                return xor(otherBT);

            if (otherTyped is UnknownValueBits otherBits)
                return xor(otherBits);
        }

        return UnknownTypedValue.Create(type);
    }

    public override UnknownValueBase TypedMod(object right)
    {
        throw new NotImplementedException();
    }

    public override UnknownValueBase TypedDiv(object right)
    {
        throw new NotImplementedException();
    }

    public override UnknownValueBase TypedSub(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            return add(-l);
        }

        throw new NotImplementedException();
    }

    public override bool Equals(object obj) => (obj is UnknownValueBitTracker other) && type.Equals(other.type) && _var_id == other._var_id && _bits.SequenceEqual(other._bits);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(type);
        foreach (var b in _bits)
            hash.Add(b);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        string result = $"UnknownValueBitTracker<{type}>[";
        int start = type.nbits - 1;
        while (start >= 0 && _bits[start] == ANY)
            start--;

        if (start != type.nbits - 1)
            result += "…";

        for (int i = start; i >= 0; i--)
        {
            BitType bit = _bits[i];
            result += bit switch
            {
                ANY => "_",
                ONE => "1",
                ZERO => "0",
                _ => bit > 0 ? BITMAP[(bit - 2) % BITMAP.Length] : BITMAP[(-bit - 2) % BITMAP.Length].ToString().ToUpperInvariant()
            };
        }

        result += "]";
        return result;
    }
}

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

