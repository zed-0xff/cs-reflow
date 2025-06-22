public abstract class UnknownValueBitsBase : UnknownTypedValue
{
    public UnknownValueBitsBase(TypeDB.IntInfo type) : base(type) { }

    public abstract UnknownValueBase TypedMul(object right);
    public abstract UnknownValueBase TypedBitwiseAnd(object right);
}
