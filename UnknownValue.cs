using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class UnknownValue
{
    public string? Type { get; } = null;

    public static readonly ulong MAX_DISCRETE_CARDINALITY = 1000000;

    public UnknownValue()
    {
    }

    public UnknownValue(string? type)
    {
        Type = type;
    }

    public static UnknownValue Create() => new UnknownValue();
    public static UnknownValue Create(string type) => type == null ? new UnknownValue() : new UnknownValueRange(type);
    public static UnknownValue Create(Type? type) => Create(type?.ToString());
    public static UnknownValue Create(TypeSyntax type) => Create(type.ToString());

    public static UnknownValue operator +(UnknownValue left, object right)
    {
        return UnknownValue.Create(left.Type);
    }

    public virtual UnknownValue Cast(string toType)
    {
        throw new NotImplementedException($"{ToString()}.Cast({toType}): not implemented.");
    }

    public virtual string ToString()
    {
        return $"UnknownValue<{Type}>";
    }

    public virtual ulong Cardinality()
    {
        throw new NotImplementedException($"{ToString()}.Cardinality(): not implemented.");
    }

    public virtual IEnumerable<long> Values()
    {
        throw new NotImplementedException($"{ToString()}.Values(): not implemented.");
    }

    public static UnknownValue operator %(UnknownValue left, object right)
    {
        throw new NotImplementedException($"{left.ToString()}.%: not implemented.");
    }

    public virtual UnknownValue Mod(object right)
    {
        throw new NotImplementedException($"{ToString()}.Mod(): not implemented.");
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
                return UnknownValue.Create(Type);

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
}
