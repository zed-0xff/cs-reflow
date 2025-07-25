using Microsoft.CodeAnalysis.CSharp;

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

        return UnknownTypedValue.Create(TypeDB.Find(type));
    }

    public static UnknownValueBase Create(Type? type) => (type is null) ? Create() : Create(type.ToString());
    public static UnknownValueBase Create(Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax type) => Create(type.ToString());
    public static UnknownValueBase Create(TypeDB.IntType? type) => (type is null) ? Create() : UnknownTypedValue.Create(type);

    public override UnknownValueBase WithTag(string key, object? value) => HasTag(key, value) ? this : new() { _tags = add_tag(key, value) };
    public override UnknownValueBase WithVarID(int id) => Equals(_var_id, id) ? this : new() { _var_id = id };

    public override UnknownValueBase Cast(TypeDB.IntType toType) => Create(toType);

    public override string ToString() => "UnknownValue" + TagStr();

    public override bool Contains(long value) => throw new NotImplementedException($"{ToString()}.Contains(): not implemented.");
    public override CardInfo Cardinality() => throw new NotImplementedException($"{ToString()}.Cardinality(): not implemented.");
    public override IEnumerable<long> Values() => throw new NotImplementedException($"{ToString()}.Values(): not implemented.");

    public override object UnaryOp(SyntaxKind _) => new UnknownValue();
    public override object BinaryOp(SyntaxKind _, object rValue) => new UnknownValue();
    public override object InverseBinaryOp(SyntaxKind _, object lValue) => new UnknownValue();

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

    public override long Min() => throw new NotImplementedException($"{ToString()}.Min(): not implemented.");
    public override long Max() => throw new NotImplementedException($"{ToString()}.Max(): not implemented.");

    public override object Eq(object right) => UnknownValue.Create("bool");
    public override object Gt(object right) => UnknownValue.Create("bool");
    public override object Lt(object right) => UnknownValue.Create("bool");

    public override bool Equals(object? obj) => obj is UnknownValue;

    public override int GetHashCode() => typeof(UnknownValue).GetHashCode();

    public override UnknownValueBase Merge(object other) => this;
}
