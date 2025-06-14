public abstract class UnknownTypedValue : UnknownValueBase
{
    public TypeDB.IntInfo type { get; }

    public UnknownTypedValue(TypeDB.IntInfo type) : base()
    {
        this.type = type;
    }

    public static UnknownTypedValue Create(TypeDB.IntInfo type) => new UnknownValueRange(type);

    public static readonly long MAX_DISCRETE_CARDINALITY = 1_000_000L;

    public long MaskNoSign(long value)
    {
        return value & type.Mask;
    }

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

    public override object Eq(object right)
    {
        if (TryConvertToLong(right, out long l))
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

        return right switch
        {
            UnknownTypedValue r => IntersectsWith(r) ? UnknownValue.Create("bool") : false,
            _ => UnknownValue.Create("bool")
        };
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

    public override UnknownValueBase Mul(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            if (l == 0) return new UnknownValueList(type, new List<long> { 0 });
            if (l == 1) return this;

            if (Cardinality() > MAX_DISCRETE_CARDINALITY && TryGetTailPow2(l, out int pow2))
            {
                return new UnknownValueBits(type, 0, (1 << pow2) - 1);
            }
        }

        if (Cardinality() > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(type);

        if (TryConvertToLong(right, out l))
            return new UnknownValueList(type, Values().Select(v => MaskWithSign(v * l)));

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

        return new UnknownValueList(type, values.OrderBy(x => x).ToList());
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
            return new UnknownValueList(type, Values().Select(v => v & mask));

        return UnknownValueBits.CreateFromAnd(type, mask);
    }

    public override UnknownValueBase BitwiseOr(object right)
    {
        if (!TryConvertToLong(right, out long mask))
            return UnknownValue.Create(type);

        if (Cardinality() <= MAX_DISCRETE_CARDINALITY)
            return new UnknownValueList(type, Values().Select(v => v | mask));

        return UnknownValueBits.CreateFromOr(type, mask);
    }

    public override UnknownValueBase Sub(object right)
    {
        if (right is not UnknownTypedValue otherTyped)
            return UnknownValue.Create(type);

        long resultCardinality = Cardinality() * otherTyped.Cardinality();
        if (resultCardinality > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(type);

        return new UnknownValueList(type,
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
