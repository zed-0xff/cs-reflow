public abstract class UnknownValueBase
{
    public abstract override string ToString();
    public abstract UnknownValueBase Cast(string toType);
    public abstract ulong Cardinality();
    public abstract IEnumerable<long> Values();

    public abstract UnknownValueBase Add(object right);
    public abstract UnknownValueBase Div(object right);
    public abstract UnknownValueBase Mod(object right);
    public abstract UnknownValueBase Mul(object right);
    public abstract UnknownValueBase Sub(object right);
    public abstract UnknownValueBase Xor(object right);

    public abstract object Eq(object right);
    public abstract object Gt(object right);
    public abstract object Lt(object right);

    public virtual object Ne(object right)
    {
        return Eq(right) switch
        {
            UnknownValueBase other => UnknownValue.Create("bool"),
            bool b => !b,
            _ => throw new NotImplementedException($"{ToString()}.Ne(): unexpected type {right?.GetType()}")
        };
    }

    // syntax sugar
    public virtual long Min() => Values().Min();
    public virtual long Max() => Values().Max();

    public object Op(string op, object rValue)
    {
        return op switch
        {
            "+" => Add(rValue),
            "-" => Sub(rValue),
            "*" => Mul(rValue),
            "/" => Div(rValue),
            "%" => Mod(rValue),
            "^" => Xor(rValue),

            "!=" => Ne(rValue),
            "<" => Lt(rValue),
            //            "<=" => Eq(rValue),
            "==" => Eq(rValue),
            ">" => Gt(rValue),
            //            ">=" => Eq(rValue),
            //
            //            "&" => Xor(rValue),
            //            "|" => Xor(rValue),
            //
            //            "<<" => Xor(rValue),
            //            ">>" => Xor(rValue),
            //            ">>>" => Xor(rValue),

            _ => throw new NotImplementedException($"{ToString()}.Op({op}): not implemented"),
        };
    }

    public object InverseOp(string op, object lValue)
    {
        return op switch
        {
            "+" => Op(op, lValue),
            "*" => Op(op, lValue),
            "^" => Op(op, lValue),
            "&" => Op(op, lValue),
            "|" => Op(op, lValue),
            "!=" => Op(op, lValue),
            "==" => Op(op, lValue),

            "<" => Op(">=", lValue),
            "<=" => Op(">", lValue),
            ">" => Op("<=", lValue),
            ">=" => Op("<", lValue),

            _ => throw new NotImplementedException($"{ToString()}.InverseOp(): '{op}' is not implemented"),
        };
    }

    protected static bool TryConvertToLong(object obj, out long result)
    {
        switch (obj)
        {
            case int i:
                result = i;
                return true;
            case uint u:
                result = u;
                return true;
            case long l:
                result = l;
                return true;
            case ulong ul when ul <= long.MaxValue:
                result = (long)ul;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
