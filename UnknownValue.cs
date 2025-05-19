using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class UnknownValue
{
    public string? Type { get; } = null;
    public LongRange? Range { get; private set; } = null;

    const ulong MAX_DISCRETE_CARDINALITY = 1000000;

    void init()
    {
        switch (Type)
        {
            case "int":
                Range = new LongRange(int.MinValue, int.MaxValue);
                break;
            case "uint":
                Range = new LongRange(uint.MinValue, uint.MaxValue);
                break;
        }
    }

    public UnknownValue()
    {
    }

    public UnknownValue(Type? type)
    {
        Type = type?.ToString();
        init();
    }

    public UnknownValue(TypeSyntax type)
    {
        Type = type.ToString();
        init();
    }

    public UnknownValue(string type)
    {
        Type = type;
        init();
    }

    public UnknownValue(string type, LongRange range)
    {
        Type = type;
        Range = range;
    }

    public ulong Cardinality()
    {
        if (Range is null)
            return 0;

        return (ulong)(Range.Max - Range.Min + 1); // because both are inclusive
    }

    public UnknownValue Cast(string toType)
    {
        if (Type == "uint" && toType == "int" && Range is not null && Range.Max <= 0x7FFFFFFF)
        {
            return new UnknownValue("int", new LongRange(0, Range.Max));
        }
        return new UnknownValue(toType);
    }

    private static bool TryConvertToLong(object obj, out long result)
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

    public static UnknownValue operator /(UnknownValue left, object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValue(left.Type, left.Range / l)
            : new UnknownValue(left.Type);
    }

    public static UnknownValue operator +(UnknownValue left, object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValue(left.Type, left.Range + l)
            : new UnknownValue(left.Type);
    }

    public static UnknownValue operator -(UnknownValue left, object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValue(left.Type, left.Range - l)
            : new UnknownValue(left.Type);
    }

    public static UnknownValue operator *(UnknownValue left, object right)
    {
        return TryConvertToLong(right, out long l)
            ? new UnknownValue(left.Type, left.Range * l)
            : new UnknownValue(left.Type);
    }

    public static UnknownValue operator %(UnknownValue left, object right)
    {
        if (!TryConvertToLong(right, out long l))
            return new UnknownValue(left.Type);

        if (l == 0)
            throw new DivideByZeroException();

        // TODO: case when left is not full range
        if (l > 0)
            return new UnknownValue(left.Type, new LongRange(0, l - 1));
        else
            return new UnknownValue(left.Type, new LongRange(l + 1, 0));

    }

    public static UnknownValue operator ^(UnknownValue left, object right)
    {
        if (!TryConvertToLong(right, out long l))
            return new UnknownValue(left.Type);

        if (left.Cardinality() > MAX_DISCRETE_CARDINALITY)
            return new UnknownValue(left.Type);

        // TODO: 

        return new UnknownValue(left.Type);
    }

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
                return new UnknownValue(Type);

            case "!=":
            case "<":
            case "<=":
            case "==":
            case ">":
            case ">=":
                return new UnknownValue(typeof(bool));

            default:
                throw new NotImplementedException($"{ToString()}: Operator {op} is not implemented");
        }
    }

    public object InverseOp(string op, object lValue)
    {
        return new UnknownValue();
    }

    public override string ToString()
    {
        return $"UnknownValue<{Type}>";
    }
}
