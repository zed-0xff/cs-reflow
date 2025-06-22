using System.Collections.ObjectModel;

using BitType = int;

public class UnknownValueBitTracker : UnknownValueBitsBase
{
    private const BitType ANY = -1;
    private const BitType ONE = 1;
    private const BitType ZERO = 0;

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
    readonly int _var_id;

    public ReadOnlyCollection<BitType> Bits => _bits; // for tests only?

    public UnknownValueBitTracker(TypeDB.IntInfo type, int var_id, IEnumerable<BitType>? bits = null) : base(type)
    {
        _var_id = var_id;
        _bits = new(init(bits));
    }

    public override UnknownValueBase WithTag(object? tag) => Equals(_tag, tag) ? this : new UnknownValueBitTracker(type, _var_id, _bits) { _tag = tag };
    public override UnknownValueBase WithVarID(int id) => _var_id == id ? this : new UnknownValueBitTracker(type, id, _bits);

    public override bool IsFullRange() => _bits.All(b => b == ANY);

    // expects that _var_id is already set
    List<BitType> init(IEnumerable<BitType>? bits)
    {
        if (bits == null)
        {
            return Enumerable.Range(2 + _var_id * type.nbits, type.nbits).Reverse().ToList();
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

        return new UnknownValueBitTracker(type, _var_id, _bits.Select((b, i) => i == idx ? value : b));
    }

    public (long, long) MaskVal(bool minimal = false)
    {
        long mask = 0;
        long val = 0;
        for (int i = 0; i < type.nbits; i++)
        {
            if (_bits[i] != ANY)
            {
                mask |= 1L << i;
                val |= (long)_bits[i] << i;
            }
            else if (minimal)
            {
                // all further _bits will be uncertain, so no need to decode them
                return (mask, val);
            }
        }
        return (mask, extend_sign(val));
    }

    long extend_sign(long v)
    {
        if (type.signed)
        {
            long sign_bit = (1L << (type.nbits - 1));
            if ((v & sign_bit) != 0)
            {
                v |= ~type.Mask;
            }
        }
        return v;
    }

    public override object Cast(TypeDB.IntInfo toType)
    {
        if (toType.nbits == type.nbits)
            return new UnknownValueBitTracker(toType, _var_id, _bits);

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
        {
            if (_bits[i] == ANY)
            {
                cardinality <<= 1;
            }
        }
        return cardinality;
    }

    public override IEnumerable<long> Values()
    {
        var (mask, val) = MaskVal();

        // Determine the bit positions that are "any" (i.e., mask bit = 1)
        List<int> floatingBits = new();
        for (int i = 0; i < _bits.Count; i++)
        {
            if (_bits[i] == ANY)
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
        if (type.signed && _bits[type.nbits - 1] == ANY)
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
        if (type.signed && _bits[type.nbits - 1] == ANY)
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

    public override bool IntersectsWith(UnknownTypedValue other)
    {
        var (mask, val) = MaskVal();
        if (other is UnknownValueBitTracker otherBits)
        {
            var (other_mask, other_val) = otherBits.MaskVal();
            var commonMask = mask & other_mask;
            if ((val & commonMask) != (other_val & commonMask))
                return false;
        }
        return other.Values().Any(v => ((v & mask) == val));
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
        return new UnknownValueBitTracker(type, _var_id, newBits);
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
        return new UnknownValueBitTracker(type, _var_id, newBits);
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
        return new UnknownValueBitTracker(type, _var_id, newBits);
    }

    public override UnknownValueBase TypedBitwiseAnd(object right)
    {
        throw new NotImplementedException();
    }

    public override UnknownValueBase TypedBitwiseOr(object right)
    {
        throw new NotImplementedException();
    }

    public override UnknownValueBitTracker BitwiseNot() =>
        new UnknownValueBitTracker(type, _var_id, _bits.Select(b => b switch
        {
            ANY => ANY,
            ONE => ZERO,
            ZERO => ONE,
            _ => -b // invert the bit id
        }));

    public override UnknownValueBase Negate() => BitwiseNot().Add(1);

    private UnknownValueBase calc_symm(Func<long, long, long> op, object right, long identity, UnknownValueBase id_val, bool useMinMask = true)
    {
        throw new NotImplementedException();
    }

    public override UnknownValueBase TypedAdd(object right)
    {
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
            return new UnknownValueBitTracker(type, _var_id, newBits);
        }

        if (right is UnknownValueBitTracker other)
        {
            // XOR with another UnknownValueBitTracker
            if (type != other.type)
                return UnknownValue.Create(); // TODO: type promotion

            List<BitType> newBits = new(_bits);
            for (int i = 0; i < type.nbits; i++)
            {
                if (_bits[i] == ANY || other._bits[i] == ANY)
                    newBits[i] = ANY;
                else
                    newBits[i] = (newBits[i], other._bits[i]) switch
                    {
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
            return new UnknownValueBitTracker(type, _var_id, newBits);
        }

        return UnknownTypedValue.Create(type);
    }

    public override UnknownValueBase TypedMul(object right)
    {
        throw new NotImplementedException();
    }

    public override UnknownValueBase TypedMod(object right)
    {
        throw new NotImplementedException();
    }

    private UnknownValueBase calc_asym(Func<long, long, long> op, object right, long identity, UnknownValueBase id_val)
    {
        throw new NotImplementedException();
    }

    public override UnknownValueBase TypedDiv(object right) => calc_asym((a, b) => a / b, right, 1, this);
    public override UnknownValueBase TypedSub(object right) => calc_asym((a, b) => a - b, right, 0, this);

    public override bool Equals(object obj) => (obj is UnknownValueBitTracker other) && type.Equals(other.type) && _var_id == other._var_id && _bits.SequenceEqual(other._bits);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(type);
        foreach (var b in _bits)
            hash.Add(b);
        return hash.ToHashCode();
    }

    public override UnknownValueBase Merge(object other)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        string result = $"UnknownValueBitTracker<{type}>[";
        int start = type.nbits - 1;
        while (start >= 0 && _bits[start] == ANY)
        {
            start--;
        }
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
