public class UnknownValueList : UnknownValue
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

    public static UnknownValue operator ^(UnknownValueList left, object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(left.Type);

        if (left.Cardinality() > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(left.Type);

        return new UnknownValueList(left.Type, left.Values().Select(v => v ^ l).OrderBy(x => x).ToList());
    }
}

