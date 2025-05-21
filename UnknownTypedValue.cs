public abstract class UnknownTypedValue : UnknownValueBase
{
    public string Type { get; }

    public static UnknownTypedValue Create(string type)
    {
        return new UnknownValueRange(type);
    }

    public static readonly ulong MAX_DISCRETE_CARDINALITY = 1000000;

    static readonly Dictionary<string, LongRange> RANGES = new()
    {
        ["bool"] = new LongRange(0, 1),
        ["byte"] = new LongRange(byte.MinValue, byte.MaxValue),
        ["sbyte"] = new LongRange(sbyte.MinValue, sbyte.MaxValue),
        ["int"] = new LongRange(int.MinValue, int.MaxValue),
        ["uint"] = new LongRange(uint.MinValue, uint.MaxValue),
        ["nint"] = new LongRange(nint.MinValue, nint.MaxValue), // TODO: 32/64 bit cmdline switch
    };

    public static bool IsTypeSupported(string type)
    {
        return RANGES.ContainsKey(ShortType(type));
    }

    public static LongRange Type2Range(string type)
    {
        if (RANGES.TryGetValue(ShortType(type), out LongRange range))
            return range;
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

    public UnknownTypedValue(string type) : base()
    {
        Type = ShortType(type);
    }

    public abstract bool Contains(long value);

    public override object Eq(object right)
    {
        if (TryConvertToLong(right, out long l))
            return Contains(l);

        return right switch
        {
            UnknownTypedValue r => r.Contains(l),
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
}
