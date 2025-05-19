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

    public override UnknownValue Cast(string toType)
    {
        throw new NotImplementedException($"{ToString()}.Cast({toType}): not implemented.");
    }

    public override string ToString()
    {
        return $"UnknownValue<{Type}>[{values.Count}]";
    }

    public override ulong Cardinality()
    {
        return (ulong)values.Count;
    }

    public override IEnumerable<long> Values()
    {
        return values;
    }

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
