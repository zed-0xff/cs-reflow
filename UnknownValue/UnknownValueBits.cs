using System.Collections.ObjectModel;

public class UnknownValueBits : UnknownValueBitsBase
{
    private const sbyte ANY = -1;
    private const sbyte ONE = 1;
    private const sbyte ZERO = 0;

    readonly ReadOnlyCollection<sbyte> _bits;

    public ReadOnlyCollection<sbyte> Bits => _bits;

    public UnknownValueBits(TypeDB.IntInfo type, IEnumerable<sbyte>? bits = null) : base(type)
    {
        _bits = new(init(bits));
    }

    public UnknownValueBits(TypeDB.IntInfo type, long val, long mask) : base(type) // mask represents the known _bits
    {
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

    public override UnknownValueBase WithTag(object? tag) => Equals(_tag, tag) ? this : new UnknownValueBits(type, _bits) { _tag = tag };
    public override UnknownValueBase WithVarID(int id) => _var_id == id ? this : new UnknownValueBits(type, _bits) { _var_id = id };

    public override bool IsFullRange() => _bits.All(b => b == ANY);

    public static UnknownValueBits CreateFromAnd(TypeDB.IntInfo type, long mask)
    {
        var _bits = new sbyte[type.nbits];
        for (int i = 0; i < type.nbits; i++)
            _bits[i] = (sbyte)(((mask & (1L << i)) != 0) ? ANY : ZERO);
        return new UnknownValueBits(type, _bits);
    }

    public static UnknownValueBits CreateFromOr(TypeDB.IntInfo type, long mask)
    {
        var _bits = new sbyte[type.nbits];
        for (int i = 0; i < type.nbits; i++)
            _bits[i] = (sbyte)(((mask & (1L << i)) != 0) ? ONE : ANY);
        return new UnknownValueBits(type, _bits);
    }

    List<sbyte> init(IEnumerable<sbyte>? bits)
    {
        if (bits == null)
            bits = Enumerable.Repeat<sbyte>(ANY, type.nbits);

        if (bits.Count() != type.nbits)
            throw new ArgumentException($"Expected {type.nbits} _bits, but got {bits.Count()} _bits.");

        return new List<sbyte>(bits);
    }

    public override string ToString()
    {
        string result = $"UnknownValueBits<{type}>[";
        int start = type.nbits - 1;
        while (start >= 0 && _bits[start] == ANY)
        {
            start--;
        }
        for (int i = start; i >= 0; i--)
        {
            result += _bits[i] == ANY ? "_" : _bits[i].ToString();
        }

        result += "]";
        return result;
    }

    // returns new UnknownValueBits with the bit at idx set to value
    public UnknownValueBits SetBit(int idx, sbyte value)
    {
        if (idx < 0 || idx >= type.nbits)
            throw new ArgumentOutOfRangeException($"Index {idx} out of range for {type.nbits} _bits.");

        return new UnknownValueBits(type, _bits.Select((b, i) => i == idx ? value : b));
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
            return new UnknownValueBits(toType, _bits);

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
        if (other is UnknownValueBits otherBits)
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

        List<sbyte> newBits = new(_bits);
        for (int i = 0; i < l; i++)
        {
            newBits.RemoveAt(_bits.Count - 1);
            newBits.Insert(0, ZERO); // should be after remove!
        }
        return new UnknownValueBits(type, newBits);
    }

    public override UnknownTypedValue TypedSignedShiftRight(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        sbyte sign = type.signed ? _bits[type.nbits - 1] : ZERO;
        List<sbyte> newBits = new(_bits);
        for (int i = 0; i < l; i++)
        {
            newBits.RemoveAt(0);
            newBits.Add(sign);
        }
        return new UnknownValueBits(type, newBits);
    }

    public override UnknownTypedValue TypedUnsignedShiftRight(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<sbyte> newBits = new(_bits);
        for (int i = 0; i < l; i++)
        {
            newBits.RemoveAt(0);
            newBits.Add(ZERO);
        }
        return new UnknownValueBits(type, newBits);
    }

    public override UnknownValueBase TypedBitwiseAnd(object right)
    {
        if (right is UnknownValueBits otherBits)
        {
            var (mask1, val1) = MaskVal();
            var (mask2, val2) = otherBits.MaskVal();
            return new UnknownValueBits(type, val1 & val2, mask1 & mask2);
        }

        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<sbyte> newBits = new(_bits);
        for (int i = 0; i < type.nbits; i++)
        {
            if ((l & (1L << i)) == 0)
            {
                newBits[i] = ZERO;
            }
        }
        return new UnknownValueBits(type, newBits);
    }

    public override UnknownValueBase TypedBitwiseOr(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<sbyte> newBits = new(_bits);
        for (int i = 0; i < type.nbits; i++)
        {
            if ((l & (1L << i)) != 0)
            {
                newBits[i] = ONE;
            }
        }
        return new UnknownValueBits(type, newBits);
    }

    public override UnknownValueBase BitwiseNot()
    {
        var (mask, val) = MaskVal();
        return new UnknownValueBits(type, ~val, mask);
    }

    public override UnknownValueBase Negate() => BitwiseNot().Add(1);

    private UnknownValueBase calc_symm(Func<long, long, long> op, object right, long identity, UnknownValueBase id_val, bool useMinMask = true)
    {
        switch (right)
        {
            case UnknownValueBits otherBits:
                var (mask1, val1) = MaskVal(useMinMask);
                var (mask2, val2) = otherBits.MaskVal(useMinMask);
                var newMask = mask1 & mask2;
                return new UnknownValueBits(type, op(val1, val2), newMask);

            case UnknownValueSet otherList:
                if (Cardinality() > MAX_DISCRETE_CARDINALITY)
                    return UnknownValue.Create(type);

                var newValues = new HashSet<long>();
                foreach (var v1 in Values())
                {
                    foreach (var v2 in otherList.Values())
                    {
                        newValues.Add(MaskWithSign(op(v1, v2)));
                        if ((ulong)newValues.Count > MAX_DISCRETE_CARDINALITY)
                            return UnknownValue.Create(type);
                    }
                }
                return new UnknownValueSet(type, newValues.OrderBy(x => x).ToList());
        }

        return calc_asym(op, right, identity, id_val);
    }

    public override UnknownValueBase TypedAdd(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            // bitwise add until carry to unknown bit
            var newBits = Enumerable.Repeat<sbyte>(ANY, type.nbits).ToList();
            sbyte carry = 0;
            for (int i = 0; i < type.nbits; i++, l >>= 1)
            {
                var add = carry + (l & 1);
                if (_bits[i] == ANY)
                {
                    if (add != 0)
                        break;
                }
                else
                {
                    var sum = _bits[i] + add;
                    newBits[i] = (sum & 1) == 1 ? ONE : ZERO;
                    carry = (sbyte)(sum >> 1);
                }
            }
            return new UnknownValueBits(type, newBits);
        }

        return calc_symm((a, b) => a + b, right, 0, this);
    }

