public abstract class UnknownTypedValue : UnknownValueBase
{
    protected string Type;

    public static readonly ulong MAX_DISCRETE_CARDINALITY = 1000000;

    public UnknownTypedValue(string type) : base()
    {
        Type = type;
    }

}
