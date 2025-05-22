public class UnknownValueBits : UnknownTypedValue
{
    private List<sbyte> bits;

    public UnknownValueBits(IntInfo type, IEnumerable<sbyte>? bits = null) : base(type)
    {
        init(bits);
    }

    public UnknownValueBits(IntInfo type, long val, long mask) : base(type)
    {
        init(null);
        for (int i = 0; i < type.nbits; i++)
        {
            if ((mask & (1L << i)) != 0)
            {
                bits[i] = (sbyte)((val & (1L << i)) != 0 ? 1 : 0);
            }
        }
    }

    public UnknownValueBits(string type, IEnumerable<sbyte>? bits = null) : base(type)
    {
        init(bits);
    }

    public static UnknownValueBits CreateFromAnd(IntInfo type, long mask)
    {
        var bits = new sbyte[type.nbits];
        for (int i = 0; i < type.nbits; i++)
            bits[i] = (sbyte)(((mask & (1L << i)) != 0) ? -1 : 0);
        return new UnknownValueBits(type, bits);
    }

    public static UnknownValueBits CreateFromOr(IntInfo type, long mask)
    {
        var bits = new sbyte[type.nbits];
        for (int i = 0; i < type.nbits; i++)
            bits[i] = (sbyte)(((mask & (1L << i)) != 0) ? 1 : -1);
        return new UnknownValueBits(type, bits);
    }

    void init(IEnumerable<sbyte>? bits)
    {
        if (bits == null)
            bits = Enumerable.Repeat<sbyte>(-1, type.nbits);

        if (bits.Count() != type.nbits)
            throw new ArgumentException($"Expected {type.nbits} bits, but got {bits.Count()} bits.");

        this.bits = new List<sbyte>(bits);
    }

    public override string ToString()
    {
        string result = $"UnknownValueBits<{type}>[";
        int start = type.nbits - 1;
        while (start >= 0 && bits[start] == -1)
        {
            start--;
        }
        for (int i = start; i >= 0; i--)
        {
            result += bits[i] == -1 ? "_" : bits[i].ToString();
        }

        result += "]";
        return result;
    }

    public void SetBit(int idx, sbyte value)
    {
        if (idx < 0 || idx >= type.nbits)
            throw new ArgumentOutOfRangeException($"Index {idx} out of range for {type.nbits} bits.");

        bits[idx] = value;
    }

    public (long, long) MaskVal(bool minimal = false)
    {
        long mask = 0;
        long val = 0;
        for (int i = 0; i < type.nbits; i++)
        {
            if (bits[i] != -1)
            {
                mask |= 1L << i;
                val |= (long)bits[i] << i;
            }
            else if (minimal)
            {
                // all further bits will be uncertain, so no need to decode them
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

    public override object Cast(string toType)
    {
        if (GetType(toType).nbits == type.nbits)
            return new UnknownValueBits(toType, bits);

        return base.Cast(toType);
    }

    public override bool Contains(long value)
    {
        var (mask, val) = MaskVal();
        return ((value & mask) == val);
    }

    public override long Cardinality()
    {
        long cardinality = 1;
        for (int i = 0; i < type.nbits; i++)
        {
            if (bits[i] == -1)
            {
                cardinality *= 2;
            }
        }
        return cardinality;
    }

    public override IEnumerable<long> Values()
    {
        var (mask, val) = MaskVal();

        // Determine the bit positions that are "any" (i.e., mask bit = 1)
        List<int> floatingBits = new();
        for (int i = 0; i < bits.Count; i++)
        {
            if (bits[i] == -1)
                floatingBits.Add(i);
        }

        int floatingCount = floatingBits.Count;
        long combinations = 1L << floatingCount;

        for (long i = 0; i < combinations; i++)
        {
            long dynamicPart = 0;
            for (int j = 0; j < floatingCount; j++)
            {
                if (((i >> j) & 1) != 0)
                    dynamicPart |= (1L << floatingBits[j]);
            }

            yield return (val & ~mask) | (dynamicPart & mask);
        }
    }

    public override long Min()
    {
        var (mask, val) = MaskVal();
        if (type.signed && bits[type.nbits - 1] == -1)
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
        if (type.signed && bits[type.nbits - 1] == -1)
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

    public override UnknownTypedValue ShiftLeft(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<sbyte> newBits = new(bits);
        for (int i = 0; i < l; i++)
        {
            newBits.Insert(0, 0);
            newBits.RemoveAt(bits.Count - 1);
        }
        return new UnknownValueBits(type, newBits);
    }

    public override UnknownTypedValue UnsignedShiftRight(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<sbyte> newBits = new(bits);
        for (int i = 0; i < l; i++)
        {
            newBits.RemoveAt(0);
            newBits.Add(0);
        }
        return new UnknownValueBits(type, newBits);
    }

    public override UnknownValueBase BitwiseAnd(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<sbyte> newBits = new(bits);
        for (int i = 0; i < type.nbits; i++)
        {
            if ((l & (1L << i)) == 0)
            {
                newBits[i] = 0;
            }
        }
        return new UnknownValueBits(type, newBits);
    }

    public override UnknownValueBase BitwiseOr(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<sbyte> newBits = new(bits);
        for (int i = 0; i < type.nbits; i++)
        {
            if ((l & (1L << i)) != 0)
            {
                newBits[i] = 1;
            }
        }
        return new UnknownValueBits(type, newBits);
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

    private UnknownValueBase calc_symm(Func<long, long, long> op, object right, long identity, UnknownValueBase id_val)
    {
        switch (right)
        {
            case UnknownValueBits otherBits:
                var (mask1, val1) = MaskVal(true);
                var (mask2, val2) = otherBits.MaskVal(true);
                var newMask = mask1 & mask2;
                return new UnknownValueBits(type, op(val1, val2), newMask);

            case UnknownValueList otherList:
                if (Cardinality() > MAX_DISCRETE_CARDINALITY)
                    return UnknownValue.Create(type);

                var newValues = new HashSet<long>();
                foreach (var v1 in Values())
                {
                    foreach (var v2 in otherList.Values())
                    {
                        newValues.Add(Mask(op(v1, v2)));
                        if (newValues.Count > MAX_DISCRETE_CARDINALITY)
                            return UnknownValue.Create(type);
                    }
                }
                return new UnknownValueList(type, newValues.OrderBy(x => x).ToList());
        }

        return calc_asym(op, right, identity, id_val);
    }

    public override UnknownValueBase Add(object right)
    {
        if (right == this) // '==' and not 'equals' because we want to use the same instance
            return ShiftLeft(1);

        return calc_symm((a, b) => a + b, right, 0, this);
    }

    public override UnknownValueBase Xor(object right)
    {
        if (right == this) // '==' and not 'equals' because we want to use the same instance
            return new UnknownValueList(type, new List<long> { 0 });

        return calc_symm((a, b) => a ^ b, right, 0, this);
    }

    public override UnknownValueBase Mul(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            if (l == 0) return new UnknownValueList(type, new List<long> { 0 });
            if (l == 1) return this;

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

    public override UnknownValueBase Div(object right) => calc_asym((a, b) => a / b, right, 1, this);
    public override UnknownValueBase Mod(object right) => calc_asym((a, b) => a % b, right, 1, new UnknownValueList(type, new List<long> { 0 }));
    public override UnknownValueBase Sub(object right) => calc_asym((a, b) => a - b, right, 0, this);

    public override bool Equals(object obj) => (obj is UnknownValueBits other) && type.Equals(other.type) && bits.SequenceEqual(other.bits);

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
