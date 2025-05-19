public abstract class UnknownValueBase
{
    public abstract string ToString();
    public abstract UnknownValueBase Cast(string toType);
    public abstract ulong Cardinality();
    public abstract IEnumerable<long> Values();

    public abstract UnknownValueBase Add(object right);
    public abstract UnknownValueBase Div(object right);
    public abstract UnknownValueBase Mod(object right);
    public abstract UnknownValueBase Mul(object right);
    public abstract UnknownValueBase Sub(object right);
    public abstract UnknownValueBase Xor(object right);

    public object Op(string op, object rValue)
    {
        switch (op)
        {
            case "+":
            case "-":
            case "*":
            case "/":
            case "%":
            case "^":
            case "&":
            case "|":
            case "<<":
            case ">>":
            case ">>>":
                return new UnknownValue();

            case "!=":
            case "<":
            case "<=":
            case "==":
            case ">":
            case ">=":
                return UnknownValue.Create(typeof(bool));

            default:
                throw new NotImplementedException($"{ToString()}: Operator {op} is not implemented");
        }
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
