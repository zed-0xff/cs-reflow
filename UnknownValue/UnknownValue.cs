using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class UnknownValue : UnknownValueBase
{
    public UnknownValue()
    {
    }

    public static UnknownValueBase Create() => new UnknownValue();
    public static UnknownValueBase Create(string type)
    {
        if (string.IsNullOrEmpty(type) || !UnknownTypedValue.IsTypeSupported(type))
            return new UnknownValue();

        return UnknownTypedValue.Create(type);
    }

    public static UnknownValueBase Create(Type? type) => Create(type?.ToString());
    public static UnknownValueBase Create(TypeSyntax type) => Create(type.ToString());
    public static UnknownValueBase Create(UnknownTypedValue.IntInfo type) => UnknownTypedValue.Create(type);

    public override UnknownValueBase Cast(string toType)
    {
        return UnknownValue.Create(toType);
    }

    public override string ToString()
    {
        return "UnknownValue";
    }

    public override long Cardinality()
    {
        throw new NotImplementedException($"{ToString()}.Cardinality(): not implemented.");
    }

    public override IEnumerable<long> Values()
    {
        throw new NotImplementedException($"{ToString()}.Values(): not implemented.");
    }

    public override UnknownValue Add(object right) => new UnknownValue();
    public override UnknownValue Div(object right) => new UnknownValue();
    public override UnknownValue Mod(object right) => new UnknownValue();
    public override UnknownValue Mul(object right) => new UnknownValue();
    public override UnknownValue Sub(object right) => new UnknownValue();
    public override UnknownValue Xor(object right) => new UnknownValue();

    // narrows the scope, but the type is still unknown
    public override UnknownValue BitwiseAnd(object right) => new UnknownValue();
    public override UnknownValue BitwiseOr(object right) => new UnknownValue();
    public override UnknownValue BitwiseNot() => new UnknownValue();
    public override UnknownValue ShiftLeft(object right) => new UnknownValue();
    public override UnknownValue UnsignedShiftRight(object right) => new UnknownValue();
    public override UnknownValue Negate() => new UnknownValue();

    public override object Eq(object right) => UnknownValue.Create("bool");
    public override object Gt(object right) => UnknownValue.Create("bool");
    public override object Lt(object right) => UnknownValue.Create("bool");

    public override bool Equals(object? obj) => obj is UnknownValue;

    public override int GetHashCode() => typeof(UnknownValue).GetHashCode();
}
