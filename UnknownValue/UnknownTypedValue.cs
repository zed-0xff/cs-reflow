public abstract class UnknownTypedValue : UnknownValueBase
{
    public IntInfo type { get; }

    public UnknownTypedValue(string typeName) : base()
    {
        type = GetType(typeName);
    }

    public UnknownTypedValue(IntInfo type) : base()
    {
        this.type = type;
    }

    public static UnknownTypedValue Create(string typeName) => new UnknownValueRange(typeName);
    public static UnknownTypedValue Create(IntInfo type) => new UnknownValueRange(type);

    public static readonly long MAX_DISCRETE_CARDINALITY = 1_000_000L;

    public class IntInfo
    {
        public string Name { get; init; }
        public int nbits { get; init; }
        public bool signed { get; init; }

        public long MinValue => signed ? -(1L << (nbits - 1)) : 0;
        public long MaxValue => signed ? (1L << (nbits - 1)) - 1 : (1L << nbits) - 1;

        public long Mask => (1L << nbits) - 1;
        public LongRange Range => new LongRange(MinValue, MaxValue);

        public override string ToString() => Name;
        public override bool Equals(object obj)
        {
            return (obj is IntInfo other) && nbits == other.nbits && signed == other.signed;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    static readonly Dictionary<string, IntInfo> INFOS = new()
    {
        ["bool"] = new IntInfo { Name = "bool", nbits = 1, signed = false },
        ["byte"] = new IntInfo { Name = "byte", nbits = 8, signed = false },
        ["sbyte"] = new IntInfo { Name = "sbyte", nbits = 8, signed = true },
        ["short"] = new IntInfo { Name = "short", nbits = 16, signed = true },
        ["ushort"] = new IntInfo { Name = "ushort", nbits = 16, signed = false },
        ["int"] = new IntInfo { Name = "int", nbits = 32, signed = true },
        ["uint"] = new IntInfo { Name = "uint", nbits = 32, signed = false },
        ["long"] = new IntInfo { Name = "long", nbits = 64, signed = true },
        ["ulong"] = new IntInfo { Name = "ulong", nbits = 64, signed = false },
        ["nint"] = new IntInfo { Name = "nint", nbits = 32, signed = true }, // TODO: 32/64 bit cmdline switch
        ["nuint"] = new IntInfo { Name = "nuint", nbits = 32, signed = false }, // TODO: 32/64 bit cmdline switch
    };

    public long Mask(long value)
    {
        return value & type.Mask;
    }

    public long Capacity()
    {
        return 1L << type.nbits;
    }

    public static bool IsTypeSupported(string typeName)
    {
        return INFOS.ContainsKey(ShortType(typeName));
    }

    public static IntInfo GetType(string typeName)
    {
        if (!INFOS.TryGetValue(ShortType(typeName), out IntInfo? t))
            throw new NotImplementedException($"UnknownTypedValue: {typeName} not implemented.");
        return t;
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
    public abstract bool IntersectsWith(UnknownTypedValue right);
    public abstract long Min();
    public abstract long Max();

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

    public override UnknownValueBase Mul(object right)
    {
        if (Cardinality() > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(type);

        if (TryConvertToLong(right, out long l))
            return new UnknownValueList(type, Values().Select(v => Mask(v * l)).Distinct().OrderBy(x => x).ToList());

        if (right is not UnknownTypedValue ru)
            return new UnknownValueRange(type);

        HashSet<long> values = new();
        foreach (long v in Values())
        {
            foreach (long r in ru.Values())
            {
                long maskedValue = Mask(v * r);
                if (values.Count >= MAX_DISCRETE_CARDINALITY)
                    return UnknownValue.Create(type);

                values.Add(maskedValue);
            }
        }

        return new UnknownValueList(type, values.OrderBy(x => x).ToList());
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
            return UnknownValue.Create(type);

        if (Cardinality() <= MAX_DISCRETE_CARDINALITY)
            return new UnknownValueList(type, Values().Select(v => v & mask).Distinct().OrderBy(x => x).ToList());

        return UnknownValueBits.CreateFromAnd(type, mask);
    }

    public override UnknownValueBase BitwiseOr(object right)
    {
        if (!TryConvertToLong(right, out long mask))
            return UnknownValue.Create(type);

        if (Cardinality() <= MAX_DISCRETE_CARDINALITY)
            return new UnknownValueList(type, Values().Select(v => v | mask).Distinct().OrderBy(x => x).ToList());

        return UnknownValueBits.CreateFromOr(type, mask);
    }
}
