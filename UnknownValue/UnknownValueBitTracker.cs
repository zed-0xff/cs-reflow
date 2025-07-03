using System.Collections.ObjectModel;

using BitType = int;
using UnkBits = UnknownValueBits;

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

    public UnknownValueBitTracker(UnknownTypedValue parent, IEnumerable<BitType>? bits = null) : base(parent.type, bits2span(parent.type, bits))
    {
        if (parent._var_id is null)
            throw new ArgumentException("Parent must have a var_id set.", nameof(parent));

        _var_id = parent._var_id;
        _bits = new(init(bits));
    }

    public UnknownValueBitTracker(TypeDB.IntType type, int var_id, IEnumerable<BitType>? bits = null) : base(type, bits2span(type, bits))
    {
        _var_id = var_id;
        _bits = new(init(bits));
    }

    public UnknownValueBitTracker(TypeDB.IntType type, int var_id, BitSpan bitspan) : base(type, bitspan)
    {
        _var_id = var_id;
        _bits = new(span2bits(init(null), bitspan, ZERO, ONE));
    }

    protected static BitSpan bits2span(TypeDB.IntType type, IEnumerable<BitType>? bits)
    {
        if (bits == null)
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
                default:
                    max |= v;
                    break;
            }

            v <<= 1;
            i++;
        }

        if (enumerator.MoveNext())
            throw new ArgumentException($"bits is longer than expected: expected {type.nbits}");

        return new BitSpan(min, max);
    }

    public override UnknownValueBase WithTag(string key, object? value) => HasTag(key, value) ? this : new UnknownValueBitTracker(this, _bits) { _tags = add_tag(key, value) };
    public override UnknownValueBase WithVarID(int id) => _var_id == id ? this : new UnknownValueBitTracker(type, id, _bits);
    public override UnknownTypedValue WithType(TypeDB.IntType newType) => new UnknownValueBitTracker(newType, _var_id!.Value, _bits.ToList().GetRange(0, newType.nbits));

    public bool HasPrivateBits() => _bits.Any(b => b.IsPrivateBit());

    // expects that _var_id is already set
    List<BitType> init(IEnumerable<BitType>? bits)
    {
        if (bits == null)
        {
            if (_var_id is null)
                throw new ArgumentException("Cannot initialize bits without var_id set.", nameof(bits));
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

    public override object Cast(TypeDB.IntType toType)
    {
        if (toType.nbits == type.nbits)
            return new UnknownValueBitTracker(toType!, _var_id!.Value, _bits);

        return base.Cast(toType);
    }

    public override UnknownTypedValue Upcast(TypeDB.IntType toType)
    {
        var newBits = new List<BitType>(_bits);
        BitType b = (type.signed && toType.signed) ? _bits[type.nbits - 1] : ZERO;
        while (newBits.Count < toType.nbits)
            newBits.Insert(0, b);
        return new UnknownValueBitTracker(toType, _var_id!.Value, newBits);
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

        if (other is UnkBits otherUnk)
        {
            if (type.nbits != otherUnk.type.nbits)
                return false;

            var otherBits = otherUnk.GetBits().ToList();
            for (int i = 0; i < type.nbits; i++)
            {
                if (_bits[i] == otherBits[i] || _bits[i] == ANY || otherBits[i] == UnkBits.ANY)
                    continue;

                return false;
            }
            return true;
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

        if (right is UnkBits otherUnk)
        {
            if (type != otherUnk.type)
                throw new NotImplementedException($"Cannot AND {type} with {otherUnk.type}");

            List<BitType> newBits = new(_bits);
            var otherBitsList = otherUnk.GetBits().ToList();
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBitsList[i]) switch
                {
                    (ZERO, _) => ZERO,
                    (_, UnkBits.ZERO) => ZERO,

                    (ANY, _) => ANY,
                    (_, UnkBits.ANY) => ANY,

                    (_, UnkBits.ONE) => newBits[i],

                    _ => throw new NotImplementedException($"Cannot AND {type} with {otherUnk.type} at bit {i}")
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

        if (right is not UnknownTypedValue otherTyped)
            return UnknownTypedValue.Create(type);

        if (otherTyped.CanConvertTo(this))
            right = otherTyped.ConvertTo<UnknownValueBitTracker>();

        if (right is UnknownValueBitTracker otherBT)
        {
            if (type != otherBT.type)
                throw new NotImplementedException($"Cannot OR {type} with {otherBT.type}");

            List<BitType> newBits = new(_bits);
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBT._bits[i]) switch
                {
                    (ONE, _) or (_, ONE) => ONE,
                    (ANY, _) or (_, ANY) => ANY,

                    (ZERO, ZERO) => ZERO,

                    (ZERO, BitType x) => x,
                    (BitType x, ZERO) => x,

                    (BitType x, BitType y) =>
                        x == y ? x :
                        x == -y ? ONE :
                        ANY
                    // all cases covered
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        right = otherTyped.ToBits();
        if (right is UnkBits otherUnk)
        {
            if (type != otherUnk.type)
                throw new NotImplementedException($"Cannot OR {type} with {otherUnk.type}");

            List<BitType> newBits = new(_bits);
            var otherBitsList = otherUnk.GetBits().ToList();
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBitsList[i]) switch
                {
                    (_, UnkBits.ONE) or (ONE, _) => ONE,
                    (_, UnkBits.ANY) or (ANY, _) => ANY,
                    (_, UnkBits.ZERO) => newBits[i],

                    _ => throw new NotImplementedException($"Cannot AND {type} with {otherUnk.type} at bit {i}")
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

        if (right is UnkBits otherUnk)
        {
            if (type != otherUnk.type)
                throw new NotImplementedException($"Cannot add {type} with {otherUnk.type}");

            return add(otherUnk);
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
                otherTyped = otherTyped.ConvertTo<UnknownValueBitTracker>();

            if (otherTyped is UnknownValueBitTracker otherBT)
                return xor(otherBT);

            if (otherTyped is UnkBits otherUnk)
                return xor(otherUnk);
        }

        return UnknownTypedValue.Create(type);
    }

    public override UnknownValueBase TypedMod(object right)
    {
        throw new NotImplementedException();
    }

    public override UnknownValueBase TypedSub(object right)
    {
        if (TryConvertToLong(right, out long l))
            return add(-l);

        if (right is UnknownTypedValue otherTyped)
            return Add(otherTyped.Negate());

        throw new NotImplementedException();
    }

    public override object Eq(object other)
    {
        if (other is not UnknownValueBitTracker otherBT || otherBT.type != type)
            return base.Eq(other);

        bool inconclusive = false;
        for (int i = 0; i < type.nbits; i++)
        {
            switch (_bits[i], otherBT._bits[i])
            {
                // can still be improved f.ex. when same bit _id_ is compared both with ONE and ZERO
                // i.e. <bbcd> vs <01__>
                case (_, ANY):
                case (ANY, _):
                case (ZERO, ZERO):
                case (ONE, ONE):
                    continue;

                case (ZERO, ONE):
                case (ONE, ZERO):
                    return false;

                case (BitType x1, ONE) when x1.IsPrivateBit():
                case (BitType x2, ZERO) when x2.IsPrivateBit():
                case (ONE, BitType y1) when y1.IsPrivateBit():
                case (ZERO, BitType y2) when y2.IsPrivateBit():
                    inconclusive = true; // but still maybe false if bit is compared with inverted self in other cases
                    continue;

                case (BitType x, BitType y) when x.IsPrivateBit() && y.IsPrivateBit():
                    if (x == y)
                        continue;
                    if (x == -y)
                        return false;
                    inconclusive = true; // different bit ids
                    break;
            }
        }
        return inconclusive ? UnknownValue.Create(TypeDB.Bool) : true;
    }

    public override bool Equals(object? obj) => (obj is UnknownValueBitTracker other) && type.Equals(other.type) && _var_id == other._var_id && _bits.SequenceEqual(other._bits);

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

