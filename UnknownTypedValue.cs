public abstract class UnknownTypedValue : UnknownValueBase
{
    public string Type { get; }
    public int nbits { get; }
    public bool signed { get; }

    public UnknownTypedValue(string type) : base()
    {
        Type = ShortType(type);
        if (!INFOS.TryGetValue(Type, out IntInfo info))
            throw new NotImplementedException($"UnknownTypedValue: {Type} not implemented.");

        nbits = info.nbits;
        signed = info.signed;
    }

    public static UnknownTypedValue Create(string type)
    {
        return new UnknownValueRange(type);
    }

    public static readonly ulong MAX_DISCRETE_CARDINALITY = 1_000_000;

    public class IntInfo
    {
        public int nbits { get; init; }
        public bool signed { get; init; }
    }

    static readonly Dictionary<string, IntInfo> INFOS = new()
    {
        ["bool"] = new IntInfo { nbits = 1, signed = false },
        ["byte"] = new IntInfo { nbits = 8, signed = true },
        ["sbyte"] = new IntInfo { nbits = 8, signed = true },
        ["short"] = new IntInfo { nbits = 16, signed = true },
        ["ushort"] = new IntInfo { nbits = 16, signed = false },
        ["int"] = new IntInfo { nbits = 32, signed = true },
        ["uint"] = new IntInfo { nbits = 32, signed = false },
        ["long"] = new IntInfo { nbits = 64, signed = true },
        ["ulong"] = new IntInfo { nbits = 64, signed = false },
        ["nint"] = new IntInfo { nbits = 32, signed = true }, // TODO: 32/64 bit cmdline switch
        ["nuint"] = new IntInfo { nbits = 32, signed = false }, // TODO: 32/64 bit cmdline switch
    };

    public long Mask(long value)
    {
        return value & ((1L << nbits) - 1);
    }

    public static bool IsTypeSupported(string type)
    {
        return INFOS.ContainsKey(ShortType(type));
    }

    public static LongRange Type2Range(string type)
    {
        if (INFOS.TryGetValue(type, out IntInfo info))
            return new LongRange(info.nbits, info.signed);
        else
            throw new NotImplementedException($"UnknownValueRange: {type} not implemented.");
    }

    protected static string ShortType(string type)
    {
        return type switch
        {
            "System.Boolean" => "bool",
            "System.Byte" => "byte",
            "System.Int32" => "int",
            "System.SByte" => "sbyte",
            "System.UInt32" => "uint",
            _ => type,
        };
    }

    public abstract bool Contains(long value);

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

    public abstract bool IntersectsWith(UnknownTypedValue right);

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

    public override UnknownValueBase Mul(object right)
    {
        if (Cardinality() > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(Type);

        if (TryConvertToLong(right, out long l))
            return new UnknownValueList(Type, Values().Select(v => Mask(v * l)).Distinct().OrderBy(x => x).ToList());

        if (right is not UnknownTypedValue ru)
            return new UnknownValueRange(Type);

        HashSet<long> values = new();
        foreach (long v in Values())
        {
            foreach (long r in ru.Values())
            {
                long maskedValue = Mask(v * r);
                if (values.Count >= (int)MAX_DISCRETE_CARDINALITY)
                    return UnknownValue.Create(Type);

                values.Add(maskedValue);
            }
        }

        return new UnknownValueList(Type, values.OrderBy(x => x).ToList());
    }

    public override object Cast(string toType)
    {
        toType = ShortType(toType);
        if (toType == "bool")
        {
            switch (Cardinality())
            {
                case 0:
                    return UnknownValue.Create("bool");
                case 1:
                    return !Contains(0);
                default:
                    return Contains(0) ? UnknownValue.Create("bool") : true;
            }
        }
        throw new NotImplementedException($"{ToString()}.Cast({toType}): not implemented.");
    }

    public override UnknownValueBase BitwiseAnd(object right)
    {
        if (!TryConvertToLong(right, out long mask))
            return UnknownValue.Create(Type);

        if (Cardinality() <= MAX_DISCRETE_CARDINALITY)
            return new UnknownValueList(Type, Values().Select(v => v & mask).Distinct().OrderBy(x => x).ToList());

        if (Mask2Cardinality(mask) > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(Type);

        return new UnknownValueList(Type, Mask2List(mask));
    }

    public ulong Mask2Cardinality(long mask)
    {
        // count nonzero bits in l
        int bitCount = 0;
        for (int i = 0; i < nbits; i++)
        {
            if ((mask & (1L << i)) != 0)
                bitCount++;
        }
        return 1UL << bitCount;
    }

    public List<long> Mask2List(long mask)
    {
        List<int> bitPositions = new();

        // Collect positions of all set bits
        for (int i = 0; i < 64; i++)
        {
            if ((mask & (1L << i)) != 0)
                bitPositions.Add(i);
        }

        int bitCount = bitPositions.Count;
        ulong total = 1UL << bitCount;
        List<long> result = new((int)total);

        // Generate all combinations
        for (ulong i = 0; i < total; i++)
        {
            long value = 0;
            for (int j = 0; j < bitCount; j++)
            {
                if (((i >> j) & 1) != 0)
                    value |= 1L << bitPositions[j];
            }
            result.Add(value);
        }

        return result;
    }
}
