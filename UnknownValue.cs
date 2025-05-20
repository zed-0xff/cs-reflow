using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class UnknownValue : UnknownValueBase
{
    public UnknownValue()
    {
    }

    public static UnknownValueBase Create() => new UnknownValue();
    public static UnknownValueBase Create(string type) => type == null ? Create() : new UnknownValueRange(type);
    public static UnknownValueBase Create(Type? type) => Create(type?.ToString());
    public static UnknownValueBase Create(TypeSyntax type) => Create(type.ToString());

    public override UnknownValueBase Cast(string toType)
    {
        return UnknownValue.Create(toType);
    }

    public override string ToString()
    {
        return "UnknownValue";
    }

    public override ulong Cardinality()
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

    public override object Eq(object right) => UnknownValue.Create("bool");
    public override object Gt(object right) => UnknownValue.Create("bool");
    public override object Lt(object right) => UnknownValue.Create("bool");
}