    public override UnknownValueBase TypedXor(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            // just do a bit-by-bit xor on known _bits, no carry involved
            var newBits = new List<sbyte>(_bits);
            int i = 0;
            while (l != 0 && i < type.nbits)
            {
                if (newBits[i] != ANY)
                {
                    newBits[i] ^= (sbyte)(l & 1);
                }
                l >>= 1;
                i++;
            }
            return new UnknownValueBits(type, newBits);
        }

        return calc_symm((a, b) => a ^ b, right, 0, this, false);
    }

    public override UnknownValueBase TypedMul(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            var (minMask, val) = MaskVal(true);
            val *= l;
            var newMask = minMask;

            l >>= 1;
            while (l > 0)
            {
                newMask = (newMask << 1) | 1;
                l >>= 1;
            }
            return new UnknownValueBits(type, val, newMask);
        }

        return calc_symm((a, b) => a * b, right, 1, this);
    }

    public override UnknownValueBase TypedMod(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        if (l <= 0)
            return UnknownValue.Create();

        // TODO: apply knowledge of known _bits
        return type.signed ? new UnknownValueRange(type, -l + 1, l - 1) : new UnknownValueRange(type, 0, l - 1);
    }

    private UnknownValueBase calc_asym(Func<long, long, long> op, object right, long identity, UnknownValueBase id_val)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        if (l == identity)
            return id_val;

        var (minMask, val) = MaskVal(true);
        return new UnknownValueBits(type, op(val, l), minMask); // apply conservative mask
    }

    public override UnknownValueBase TypedDiv(object right) => calc_asym((a, b) => a / b, right, 1, this);
    public override UnknownValueBase TypedSub(object right) => calc_asym((a, b) => a - b, right, 0, this);

    public override bool Equals(object obj) => (obj is UnknownValueBits other) && type.Equals(other.type) && _bits.SequenceEqual(other._bits);

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
        return other switch
        {
            // TODO: narrower range with new class, that contains a list of UnknownValueBase
            UnknownValueBits otherBits => new UnknownValueBits(type, _bits.Zip(otherBits._bits, (b1, b2) => (sbyte)(b1 == b2 ? b1 : ANY)).ToList()),
            _ => base.Merge(other)
        };
    }
}
