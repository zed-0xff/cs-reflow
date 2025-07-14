using Microsoft.CodeAnalysis.CSharp;
using System.Collections.ObjectModel;

public abstract class UnknownValueBase
{
    protected IReadOnlyDictionary<string, object>? _tags { get; init; } = null;
    public int? _var_id { get; init; } = null; // public for UnknownValueBitTracker

    protected IReadOnlyDictionary<string, object>? add_tag(string key, object? value)
    {
        var dict = _tags is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(_tags);

        if (value is null)
        {
            if (dict.ContainsKey(key))
                dict.Remove(key);
            return dict.Count == 0 ? null : new ReadOnlyDictionary<string, object>(dict);
        }
        else
        {
            dict[key] = value;
            return new ReadOnlyDictionary<string, object>(dict);
        }
    }

    protected string TagStr()
    {
        if (_tags is null || _tags.Count == 0)
            return string.Empty;

        var tags = "{" + string.Join(", ", _tags.Select(kv => $"{kv.Key}={kv.Value}")) + "}";
        return tags;
    }

    public bool TryGetTag(string key, out object? value)
    {
        if (_tags is null || !_tags.TryGetValue(key, out value))
        {
            value = null;
            return false;
        }
        return true;
    }

    protected bool HasTag(string key) => _tags is not null && _tags.ContainsKey(key);
    protected bool HasTag(string key, object? value)
    {
        if (value is null)
            return _tags is null || !_tags.ContainsKey(key);

        if (_tags is null)
            return false;

        if (!_tags.TryGetValue(key, out var tagValue))
            return false;

        return value is null ? tagValue is null : value.Equals(tagValue);
    }

    public bool IsPointer() => HasTag("pointee");

    protected string VarIDStr() => _var_id.HasValue ? $"`{_var_id.Value}" : string.Empty;

    public abstract UnknownValueBase WithTag(string key, object? value);
    public UnknownValueBase WithoutTag(string key) => WithTag(key, null);
    public abstract UnknownValueBase WithVarID(int id);

    public abstract override string ToString();
    public abstract object Cast(TypeDB.IntType toType);
    public abstract CardInfo Cardinality();
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

    public abstract long Min();
    public abstract long Max();

    static object LogicalNot(object value) =>
        value switch
        {
            bool b => !b,
            UnknownValueBase u =>
                u.Cast(TypeDB.Bool) switch
                {
                    bool b => !b,
                    UnknownValueBase other => other.BitwiseNot(),
                    _ => throw new NotImplementedException($"{value?.GetType()} is not supported for LogicalNot")
                },
            _ => throw new NotImplementedException($"{value?.GetType()} is not supported for LogicalNot")
        };

    public virtual object Lte(object right) => LogicalNot(Gt(right));
    public virtual object Gte(object right) => LogicalNot(Lt(right));
    public virtual object Ne(object right) => LogicalNot(Eq(right));

    public abstract object UnaryOp(SyntaxKind op);
    public abstract object BinaryOp(SyntaxKind kind, object rValue);
    public abstract object InverseBinaryOp(SyntaxKind kind, object lValue);

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
