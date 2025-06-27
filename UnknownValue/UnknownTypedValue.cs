public abstract class UnknownTypedValue : UnknownValueBase
{
    public static readonly ulong MAX_DISCRETE_CARDINALITY = 1_000_000UL;
    public static readonly Type DEFAULT_TYPE = typeof(UnknownValueRange);

    public TypeDB.IntInfo type { get; }
    public TypeDB.IntInfo Type => type;

    public UnknownTypedValue(TypeDB.IntInfo type) : base()
    {
        this.type = type;
    }

    public abstract UnknownTypedValue TypedAdd(object right);
    public abstract UnknownValueBase TypedDiv(object right);
    public abstract UnknownValueBase TypedMod(object right);
    public abstract UnknownValueBase TypedSub(object right);
    public abstract UnknownValueBase TypedXor(object right);

    public abstract UnknownTypedValue TypedShiftLeft(object right);
    public abstract UnknownValueBase TypedSignedShiftRight(object right);
    public abstract UnknownValueBase TypedUnsignedShiftRight(object right);

    public static UnknownValueRange Create(TypeDB.IntInfo type) => new UnknownValueRange(type); // DEFAULT_TYPE

    static readonly Dictionary<TypeDB.IntInfo, UnknownTypedValue> _zeroes = new();
    static readonly Dictionary<TypeDB.IntInfo, UnknownTypedValue> _ones = new();

    public static UnknownTypedValue Zero(TypeDB.IntInfo type) =>
        _zeroes.TryGetValue(type, out var zero) ? zero :
        (_zeroes[type] = new UnknownValueRange(type, 0, 0));

    public static UnknownTypedValue One(TypeDB.IntInfo type) =>
        _ones.TryGetValue(type, out var one) ? one :
        (_ones[type] = new UnknownValueRange(type, 1, 1));

    public bool IsZero() => Equals(Zero(type));
    public bool IsOne() => Equals(One(type));

    public bool IsOverflow(long value) => !type.CanFit(value);
    public bool IsPositive(long value) => (value & type.SignMask) == 0;
    public bool IsNegative(long value) => (value & type.SignMask) != 0;
    public long SignExtend(long value) => IsNegative(value) ? (value | ~type.Mask) : value;
    public long MaskNoSign(long value) => value & type.Mask;
    public long MaskWithSign(long value) => SignExtend(MaskNoSign(value));
    public long Capacity() => (1L << type.nbits);
    public static bool IsTypeSupported(string typeName) => TypeDB.TryFind(typeName) is not null;

    public abstract bool IsFullRange();
    public abstract override bool Contains(long value);
    public abstract bool Typed_IntersectsWith(UnknownTypedValue right);
    public abstract override long Min();
    public abstract override long Max();
    public abstract BitSpan BitSpan();

    public UnknownValueBitsBase ToBits() => ToBits(BitSpan());
    public UnknownValueBitsBase ToBits(BitSpan bitspan) =>
        (this is UnknownValueBitsBase bits) ? bits :
        (_var_id == null) ? new UnknownValueBits(type, bitspan) : new UnknownValueBitTracker(type, _var_id.Value, bitspan);

    public bool CanConvertTo(UnknownTypedValue other)
    {
        if (type != other.type)
            return false;

        if (other is UnknownValueBitTracker)
            return CanConvertTo<UnknownValueBitTracker>();

        if (other is UnknownValueBits)
            return CanConvertTo<UnknownValueBits>();

        return false;
    }

    public virtual UnknownTypedValue ConvertTo<T>()
    {
        if (typeof(T) == typeof(UnknownValueBitTracker))
            return new UnknownValueBitTracker(type, _var_id.Value);

        throw new NotSupportedException($"Cannot convert {GetType()} to {typeof(T)}");
    }

    public virtual bool CanConvertTo<T>()
    {
        if (typeof(T) == typeof(UnknownValueBitTracker))
        {
            return this is not UnknownValueBitTracker
                && _var_id is not null
                && IsFullRange();
        }

        return false;
    }

    public UnknownTypedValue MaybeConvertTo<T>() where T : UnknownValueBitTracker
    {
        if (CanConvertTo<T>())
            return ConvertTo<T>();

        return this;
    }

    public override object Eq(object other)
    {
        if (TryConvertToLong(other, out long l))
        {
            if (Contains(l))
            {
                if (Cardinality() == 1)
                    return true;
                else
                    return UnknownValue.Create(TypeDB.Bool);
            }
            else
            {
                return false;
            }
        }

        if (other is UnknownTypedValue r)
        {
            if (Cardinality() == 1 && r.Cardinality() == 1 && Values().First() == r.Values().First())
                return true;

            if (!IntersectsWith(r))
                return false;
        }

        return UnknownValue.Create(TypeDB.Bool);
    }

    public bool IntersectsWith(UnknownTypedValue right)
    {
        var left = this;
        var cardL = left.Cardinality();
        if (cardL == 0)
            return false;

        var cardR = right.Cardinality();
        if (cardR == 0)
            return false;

        if (left.type == right.type)
        {
            if (left.IsFullRange() || right.IsFullRange())
                return true;

            if (cardL == 1)
                return right.Contains(left.Values().First());

            if (cardR == 1)
                return left.Contains(right.Values().First());
        }

        return left.Typed_IntersectsWith(right);
    }

    public override object Gt(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            if (Min() > l)
                return true;

            if (Max() < l)
                return false;
        }

        return UnknownValue.Create(TypeDB.Bool);
    }

    public override object Lt(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            if (Max() < l)
                return true;

            if (Min() >= l)
                return false;
        }

        return UnknownValue.Create(TypeDB.Bool);
    }

    // sealed to prevent overriding in derived classes
    public sealed override UnknownValueBase Add(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (TryConvertToLong(right, out long l) && (l == 0))
            return this;

        if (right is UnknownTypedValue otherTyped && otherTyped.IsZero())
            return this;

        UnknownTypedValue result = (right == this) ? TypedShiftLeft(1) : TypedAdd(right);
        return result.Normalize();
    }

    public sealed override UnknownValueBase ShiftLeft(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return this;
            if (l < 0)
                throw new ArgumentException("Shift count cannot be negative.");
            if (l >= type.nbits)
            {
                if (l > type.nbits)
                    Logger.warn_once($"Shift count {l} exceeds type bit width {type.nbits}.");

                return Zero(type);
            }
        }

        if (right is UnknownTypedValue otherTyped && otherTyped.IsZero())
            return this;

        return TypedShiftLeft(right);
    }

    public sealed override UnknownValueBase SignedShiftRight(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return this;
            if (l < 0)
                throw new ArgumentException("Shift count cannot be negative.");
            // if (l >= type.nbits && type.unsigned) // TODO: check // TODO: return MinusOne for signed?
            //     return Zero(type);
        }

        if (right is UnknownTypedValue otherTyped && otherTyped.IsZero())
            return this;

        return TypedSignedShiftRight(right);
    }

    public sealed override UnknownValueBase UnsignedShiftRight(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return this;
            if (l < 0)
                throw new ArgumentException("Shift count cannot be negative.");
            if (l >= type.nbits)
                return Zero(type);
        }

        if (right is UnknownTypedValue otherTyped && otherTyped.IsZero())
            return this;

        return TypedUnsignedShiftRight(right);
    }

    public sealed override UnknownValueBase Mod(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (TryConvertToLong(right, out long l))
        {
            if (l < 0)
                l = -l; // in C# always x%y == x % (-y)

            if (l == 0)
                return UnknownValue.Create(); // division by zero
            if (l == 1)
                return Zero(type);

            int pow = MaxPow2Divider(l);
            if (Math.Pow(2, pow) == l) // mod by a power of 2
            {
                if (!type.signed)
                {
                    return BitwiseAnd(l - 1); // XXX also valid for signed types when all values >= 0
                }
            }
        }

        if (right is UnknownTypedValue otherTyped)
        {
            if (otherTyped.IsZero())
                return UnknownValue.Create(); // division by zero
            if (otherTyped.IsOne() || otherTyped == this)
                return Zero(type);
        }

        return TypedMod(right);
    }

    public sealed override UnknownValueBase Div(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return UnknownValue.Create(); // division by zero
            if (l == 1)
                return this;
            if (l == -1)
                return Negate();
        }

        if (right is UnknownTypedValue otherTyped)
        {
            if (otherTyped.IsZero())
                return UnknownValue.Create(); // division by zero
            if (otherTyped.IsOne())
                return this;
            if (otherTyped == this)
                return One(type);
        }

        return TypedDiv(right);
    }

    public static int MaxPow2Divider(long x)
    {
        int result = 0;
        while (x != 0 && (x & 1) == 0)
        {
            x >>= 1;
            result++;
        }
        return result;
    }

    public sealed override UnknownValueBase Mul(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (right is UnknownTypedValue otherTyped)
        {
            if (otherTyped.IsZero())
                return Zero(type);
            if (otherTyped.IsOne())
                return this;
            if (otherTyped is UnknownValueBitsBase bitsRight && this is not UnknownValueBitsBase)
                return bitsRight.TypedMul(this);
        }

        int pow = 0;
        bool isNumeric = TryConvertToLong(right, out long l);

        if (isNumeric)
        {
            if (l == 0) return Zero(type);
            if (l == 1) return this;
            if (l == -1) return Negate();

            pow = MaxPow2Divider(l);
            if (Math.Pow(2, pow) == l) // multiplication by a power of 2
                return ShiftLeft(pow);
        }

        if (this is UnknownValueBitsBase bits)
            return bits.TypedMul(right);

        var bitsResult = ToBits().TypedMul(right);
        UnknownTypedValue result = bitsResult;

        if (Cardinality() < MAX_DISCRETE_CARDINALITY)
        {
            UnknownTypedValue setResult = UnknownTypedValue.Create(type);
            if (isNumeric)
            {
                setResult = new UnknownValueSet(type, Values().Select(v => MaskWithSign(v * l)));
            }
            else
            {
                if (right is not UnknownTypedValue ru)
                    return bitsResult;

                // calculate product of two unknown sets
                HashSet<long> values = new();
                foreach (long v in Values())
                {
                    foreach (long r in ru.Values())
                    {
                        long maskedValue = MaskWithSign(v * r);
                        if ((ulong)values.Count >= MAX_DISCRETE_CARDINALITY)
                            return bitsResult;

                        values.Add(maskedValue);
                    }
                }
                setResult = new UnknownValueSet(type, values);
            }

            if (setResult.Cardinality() < bitsResult.Cardinality()) // check if bitsResult is a BitTracker?
                result = setResult;
        }

        return result.Normalize();
    }

    public UnknownTypedValue Normalize()
    {
        if (this is UnknownValueBitTracker bt && bt.HasPrivateBits()) // still can be FullRange, but has more intel
            return bt;

        if (IsFullRange() && GetType() != DEFAULT_TYPE)
            return UnknownTypedValue.Create(type);

        return this;
    }

    public static UnknownValueBase MaybeNormalize(UnknownValueBase value)
    {
        if (value is UnknownTypedValue typedValue)
            return typedValue.Normalize();

        return value;
    }

    public sealed override UnknownValueBase Xor(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (right == this)
            return Zero(type);

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return this;
            if (l == -1 && TryGetSizeInBits(right) == type.nbits)
                return MaybeConvertTo<UnknownValueBitTracker>().BitwiseNot();
        }

        if (right is UnknownTypedValue otherTyped)
        {
            if (otherTyped.IsZero())
                return this;
            if (otherTyped is UnknownValueBitTracker otherBT)
            {
                if (CanConvertTo<UnknownValueBitTracker>())
                    return otherBT.TypedXor(ConvertTo<UnknownValueBitTracker>());
                return otherBT.TypedXor(this);
            }
        }

        return MaybeNormalize(MaybeConvertTo<UnknownValueBitTracker>().TypedXor(right));
    }

    public override object Cast(TypeDB.IntInfo toType)
    {
        if (toType == TypeDB.Bool)
        {
            switch (Cardinality())
            {
                case 0:
                    return UnknownValue.Create(TypeDB.Bool);
                case 1:
                    return !Contains(0);
                default:
                    return Contains(0) ? UnknownValue.Create(TypeDB.Bool) : true;
            }
        }
        throw new NotImplementedException($"{ToString()}.Cast({toType}): not implemented.");
    }

    public sealed override UnknownValueBase BitwiseAnd(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create(); // can be narrower, but type is unknown

        if (right == this)
            return this;

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return Zero(type);
            if (l == -1 && TryGetSizeInBits(right) == type.nbits) // TODO: 255 for byte, etc
                return this; // full range, no change
        }

        if (right is UnknownTypedValue otherTyped && otherTyped.IsZero())
            return Zero(type);

        if (IsFullRange() && right is UnknownValueBitsBase bb)
            return bb;

        if (right is UnknownValueBitTracker otherBT) // try to use UnknownValueBitTracker first bc it retains more information
            return otherBT.TypedBitwiseAnd(this);

        if (CanConvertTo<UnknownValueBitTracker>())
            return ConvertTo<UnknownValueBitTracker>().BitwiseAnd(right);

        if (this is UnknownValueBitsBase bits)
            return bits.TypedBitwiseAnd(right);

        if (!TryConvertToLong(right, out long mask))
            return UnknownValue.Create(type);

        if (Cardinality() <= MAX_DISCRETE_CARDINALITY)
            return new UnknownValueSet(type, Values().Select(v => v & mask));

        return UnknownValueBits.CreateFromAnd(type, mask);
    }

    public sealed override UnknownValueBase BitwiseOr(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create(); // can be narrower, but type is unknown

        if (right == this)
            return this;

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return this;
        }

        if (right is UnknownTypedValue otherTyped && otherTyped.IsZero())
            return this;

        if (right is UnknownValueBitTracker otherBT) // try to use UnknownValueBitTracker first bc it retains more information
            return otherBT.TypedBitwiseOr(this);

        if (CanConvertTo<UnknownValueBitTracker>())
            return ConvertTo<UnknownValueBitTracker>().BitwiseOr(right);

        if (this is UnknownValueBitsBase bits)
            return bits.TypedBitwiseOr(right);

        if (!TryConvertToLong(right, out long mask))
            return UnknownValue.Create(type);

        if (Cardinality() <= MAX_DISCRETE_CARDINALITY)
            return new UnknownValueSet(type, Values().Select(v => v | mask));

        return UnknownValueBits.CreateFromOr(type, mask);
    }

    public sealed override UnknownValueBase Sub(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (right == this)
            return Zero(type);

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return this;

            if (l < 0)
                return Add(-l);

            if (CanConvertTo<UnknownValueBitTracker>())
                return ConvertTo<UnknownValueBitTracker>().Sub(l);
        }

        if (right is UnknownTypedValue otherTyped)
        {
            if (otherTyped.IsZero())
                return this;

            ulong productCardinality = Cardinality() * otherTyped.Cardinality(); // pessimistic
            var typedResult = TypedSub(right);

            if (typedResult.Cardinality() < productCardinality)
                return typedResult;

            return new UnknownValueSet(type,
                    Values()
                    .SelectMany(l => otherTyped.Values(), (l, r) => MaskWithSign(l - r)));
        }
        return MaybeNormalize(TypedSub(right));
    }

    public override UnknownValueBase Merge(object other)
    {
        return other switch
        {
            UnknownTypedValue otherTyped => (otherTyped.type == type) ? UnknownValue.Create(type) : UnknownValue.Create(),
            _ => base.Merge(other)
        };
    }
}
