public abstract class UnknownValueBitsBase : UnknownTypedValue
{
    public UnknownValueBitsBase(TypeDB.IntInfo type) : base(type) { }

    public abstract (long, long) MaskVal(bool minimal = false);

    protected long extend_sign(long v)
    {
        if (type.signed)
        {
            long sign_bit = (1L << (type.nbits - 1));
            if ((v & sign_bit) != 0)
            {
                v |= ~type.Mask;
            }
        }
        return v;
    }

    public abstract UnknownValueBase TypedBitwiseAnd(object right);
    public abstract UnknownValueBase TypedBitwiseOr(object right);

    public UnknownTypedValue TypedMul(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            int i = 0;
            while ((l & 1) == 0 && i < type.nbits)
            {
                l >>>= 1; i++;
            }
            var result = TypedShiftLeft(i);
            l >>>= 1; i++;
            for (; l != 0; i++, l >>>= 1)
            {
                if ((l & 1) != 0)
                {
                    result = result.TypedAdd(TypedShiftLeft(i));
                }
            }
            return result;
        }

        if (right is UnknownValueBitsBase otherBits)
        {
            bool useMinMask = true;
            var (mask1, val1) = MaskVal(useMinMask);
            var (mask2, val2) = otherBits.MaskVal(useMinMask);
            var newMask = mask1 & mask2;
            return new UnknownValueBits(type, val1 * val2, newMask);
        }

        return UnknownTypedValue.Create(type);
    }
}
