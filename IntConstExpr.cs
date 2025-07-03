using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Globalization;
using System.Numerics;

// "A constant_expression (§12.23) of type int can be converted to type sbyte, byte, short, ushort, uint, or ulong, provided the value of the constant_expression is within the range of the destination type."
//
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#10211-implicit-constant-expression-conversions

public static class NumberUtils
{
    public static T MaxMagnitudeNumber<T>(T a, T b) where T : INumber<T>
    {
        return T.MaxMagnitudeNumber(a, b);
    }

    public static T MinMagnitudeNumber<T>(T a, T b) where T : INumber<T>
    {
        return T.MinMagnitudeNumber(a, b);
    }
}

public class IntConstExpr :
    TypeDB.IIntType,
    INumber<IntConstExpr>,
    IBitwiseOperators<IntConstExpr, IntConstExpr, IntConstExpr>,
    IShiftOperators<IntConstExpr, int, IntConstExpr>,
    IShiftOperators<IntConstExpr, IntConstExpr, IntConstExpr>
{
    public int Value { get; }
    public TypeDB.IntType IntType { get; }
    public Microsoft.CodeAnalysis.SpecialType IntTypeID => IntType.id;
    public bool CanBeNegative => (Value < 0);

    public IntConstExpr(int value, TypeDB.IntType? type = null)
    {
        type ??= TypeDB.Int; // Default to int if no type is provided

        if (!type.CanFit(value))
            throw new InvalidCastException($"Cannot create IntConstExpr with value {value} and type {type}. Value is out of range for type {type}.");

        Value = value;
        IntType = type;
    }

    public static int Radix => 10;
    public static IntConstExpr AdditiveIdentity => new IntConstExpr(0);
    public static IntConstExpr MultiplicativeIdentity => new IntConstExpr(1);

    // Arithmetic operators
    public static IntConstExpr operator +(IntConstExpr a, IntConstExpr b) => new(a.Value + b.Value);
    public static IntConstExpr operator -(IntConstExpr a, IntConstExpr b) => new(a.Value - b.Value);
    public static IntConstExpr operator *(IntConstExpr a, IntConstExpr b) => new(a.Value * b.Value);
    public static IntConstExpr operator /(IntConstExpr a, IntConstExpr b) => new(a.Value / b.Value);
    public static IntConstExpr operator +(IntConstExpr a, int b) => new(a.Value + b);
    public static IntConstExpr operator -(IntConstExpr a, int b) => new(a.Value - b);
    public static IntConstExpr operator *(IntConstExpr a, int b) => new(a.Value * b);
    public static IntConstExpr operator /(IntConstExpr a, int b) => new(a.Value / b);

    // Bitwise operators
    public static IntConstExpr operator &(IntConstExpr a, IntConstExpr b) => new(a.Value & b.Value);
    public static IntConstExpr operator |(IntConstExpr a, IntConstExpr b) => new(a.Value | b.Value);
    public static IntConstExpr operator ^(IntConstExpr a, IntConstExpr b) => new(a.Value ^ b.Value);
    public static IntConstExpr operator &(IntConstExpr a, int b) => new(a.Value & b);
    public static IntConstExpr operator |(IntConstExpr a, int b) => new(a.Value | b);
    public static IntConstExpr operator ^(IntConstExpr a, int b) => new(a.Value ^ b);

    public static IntConstExpr operator ~(IntConstExpr a) => new(~a.Value);

    public static IntConstExpr operator ++(IntConstExpr a) => throw new NotSupportedException();
    public static IntConstExpr operator --(IntConstExpr a) => throw new NotSupportedException();
    public static IntConstExpr operator -(IntConstExpr a) => new(-a.Value);
    public static IntConstExpr operator +(IntConstExpr a) => new(a.Value);

    // shift operators
    public static IntConstExpr operator <<(IntConstExpr a, IntConstExpr shift) => new(a.Value << shift.Value);
    public static IntConstExpr operator >>(IntConstExpr a, IntConstExpr shift) => new(a.Value >> shift.Value);
    public static IntConstExpr operator >>>(IntConstExpr a, IntConstExpr shift) => new(a.Value >> shift.Value);
    public static IntConstExpr operator <<(IntConstExpr a, int shift) => new(a.Value << shift);
    public static IntConstExpr operator >>(IntConstExpr a, int shift) => new(a.Value >> shift);
    public static IntConstExpr operator >>>(IntConstExpr a, int shift) => new(a.Value >> shift);

    // Comparison
    public static bool operator ==(IntConstExpr? a, IntConstExpr? b) =>
        a is null ? b is null : b is not null && a.Value == b.Value;

    public static bool operator !=(IntConstExpr? a, IntConstExpr? b) =>
        !(a == b);

    public bool Equals(IntConstExpr? other) => other != null && Value == other.Value;
    public override bool Equals(object? obj) =>
        obj switch
        {
            IntConstExpr other => Equals(other),
            int otherInt => Value == otherInt,
            _ => false
        };

    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    // Required members of INumber
    public static IntConstExpr One => new(1);
    public static IntConstExpr Zero => new(0);

    public static IntConstExpr Abs(IntConstExpr value) => new(int.Abs(value.Value));

    public static bool IsCanonical(IntConstExpr value) => true;
    public static bool IsComplexNumber(IntConstExpr value) => false;
    public static bool IsEvenInteger(IntConstExpr value) => int.IsEvenInteger(value.Value);
    public static bool IsFinite(IntConstExpr value) => true; // All IntConstExpr are finite
    public static bool IsImaginaryNumber(IntConstExpr value) => false;
    public static bool IsInfinity(IntConstExpr value) => false; // IntConstExpr cannot be infinity
    public static bool IsInteger(IntConstExpr value) => true; // All IntConstExpr are integers
    public static bool IsNaN(IntConstExpr value) => false; // IntConstExpr cannot be NaN
    public static bool IsNegative(IntConstExpr value) => int.IsNegative(value.Value);
    public static bool IsNegativeInfinity(IntConstExpr value) => false; // IntConstExpr cannot be negative infinity
    public static bool IsNormal(IntConstExpr value) => true; // All IntConstExpr are normal
    public static bool IsOddInteger(IntConstExpr value) => int.IsOddInteger(value.Value);
    public static bool IsPositive(IntConstExpr value) => int.IsPositive(value.Value);
    public static bool IsPositiveInfinity(IntConstExpr value) => false; // IntConstExpr cannot be positive infinity
    public static bool IsRealNumber(IntConstExpr value) => true; // All IntConstExpr are real numbers
    public static bool IsSubnormal(IntConstExpr value) => false; // IntConstExpr cannot be subnormal
    public static bool IsZero(IntConstExpr value) => value.Value == 0;

    public static IntConstExpr MaxMagnitude(IntConstExpr x, IntConstExpr y) => new(int.MaxMagnitude(x.Value, y.Value));
    public static IntConstExpr MinMagnitude(IntConstExpr x, IntConstExpr y) => new(int.MinMagnitude(x.Value, y.Value));

    public static IntConstExpr MaxMagnitudeNumber(IntConstExpr x, IntConstExpr y) => new(NumberUtils.MaxMagnitudeNumber(x.Value, y.Value));
    public static IntConstExpr MinMagnitudeNumber(IntConstExpr x, IntConstExpr y) => new(NumberUtils.MinMagnitudeNumber(x.Value, y.Value));

    public static bool TryConvertFromChecked<TFrom>(TFrom value, out IntConstExpr result)
        where TFrom : INumberBase<TFrom>
    {
        throw new NotImplementedException();
    }

    public static bool TryConvertFromSaturating<TFrom>(TFrom value, out IntConstExpr result)
        where TFrom : INumberBase<TFrom>
    {
        throw new NotImplementedException();
    }

    public static bool TryConvertFromTruncating<TFrom>(TFrom value, out IntConstExpr result)
        where TFrom : INumberBase<TFrom>
    {
        throw new NotImplementedException();
    }

    public static bool TryConvertToChecked<TTo>(IntConstExpr value, out TTo result)
        where TTo : INumberBase<TTo>
    {
        throw new NotImplementedException();
    }

    public static bool TryConvertToSaturating<TTo>(IntConstExpr value, out TTo result)
        where TTo : INumberBase<TTo>
    {
        throw new NotImplementedException();
    }

    public static bool TryConvertToTruncating<TTo>(IntConstExpr value, out TTo result)
        where TTo : INumberBase<TTo>
    {
        throw new NotImplementedException();
    }

    public static IntConstExpr Parse(string s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
    {
        if (int.TryParse(s, style, provider ?? CultureInfo.InvariantCulture, out int result))
        {
            return new IntConstExpr(result);
        }
        throw new FormatException($"Invalid format for IntConstExpr: '{s}'");
    }

    public static IntConstExpr Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
    {
        if (int.TryParse(s, style, provider ?? CultureInfo.InvariantCulture, out int result))
        {
            return new IntConstExpr(result);
        }
        throw new FormatException($"Invalid format for IntConstExpr: '{s.ToString()}'");
    }

    public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out IntConstExpr result)
    {
        if (int.TryParse(s, style, provider ?? CultureInfo.InvariantCulture, out int intValue))
        {
            result = new IntConstExpr(intValue);
            return true;
        }
        result = Zero;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out IntConstExpr result)
    {
        if (int.TryParse(s, style, provider ?? CultureInfo.InvariantCulture, out int intValue))
        {
            result = new IntConstExpr(intValue);
            return true;
        }
        result = Zero;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out IntConstExpr result)
    {
        if (int.TryParse(s, NumberStyles.Integer, provider ?? CultureInfo.InvariantCulture, out int intValue))
        {
            result = new IntConstExpr(intValue);
            return true;
        }
        result = Zero;
        return false;
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out IntConstExpr result)
    {
        if (s is null)
        {
            result = Zero;
            return false;
        }
        return TryParse(s.AsSpan(), provider, out result);
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider = null) =>
        Value.TryFormat(destination, out charsWritten, format, provider ?? CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? provider = null) => Value.ToString(format, provider ?? CultureInfo.InvariantCulture);

    public static IntConstExpr Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) => new(int.Parse(s, NumberStyles.Integer, provider));
    public static IntConstExpr Parse(string s, IFormatProvider? provider = null) => new(int.Parse(s, NumberStyles.Integer, provider));

    public int CompareTo(object? obj)
    {
        if (obj is IntConstExpr other)
        {
            return Value.CompareTo(other.Value);
        }
        throw new ArgumentException($"Object must be of type {nameof(IntConstExpr)}", nameof(obj));
    }

    public int CompareTo(IntConstExpr? other) => Value.CompareTo(other?.Value);

    public static bool operator <(IntConstExpr left, IntConstExpr right) => left.Value < right.Value;
    public static bool operator >(IntConstExpr left, IntConstExpr right) => left.Value > right.Value;
    public static bool operator <=(IntConstExpr left, IntConstExpr right) => left.Value <= right.Value;
    public static bool operator >=(IntConstExpr left, IntConstExpr right) => left.Value >= right.Value;
    public static IntConstExpr operator %(IntConstExpr left, IntConstExpr right) => new(left.Value % right.Value);

    public bool CanCast(TypeDB.IntType toType) => toType.CanFit(Value);

    public IntConstExpr FakeCast(TypeDB.IntType toType)
    {
        if (!CanCast(toType))
            throw new InvalidCastException($"Cannot cast IntConstExpr to {toType}. {Value.GetType()} {Value} is out of range for type {toType}.");

        return new IntConstExpr(Value, toType);
    }

    // "A constant_expression (§12.23) of type int can be converted to type sbyte, byte, short, ushort, uint, or ulong, provided the value of the constant_expression is within the range of the destination type."
    public object Cast(TypeDB.IntType toType)
    {
        if (!CanCast(toType))
            throw new InvalidCastException($"Cannot cast IntConstExpr to {toType}. {Value.GetType()} {Value} is out of range for type {toType}.");

        return toType.ConvertInt(Value);
    }

    public object Materialize(TypeDB.IntType? asType = null)
    {
        object result = ((asType ?? IntType) == TypeDB.Int32) ? Value : Cast(asType ?? IntType);
        Logger.debug(() => $"{this} as {asType ?? IntType} => {result} ({result.GetType()})", "IntConstExpr.Materialize");
        return result;
    }

    public object Cast(string toTypeName) => Cast(TypeDB.Find(toTypeName));
    public object TryCast(System.Type toType) => TryCast(toType.ToString());
    public object TryCast(string toTypeName)
    {
        var type = TypeDB.TryFind(toTypeName);
        return (type != null && CanCast(type)) ? Cast(type) : this;
    }
}

