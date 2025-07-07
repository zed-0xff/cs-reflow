using Microsoft.CodeAnalysis.CSharp;

public abstract class UnknownTypedValue : UnknownValueBase, TypeDB.IIntType
{
    public static readonly ulong MAX_DISCRETE_CARDINALITY = 1_000_000UL;
    public static readonly Type DEFAULT_TYPE = typeof(UnknownValueRange);

    public TypeDB.IntType type { get; }
    public Microsoft.CodeAnalysis.SpecialType IntTypeID => type.id;
    public bool CanBeNegative => type.CanBeNegative;

    public UnknownTypedValue(TypeDB.IntType type) : base()
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type), "TypeDB.IntType cannot be null");
        this.type = type;
    }

    public abstract UnknownTypedValue WithType(TypeDB.IntType type);

    public abstract UnknownTypedValue TypedAdd(object right);
    public abstract UnknownValueBase TypedDiv(object right);
    public abstract UnknownValueBase TypedMod(object right);
    public abstract UnknownValueBase TypedSub(object right);
    public abstract UnknownValueBase TypedXor(object right);

    public abstract UnknownTypedValue TypedShiftLeft(object right);
    public abstract UnknownValueBase TypedSignedShiftRight(object right);
    public abstract UnknownValueBase TypedUnsignedShiftRight(object right);

    public static UnknownValueRange Create(TypeDB.IntType type) => new UnknownValueRange(type); // DEFAULT_TYPE

    static readonly Dictionary<TypeDB.IntType, UnknownTypedValue> _zeroes = new();
    static readonly Dictionary<TypeDB.IntType, UnknownTypedValue> _ones = new();

    public static UnknownTypedValue Zero(TypeDB.IntType type) =>
        _zeroes.TryGetValue(type, out var zero) ? zero :
        (_zeroes[type] = new UnknownValueRange(type, 0, 0));

    public static UnknownTypedValue One(TypeDB.IntType type) =>
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

    public UnknownValueBitsBase ToBits() =>
        (this is UnknownValueBitsBase bits) ? bits :
        (_var_id is null) ? new UnknownValueBits(type, BitSpan()) : new UnknownValueBitTracker(type, _var_id.Value, BitSpan());

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
        if (typeof(T) == typeof(UnknownValueBitTracker) && _var_id is not null)
            return new UnknownValueBitTracker(type, _var_id.Value);

        throw new NotSupportedException($"Cannot convert {GetType()} to {typeof(T)}");
    }

    public virtual bool CanConvertTo<T>()
    {
        if (typeof(T) == typeof(UnknownValueBitTracker))
        {
            return this is not UnknownValueBitTracker
                && _var_id is not null
                && IsFullRange(); // TODO: IsMonotonic
        }
        if (typeof(UnknownValueBitsBase).IsAssignableFrom(typeof(T)) && typeof(T) != typeof(UnknownValueBitTracker))
            return IsFullRange(); // TODO: IsMonotonic

        return false;
    }

    public UnknownTypedValue MaybeConvertTo<T>() where T : UnknownValueBitTracker
    {
        if (CanConvertTo<T>())
            return ConvertTo<T>();

        return this;
    }

    public override object UnaryOp(SyntaxKind op) =>
        op switch
        {
            SyntaxKind.PostIncrementExpression => Add(1),
            SyntaxKind.PostDecrementExpression => Sub(1),
            SyntaxKind.BitwiseNotExpression => BitwiseNot(),
            SyntaxKind.UnaryPlusExpression => this,
            SyntaxKind.UnaryMinusExpression => Negate(),
            SyntaxKind.LogicalNotExpression => Eq(0),
            _ => throw new NotImplementedException($"{ToString()}.UnaryOp({op}): not implemented"),
        };

    private object BinaryOpNoPromote(SyntaxKind kind, object rValue)
    {
        var result = kind switch
        {
            SyntaxKind.AddExpression => Add(rValue),
            SyntaxKind.SubtractExpression => Sub(rValue),
            SyntaxKind.MultiplyExpression => Mul(rValue),
            SyntaxKind.DivideExpression => Div(rValue),
            SyntaxKind.ModuloExpression => Mod(rValue),
            SyntaxKind.ExclusiveOrExpression => Xor(rValue),

            SyntaxKind.NotEqualsExpression => Ne(rValue),
            SyntaxKind.LessThanExpression => Lt(rValue),
            SyntaxKind.LessThanOrEqualExpression => Lte(rValue),
            SyntaxKind.EqualsExpression => Eq(rValue),
            SyntaxKind.GreaterThanExpression => Gt(rValue),
            SyntaxKind.GreaterThanOrEqualExpression => Gte(rValue),

            SyntaxKind.BitwiseAndExpression => BitwiseAnd(rValue),
            SyntaxKind.BitwiseOrExpression => BitwiseOr(rValue),

            SyntaxKind.LeftShiftExpression => ShiftLeft(rValue),
            SyntaxKind.RightShiftExpression => SignedShiftRight(rValue),
            SyntaxKind.UnsignedRightShiftExpression => UnsignedShiftRight(rValue),

            _ => throw new NotImplementedException($"{ToString()}.BinaryOp({kind}): not implemented"),
        };

        // materialize the UnknownTypedValue if it has only one value
        if (result is UnknownTypedValue ut && ut.Cardinality() == 1)
            return ut.type.ConvertInt(ut.Values().First());

        return result;
    }

    // NOTE: applicable to all operations, but << / >> / >>>
    bool MaybePromote(object rValue, out UnknownTypedValue promotedL, out object promotedR)
    {
        var (tl, tr) = TypeDB.Promote(this, rValue);
        if (tl is not null || tr is not null)
        {
            Logger.debug(() => $"{ToString()}.MaybePromote({rValue}): promoting {tl} and {tr}", "UnknownTypedValue.MaybePromote");
            promotedL = (tl is null) ? this : Upcast(tl);
            if (ReferenceEquals(this, rValue))
            {
                promotedR = promotedL;
            }
            else
            {
                promotedR = (tr is null) ? rValue :
                    rValue switch
                    {
                        UnknownTypedValue rt => rt.Upcast(tr),
                        _ => tr.ConvertAny(rValue)
                    };
            }
            return true;
        }

        promotedL = this;
        promotedR = rValue;
        return false;
    }

    public override object BinaryOp(SyntaxKind kind, object rValue)
    {
        if (rValue is UnknownValue)
            return UnknownValue.Create();

        if (kind == SyntaxKind.LeftShiftExpression || kind == SyntaxKind.RightShiftExpression || kind == SyntaxKind.UnsignedRightShiftExpression)
        {
            if (type.ByteSize < 4)
                return Upcast(TypeDB.Int).BinaryOpNoPromote(kind, rValue);
        }
        else if (MaybePromote(rValue, out var promotedL, out var promotedR))
        {
            return promotedL.BinaryOpNoPromote(kind, promotedR);
        }

        return BinaryOpNoPromote(kind, rValue);
    }

    // swap the left and right operands
    // XXX it is necessary to call BinaryOp() here bc BinaryOp() also handles type promotion
    public override object InverseBinaryOp(SyntaxKind kind, object lValue) =>
        kind switch
        {
            SyntaxKind.AddExpression => BinaryOp(kind, lValue),
            SyntaxKind.SubtractExpression => Negate().BinaryOp(SyntaxKind.AddExpression, lValue), // N - unk = (-unk) + N
            SyntaxKind.MultiplyExpression => BinaryOp(kind, lValue),
            SyntaxKind.ExclusiveOrExpression => BinaryOp(kind, lValue),
            SyntaxKind.BitwiseAndExpression => BinaryOp(kind, lValue),
            SyntaxKind.BitwiseOrExpression => BinaryOp(kind, lValue),
            SyntaxKind.NotEqualsExpression => BinaryOp(kind, lValue),
            SyntaxKind.EqualsExpression => BinaryOp(kind, lValue),

            SyntaxKind.LessThanExpression => BinaryOp(SyntaxKind.GreaterThanOrEqualExpression, lValue),
            SyntaxKind.LessThanOrEqualExpression => BinaryOp(SyntaxKind.GreaterThanExpression, lValue),
            SyntaxKind.GreaterThanExpression => BinaryOp(SyntaxKind.LessThanOrEqualExpression, lValue),
            SyntaxKind.GreaterThanOrEqualExpression => BinaryOp(SyntaxKind.GreaterThanExpression, lValue),

            _ => throw new NotImplementedException($"{ToString()}.InverseBinaryOp(): '{kind}' is not implemented"),
        };

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

        var (tl, tr) = TypeDB.Promote(this, right);

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

        if (right is UnknownTypedValue otherTyped && otherTyped.IsZero()) // should be before TryConvertToLong (DRY)
            return this;

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

            if (CanConvertTo<UnknownValueBitTracker>())
                return ConvertTo<UnknownValueBitTracker>().TypedShiftLeft(l);
        }

        return TypedShiftLeft(right);
    }

    public sealed override UnknownValueBase SignedShiftRight(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (right is UnknownTypedValue otherTyped && otherTyped.IsZero()) // should be before TryConvertToLong (DRY)
            return this;

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return this;
            if (l < 0)
                throw new ArgumentException("Shift count cannot be negative.");
            // if (l >= type.nbits && type.unsigned) // TODO: check // TODO: return MinusOne for signed?
            //     return Zero(type);

            if (CanConvertTo<UnknownValueBitTracker>())
                return ConvertTo<UnknownValueBitTracker>().SignedShiftRight(l);
        }

        return TypedSignedShiftRight(right);
    }

    public sealed override UnknownValueBase UnsignedShiftRight(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (right is UnknownTypedValue otherTyped && otherTyped.IsZero()) // should be before TryConvertToLong (DRY)
            return this;

        if (TryConvertToLong(right, out long l))
        {
            if (l == 0)
                return this;
            if (l < 0)
                throw new ArgumentException("Shift count cannot be negative.");
            if (l >= type.nbits)
                return Zero(type);

            if (CanConvertTo<UnknownValueBitTracker>())
                return ConvertTo<UnknownValueBitTracker>().UnsignedShiftRight(l);
        }

        return TypedUnsignedShiftRight(right);
    }

    static public bool IsPowerOfTwo(long l) => l > 0 && (l & (l - 1)) == 0;

    public sealed override UnknownValueBase Mod(object right)
    {
        if (right is UnknownValue)
            return UnknownValue.Create();

        if (MaybePromote(right, out var promotedL, out var promotedR))
            return promotedL.Mod(promotedR);

        if (TryConvertToLong(right, out long l))
        {
            if (l < 0)
                l = -l; // in C# always x%y == x % (-y)

            if (l == 0)
                return UnknownValue.Create(); // division by zero
            if (l == 1)
                return Zero(type);

            if (IsPowerOfTwo(l))
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

        return MaybeNormalize(TypedMod(right));
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

            if (IsPowerOfTwo(l))
                return SignedShiftRight((int)Math.Log2(l)); // cast is necessary, or float will be sent to ShiftRight()
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

        bool isNumeric = TryConvertToLong(right, out long l);

        if (isNumeric)
        {
            if (l == 0) return Zero(type);
            if (l == 1) return this;
            if (l == -1) return Negate();

            if (IsPowerOfTwo(l))
                return ShiftLeft((int)Math.Log2(l)); // cast is necessary, or float will be sent to ShiftLeft()
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

    public virtual UnknownTypedValue Upcast(TypeDB.IntType toType)
    {
        if (toType == type)
            return this;

        if (toType.nbits >= type.nbits && toType.signed == type.signed)
            return WithType(toType);

        if (toType.nbits > type.nbits && !type.signed && toType.signed)
            return WithType(toType);

        throw new NotImplementedException($"{ToString()}.Upcast({toType}): not implemented. (toType.nbits = {toType.nbits}, type.nbits = {type.nbits})");
    }

    public override object Cast(TypeDB.IntType toType)
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
                    return Contains(0) ? ((type == toType) ? this : UnknownValue.Create(TypeDB.Bool)) : true;
            }
        }

        if (toType.ByteSize < type.ByteSize)
        {
            var res = BitwiseAnd(toType.Mask);
            return res switch
            {
                UnknownTypedValue utv => utv.WithType(toType),
                UnknownValue uv => UnknownTypedValue.Create(toType),
                _ => throw new NotSupportedException($"Cannot cast {GetType()} to {toType}")
            };
        }

        return Upcast(toType);
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

        if (this is UnknownValueBitsBase thisBits)
            return thisBits.TypedBitwiseOr(right);

        if (CanConvertTo<UnknownValueBitsBase>())
            return ToBits().BitwiseOr(right);

        if (!TryConvertToLong(right, out long mask))
            return UnknownValue.Create(type);

        if (Cardinality() <= MAX_DISCRETE_CARDINALITY)
            return new UnknownValueSet(type, Values().Select(v => v | mask));

        // lossy - BitSpan() for range will mark some extra bits as ANY, bc not all ranges can be represented as a BitSpan
        return new UnknownValueBits(type, BitSpan() | mask);
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

            float productCardinality = (float)Cardinality() * otherTyped.Cardinality(); // pessimistic
            var typedResult = TypedSub(right);

            if (typedResult.Cardinality() < productCardinality)
                return typedResult;

            if (productCardinality < MAX_DISCRETE_CARDINALITY)
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
