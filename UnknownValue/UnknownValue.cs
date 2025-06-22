using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class UnknownValue : UnknownValueBase
{
    public class Builder
    {
        private readonly object? _tag;

        public Builder(object? tag)
        {
            _tag = tag;
        }

        public UnknownValueBase Create()
        {
            return new UnknownValue { _tag = _tag };
        }
    }

    public UnknownValue()
    {
    }

    //public static Builder WithTag(object? tag) => new Builder(tag);

    public static UnknownValueBase Create() => new UnknownValue();
    public static UnknownValueBase Create(string type)
    {
        if (string.IsNullOrEmpty(type) || !UnknownTypedValue.IsTypeSupported(type))
            return new UnknownValue();

        return UnknownTypedValue.Create(TypeDB.Find(type));
    }

    public static UnknownValueBase Create(Type? type) => Create(type?.ToString());
    public static UnknownValueBase Create(TypeSyntax type) => Create(type.ToString());
    public static UnknownValueBase Create(TypeDB.IntInfo? type) => (type == null) ? Create() : UnknownTypedValue.Create(type);

    public override UnknownValueBase WithTag(object? tag) => Equals(_tag, tag) ? this : new() { _tag = tag };
    public override UnknownValueBase WithVarID(int id) => Equals(_var_id, id) ? this : new() { _var_id = id };

    public override UnknownValueBase Cast(TypeDB.IntInfo toType) => Create(toType);

    public override string ToString() => "UnknownValue" + TagStr();

    public override bool Contains(long value) => true;

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
    public override UnknownValue SignedShiftRight(object right) => new UnknownValue();
    public override UnknownValue UnsignedShiftRight(object right) => new UnknownValue();
    public override UnknownValue Negate() => new UnknownValue();

    public override object Eq(object right) => UnknownValue.Create("bool");
    public override object Gt(object right) => UnknownValue.Create("bool");
    public override object Lt(object right) => UnknownValue.Create("bool");

    public override bool Equals(object? obj) => obj is UnknownValue;

    public override int GetHashCode() => typeof(UnknownValue).GetHashCode();

    public override UnknownValueBase Merge(object other) => this;
}
