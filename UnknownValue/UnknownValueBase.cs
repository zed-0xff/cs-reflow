using Microsoft.CodeAnalysis.CSharp;

public abstract class UnknownValueBase
{
    public abstract override string ToString();
    public abstract object Cast(string toType);
    public abstract long Cardinality();
    public abstract IEnumerable<long> Values();

    public abstract UnknownValueBase Add(object right);
    public abstract UnknownValueBase Div(object right);
    public abstract UnknownValueBase Mod(object right);
    public abstract UnknownValueBase Mul(object right);
    public abstract UnknownValueBase Sub(object right);
    public abstract UnknownValueBase Xor(object right);

    public abstract UnknownValueBase BitwiseAnd(object right);
    public abstract UnknownValueBase BitwiseOr(object right);
    public abstract UnknownValueBase BitwiseNot();
    public abstract UnknownValueBase ShiftLeft(object right);
    public abstract UnknownValueBase SignedShiftRight(object right);
    public abstract UnknownValueBase UnsignedShiftRight(object right);
    public abstract UnknownValueBase Negate();

    public abstract object Eq(object right);
    public abstract object Gt(object right);
    public abstract object Lt(object right);

    public static object LogicalOr(object left, object right)
    {
        if (left is bool lb && lb)
            return true;

        if (right is bool rb && rb)
            return true;

        return UnknownValue.Create("bool");
    }

    public virtual object Lte(object right) => LogicalOr(Lt(right), Eq(right));
    public virtual object Gte(object right) => LogicalOr(Gt(right), Eq(right));

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

    public object Op(SyntaxKind op) // unary op
    {
        return op switch
        {
            SyntaxKind.BitwiseNotExpression => BitwiseNot(),
            SyntaxKind.UnaryPlusExpression => this,
            SyntaxKind.UnaryMinusExpression => Negate(),
            SyntaxKind.LogicalNotExpression => Eq(0),
            _ => throw new NotImplementedException($"{ToString()}.Op({op}): not implemented"),
        };
    }

    public object Op(string op, object rValue) // binary op
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
            "<=" => Lte(rValue),
            "==" => Eq(rValue),
            ">" => Gt(rValue),
            ">=" => Gte(rValue),

            "&" => BitwiseAnd(rValue),
            "|" => BitwiseOr(rValue),

            "<<" => ShiftLeft(rValue),
            ">>" => SignedShiftRight(rValue), // TODO
            ">>>" => UnsignedShiftRight(rValue),

            _ => throw new NotImplementedException($"{ToString()}.Op({op}): not implemented"),
        };
    }

    public object InverseOp(string op, object lValue)
    {
        return op switch
        {
            "+" => Op(op, lValue),
            "-" => Negate().Add(lValue), // N - unk = (-unk) + N
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
