public class UnknownValueList : UnknownTypedValue
{
    public List<long> values = new();

    public UnknownValueList(string type) : base(type)
    {
    }

    public UnknownValueList(string type, List<long> values) : base(type)
    {
        this.values = values;
    }

    public override UnknownValueList Cast(string toType)
    {
        toType = ShortType(toType);
        if (Type == "uint" && toType == "int")
        {
            return new("int", Values().Select(v => (long)unchecked((int)(uint)v)).ToList());
        }
        throw new NotImplementedException($"{ToString()}.Cast({toType}): not implemented.");
    }

    public override string ToString()
    {
        return $"UnknownValue<{Type}>[{values.Count}]";
    }

    public override ulong Cardinality() => (ulong)values.Count;
    public override IEnumerable<long> Values() => values;
    public override bool Contains(long value) => values.Contains(value);

    public override UnknownValueBase Add(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => v + l).ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase Sub(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => v - l).ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase Div(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => v / l).Distinct().ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase Mod(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => v % l).Distinct().OrderBy(x => x).ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase Mul(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => v * l).Distinct().ToList())
            : new UnknownValueList(Type);

    public override UnknownValueBase Xor(object right) =>
        TryConvertToLong(right, out long l)
            ? new UnknownValueList(Type, values.Select(v => v ^ l).ToList())
            : new UnknownValueList(Type);
}
