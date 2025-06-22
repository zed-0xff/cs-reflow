public abstract class UnknownTypedValue : UnknownValueBase
{
    public static readonly long MAX_DISCRETE_CARDINALITY = 1_000_000L;

    public TypeDB.IntInfo type { get; }
    public TypeDB.IntInfo Type => type;

    public UnknownTypedValue(TypeDB.IntInfo type) : base()
    {
        this.type = type;
    }

    public abstract UnknownValueBase TypedAdd(object right);
    public abstract UnknownValueBase TypedDiv(object right);
    public abstract UnknownValueBase TypedMod(object right);
    // public abstract UnknownValueBase TypedMul(object right);
    // public abstract UnknownValueBase TypedSub(object right);
    // public abstract UnknownValueBase TypedXor(object right);
    // 
    // public abstract UnknownValueBase TypedBitwiseAnd(object right);
    // public abstract UnknownValueBase TypedBitwiseOr(object right);
    // public abstract UnknownValueBase TypedShiftLeft(object right);
    // public abstract UnknownValueBase TypedSignedShiftRight(object right);
    // public abstract UnknownValueBase TypedUnsignedShiftRight(object right);

    public static UnknownValueRange Create(TypeDB.IntInfo type) => new UnknownValueRange(type);

    static readonly Dictionary<TypeDB.IntInfo, UnknownTypedValue> _zeroes = new();
    static readonly Dictionary<TypeDB.IntInfo, UnknownTypedValue> _ones = new();

    public static UnknownTypedValue Zero(TypeDB.IntInfo type) =>
        _zeroes.TryGetValue(type, out var zero) ? zero :
        (_zeroes[type] = new UnknownValueRange(type, 0, 0));

    public static UnknownTypedValue One(TypeDB.IntInfo type) =>
        _ones.TryGetValue(type, out var one) ? one :
        (_ones[type] = new UnknownValueRange(type, 1, 1));

    public UnknownValueBitsBase ToBits() => new UnknownValueBits(type);
    // (this is UnknownValueBitsBase bits) ? bits :
    // (_var_id == null) ? new UnknownValueBits(type) : new UnknownValueBitTracker(type, _var_id.Value);

    public bool IsZero() => Equals(Zero(type));
    public bool IsOne() => Equals(One(type));

    public bool IsOverflow(long value) => !type.CanFit(value);
    public long MaskNoSign(long value) => value & type.Mask;

    public long MaskWithSign(long value)
    {
        value = MaskNoSign(value);
        if (type.signed && (value & (1L << (type.nbits - 1))) != 0)
        {
            value |= ~type.Mask;
        }
        return value;
    }

    public long Capacity()
    {
        return 1L << type.nbits;
    }

    public static bool IsTypeSupported(string typeName)
    {
        return TypeDB.TryFind(typeName) is not null;
    }

    public abstract override bool Contains(long value);
    public abstract bool IntersectsWith(UnknownTypedValue right);
    public abstract override long Min();
    public abstract override long Max();

    public override object Eq(object other)
    {
        if (TryConvertToLong(other, out long l))
        {
            if (Contains(l))
            {
                if (Cardinality() == 1)
                    return true;
                else
                    return UnknownValue.Create("bool");
            }
            else
            {
                return false;
            }
        }

        if (other is UnknownTypedValue r)
        {
            if (Cardinality() == 1 && r.Cardinality() == 1 && Values().First() == r.Values().First())
                return true;

            if (!IntersectsWith(r))
                return false;
        }
        ;
        return UnknownValue.Create("bool");
    }

    public override object Gt(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            if (Min() > l)
                return true;

            if (Max() < l)
                return false;
        }

        return UnknownValue.Create("bool");
    }

    public override object Lt(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            if (Max() < l)
                return true;

            if (Min() > l)
                return false;
        }

        return UnknownValue.Create("bool");
    }

    bool TryGetTailPow2(long value, out int pow2)
    {
        pow2 = 0;
        if (value == 0) return false;

        while ((value & 1) == 0)
        {
            value >>= 1;
            pow2++;
        }

        return (pow2 > 0);
    }

    // sealed to prevent overriding in derived classes
    public sealed override UnknownValueBase Add(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (TryConvertToLong(right, out long l) && (l == 0))
            return this;

        if (right is UnknownTypedValue otherTyped && otherTyped.IsZero())
            return this;

        if (right == this)
            return ShiftLeft(1);

        return TypedAdd(right);
    }

    public sealed override UnknownValueBase Mod(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return UnknownValue.Create(); // division by zero
            if (l == 1 || l == -1)
                return Zero(type);
        }

        if (right is UnknownTypedValue otherTyped)
        {
            if (otherTyped.IsZero())
                return UnknownValue.Create(); // division by zero
            if (otherTyped.IsOne() || otherTyped == this)
                return Zero(type);
        }

        return TypedMod(right);
    }

    public sealed override UnknownValueBase Div(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return UnknownValue.Create(); // division by zero
            if (l == 1)
                return this;
            if (l == -1)
                return Negate();
        }

        if (right is UnknownTypedValue otherTyped)
        {
            if (otherTyped.IsZero())
                return UnknownValue.Create(); // division by zero
            if (otherTyped.IsOne())
                return this;
            if (otherTyped == this)
                return One(type);
        }

        return TypedDiv(right);
    }

    public override UnknownValueBase Mul(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            if (l == 0) return Zero(type);
            if (l == 1) return this;

            if (Cardinality() > MAX_DISCRETE_CARDINALITY && TryGetTailPow2(l, out int pow2)) // TODO
            {
                return new UnknownValueBits(type, 0, (1 << pow2) - 1);
            }
        }

        if (Cardinality() > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(type);

        if (TryConvertToLong(right, out l))
            return new UnknownValueSet(type, Values().Select(v => MaskWithSign(v * l)));

        if (right is not UnknownTypedValue ru)
            return UnknownValue.Create(type);

        HashSet<long> values = new();
        foreach (long v in Values())
        {
            foreach (long r in ru.Values())
            {
                long maskedValue = MaskWithSign(v * r);
                if (values.Count >= MAX_DISCRETE_CARDINALITY)
                    return UnknownValue.Create(type);

                values.Add(maskedValue);
            }
        }

        return new UnknownValueSet(type, values.OrderBy(x => x).ToList());
    }

    public override object Cast(TypeDB.IntInfo toType)
    {
        if (toType == TypeDB.Bool)
        {
            switch (Cardinality())
            {
                case 0:
                    return UnknownValue.Create(TypeDB.Bool);
                case 1:
                    return !Contains(0);
                default:
                    return Contains(0) ? UnknownValue.Create(TypeDB.Bool) : true;
            }
        }
        throw new NotImplementedException($"{ToString()}.Cast({toType}): not implemented.");
    }

    public override UnknownValueBase BitwiseAnd(object right)
    {
        if (!TryConvertToLong(right, out long mask))
            return UnknownValue.Create(type);

        if (Cardinality() <= MAX_DISCRETE_CARDINALITY)
            return new UnknownValueSet(type, Values().Select(v => v & mask));

        return UnknownValueBits.CreateFromAnd(type, mask);
    }

    public override UnknownValueBase BitwiseOr(object right)
    {
        if (!TryConvertToLong(right, out long mask))
            return UnknownValue.Create(type);

        if (Cardinality() <= MAX_DISCRETE_CARDINALITY)
            return new UnknownValueSet(type, Values().Select(v => v | mask));

        return UnknownValueBits.CreateFromOr(type, mask);
    }

    public override UnknownValueBase Sub(object right)
    {
        if (right == this)
            return Zero(type);

        if (right is not UnknownTypedValue otherTyped)
            return UnknownValue.Create(type);

        long resultCardinality = Cardinality() * otherTyped.Cardinality();
        if (resultCardinality > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(type);

        return new UnknownValueSet(type,
                Values()
                .SelectMany(l => otherTyped.Values(), (l, r) => MaskWithSign(l - r)));
    }

    public override UnknownValueBase Merge(object other)
    {
        return other switch
        {
            UnknownTypedValue otherTyped => (otherTyped.type == type) ? UnknownValue.Create(type) : UnknownValue.Create(),
            _ => base.Merge(other)
        };
    }
}
