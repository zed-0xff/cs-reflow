using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public abstract class UnknownValueBase
{
    protected object? _tag { get; init; } = null;
    protected int? _var_id { get; init; } = null;

    protected string TagStr()
    {
        return _tag switch
        {
            null => string.Empty,
            string s => $"`{s}",
            SyntaxAnnotation sa => $"`{sa.Data}",
            _ => $"`{_tag}"
        };
    }

    public abstract UnknownValueBase WithTag(object? tag);
    public abstract UnknownValueBase WithVarID(int id);

    public abstract override string ToString();
    public abstract object Cast(TypeDB.IntInfo toType);
    public abstract ulong Cardinality();
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

    public abstract bool Contains(long value);

    public static object LogicalOr(object left, object right)
    {
        if (left is bool lb && lb)
            return true;

        if (right is bool rb && rb)
            return true;

        return UnknownValue.Create(TypeDB.Bool);
    }

    public virtual object Lte(object right) => LogicalOr(Lt(right), Eq(right));
    public virtual object Gte(object right) => LogicalOr(Gt(right), Eq(right));

    public virtual object Ne(object right)
    {
        return Eq(right) switch
        {
            UnknownValueBase other => other.Cast(TypeDB.Bool),
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
            SyntaxKind.PostIncrementExpression => Add(1),
            SyntaxKind.PostDecrementExpression => Sub(1),
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

    public static bool TryConvertToLong(object? obj, out long result)
    {
        switch (obj)
        {
            case byte b:
                result = b;
                return true;
            case sbyte sb:
                result = sb;
                return true;
            case short s:
                result = s;
                return true;
            case ushort us:
                result = us;
                return true;
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
            case IntConstExpr ice:
                result = ice.Value;
                return true;
            default:
                result = default;
                return false;
        }
    }

    public static int TryGetSizeInBits(object? obj) => obj switch
    {
        byte => 8,
        sbyte => 8,
        short => 16,
        ushort => 16,
        int => 32,
        uint => 32,
        long => 64,
        ulong => 64,
        IntConstExpr ice => ice.IntType.nbits,
        _ => 0
    };

    public virtual UnknownValueBase Merge(object other)
    {
        return other switch
        {
            UnknownValue u => u,
            _ => throw new NotImplementedException($"{ToString()}.Merge({other}): not implemented")
        };
    }
}
