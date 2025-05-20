public abstract class UnknownTypedValue : UnknownValueBase
{
    protected string Type;

    public static readonly ulong MAX_DISCRETE_CARDINALITY = 1000000;

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
}
