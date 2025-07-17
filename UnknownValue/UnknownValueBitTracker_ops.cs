using BitType = int;
using UnkBits = UnknownValueBits;

public partial class UnknownValueBitTracker
{
    public override bool Typed_IntersectsWith(UnknownTypedValue other)
    {
        if (other is UnknownValueBitTracker otherBT)
        {
            if (type.nbits != otherBT.type.nbits)
                return false;

            for (int i = 0; i < type.nbits; i++)
            {
                if (_bits[i] == otherBT._bits[i] || _bits[i] == ANY || otherBT._bits[i] == ANY)
                    continue;

                return false;
            }
            return true;
        }

        if (other is UnkBits otherUnk)
        {
            if (type.nbits != otherUnk.type.nbits)
                return false;

            var otherBits = otherUnk.GetBits().ToList();
            for (int i = 0; i < type.nbits; i++)
            {
                if (_bits[i] == otherBits[i] || _bits[i] == ANY || otherBits[i] == UnkBits.ANY)
                    continue;

                return false;
            }
            return true;
        }

        if (other.Cardinality() > 10_000 && this.Cardinality() > 10_000)
            Logger.warn($"Large intersection check: {this} with {other}.");

        if (other.Cardinality() < this.Cardinality())
            return other.Values().Any(v => Contains(v));
        else
            return Values().Any(v => other.Contains(v));
    }

    public override UnknownTypedValue TypedShiftLeft(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<BitType> newBits = new(_bits);
        for (int i = 0; i < l; i++)
        {
            newBits.RemoveAt(_bits.Count - 1);
            newBits.Insert(0, ZERO); // should be after remove!
        }
        return new UnknownValueBitTracker(this, newBits);
    }

    public override UnknownTypedValue TypedSignedShiftRight(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        BitType sign = type.signed ? _bits[type.nbits - 1] : ZERO;
        List<BitType> newBits = new(_bits);
        for (int i = 0; i < l; i++)
        {
            newBits.RemoveAt(0);
            newBits.Add(sign);
        }
        return new UnknownValueBitTracker(this, newBits);
    }

    public override UnknownTypedValue TypedUnsignedShiftRight(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<BitType> newBits = new(_bits);
        for (int i = 0; i < l; i++)
        {
            newBits.RemoveAt(0);
            newBits.Add(ZERO);
        }
        return new UnknownValueBitTracker(this, newBits);
    }

    public override UnknownValueBase TypedBitwiseAnd(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            // AND with a constant value
            List<BitType> newBits = new(_bits);
            long m = 1;
            for (int i = 0; i < type.nbits; i++, m <<= 1)
            {
                if ((l & m) == 0)
                    newBits[i] = ZERO;
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        if (right is UnknownValueBitTracker otherBT)
        {
            if (type != otherBT.type)
                throw new NotImplementedException($"Cannot AND {type} with {otherBT.type}");

            List<BitType> newBits = new(_bits);
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBT._bits[i]) switch
                {
                    (ZERO, _) => ZERO,
                    (_, ZERO) => ZERO,

                    (ANY, _) => ANY,
                    (_, ANY) => ANY,

                    (ONE, ONE) => ONE,

                    (ONE, BitType x) => x,
                    (BitType x, ONE) => x,

                    (BitType x, BitType y) =>
                        x == y ? x :
                        x == -y ? ZERO :
                        ANY,
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        if (right is UnkBits otherUnk)
        {
            if (type != otherUnk.type)
                throw new NotImplementedException($"Cannot AND {type} with {otherUnk.type}");

            List<BitType> newBits = new(_bits);
            var otherBitsList = otherUnk.GetBits().ToList();
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBitsList[i]) switch
                {
                    (ZERO, _) => ZERO,
                    (_, UnkBits.ZERO) => ZERO,

                    (ANY, _) => ANY,
                    (_, UnkBits.ANY) => ANY,

                    (_, UnkBits.ONE) => newBits[i],

                    _ => throw new NotImplementedException($"Cannot AND {type} with {otherUnk.type} at bit {i}")
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        // keep only zeroes
        return base.TypedBitwiseAnd(right);
    }

    public override UnknownValueBase Merge(object right)
    {
        if (right is UnknownValueBitTracker otherBT)
        {
            if (type.nbits != otherBT.type.nbits)
                throw new NotImplementedException($"Cannot merge {type} with {otherBT.type}");

            List<BitType> newBits = new(_bits);
            for (int i = 0; i < type.nbits; i++)
                if (newBits[i] != otherBT._bits[i])
                    newBits[i] = ANY;
            return new UnknownValueBitTracker(this, newBits);
        }

        return base.Merge(right);
    }

    public override UnknownValueBase TypedBitwiseOr(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            // OR with a constant value
            List<BitType> newBits = new(_bits);
            long m = 1;
            for (int i = 0; i < type.nbits; i++, m <<= 1)
            {
                if ((l & m) != 0)
                    newBits[i] = ONE;
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        if (right is not UnknownTypedValue otherTyped)
            return base.TypedBitwiseOr(right);

        if (otherTyped is UnknownValueSet otherSet)
        {
            if (type != otherSet.type)
                throw new NotImplementedException($"Cannot OR {type} with {otherSet.type}");

            UnknownValueBitTracker result = this;
            foreach (long v in otherSet.Values())
            {
                if (v == 0)
                    continue; // zero does not change anything

                switch (result.Merge(result.TypedBitwiseOr(v)))
                {
                    case UnknownValue unk:
                        return unk;
                    case UnknownValueBitTracker bt:
                        result = bt;
                        break;
                    case UnknownTypedValue other:
                        if (other.IsFullRange())
                            return other;
                        throw new NotImplementedException($"Unexpected result of OR with set: {other}");
                }
            }
            return result;
        }

        if (otherTyped.CanConvertTo(this))
            right = otherTyped.ConvertTo<UnknownValueBitTracker>();
        if (right is UnknownValueBitTracker otherBT)
        {
            if (type != otherBT.type)
                throw new NotImplementedException($"Cannot OR {type} with {otherBT.type}");

            List<BitType> newBits = new(_bits);
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBT._bits[i]) switch
                {
                    (ONE, _) or (_, ONE) => ONE,
                    (ANY, _) or (_, ANY) => ANY,

                    (ZERO, ZERO) => ZERO,

                    (ZERO, BitType x) => x,
                    (BitType x, ZERO) => x,

                    (BitType x, BitType y) =>
                        x == y ? x :
                        x == -y ? ONE :
                        ANY
                    // all cases covered
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        if (otherTyped.CanConvertTo<UnknownValueBits>())
            right = otherTyped.ConvertTo<UnknownValueBits>();
        if (right is UnkBits otherUnk)
        {
            if (type != otherUnk.type)
                throw new NotImplementedException($"Cannot OR {type} with {otherUnk.type}");

            List<BitType> newBits = new(_bits);
            var otherBitsList = otherUnk.GetBits().ToList();
            for (int i = 0; i < type.nbits; i++)
            {
                newBits[i] = (newBits[i], otherBitsList[i]) switch
                {
                    (_, UnkBits.ONE) or (ONE, _) => ONE,
                    (_, UnkBits.ANY) or (ANY, _) => ANY,
                    (_, UnkBits.ZERO) => newBits[i],

                    _ => throw new NotImplementedException($"Cannot AND {type} with {otherUnk.type} at bit {i}")
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        return TypedBitwiseOr(otherTyped.ToBits()); // lossy ToBits()
    }

    public override UnknownValueBitTracker BitwiseNot() =>
        new UnknownValueBitTracker(this, _bits.Select(b => b.Invert()));

    public override UnknownValueBase Negate() => BitwiseNot().Add(1);

    public override UnknownTypedValue TypedAdd(object right)
    {
        if (object.Equals(right, this)) // works because all bits have unique ids
            return TypedShiftLeft(1);

        if (TryConvertToLong(right, out long l))
            return add(l);

        if (right is UnknownValueBitTracker otherBT)
        {
            if (type != otherBT.type)
                throw new NotImplementedException($"Cannot add {type} with {otherBT.type}");

            return add(otherBT);
        }

        if (right is UnkBits otherUnk)
        {
            if (type != otherUnk.type)
                throw new NotImplementedException($"Cannot add {type} with {otherUnk.type}");

            return add(otherUnk);
        }

        return UnknownTypedValue.Create(type);
    }

    public override UnknownValueBase TypedXor(object right)
    {
        if (TryConvertToLong(right, out long l))
        {
            // XOR with a constant value
            List<BitType> newBits = new(_bits);
            long m = 1;
            for (int i = 0; i < type.nbits; i++, m <<= 1)
            {
                bool b = (l & m) != 0;
                newBits[i] = newBits[i] switch
                {
                    ANY => ANY,
                    ONE => b ? ZERO : ONE,
                    ZERO => b ? ONE : ZERO,
                    _ => b ? -newBits[i] : newBits[i]
                };
            }
            return new UnknownValueBitTracker(this, newBits);
        }

        if (right is UnknownTypedValue otherTyped)
        {
            if (otherTyped.CanConvertTo(this))
                otherTyped = otherTyped.ConvertTo<UnknownValueBitTracker>();

            if (otherTyped is UnknownValueBitTracker otherBT)
                return xor(otherBT);

            if (otherTyped is UnkBits otherUnk)
                return xor(otherUnk);
        }

        return UnknownTypedValue.Create(type);
    }

    public override UnknownValueBase TypedSub(object right)
    {
        if (TryConvertToLong(right, out long l))
            return add(-l);

        if (right is UnknownTypedValue otherTyped)
            return Add(otherTyped.Negate());

        throw new NotImplementedException();
    }

    public override object Eq(object other)
    {
        if (other is not UnknownValueBitTracker otherBT || otherBT.type != type)
            return base.Eq(other);

        bool inconclusive = false;
        for (int i = 0; i < type.nbits; i++)
        {
            switch (_bits[i], otherBT._bits[i])
            {
                // can still be improved f.ex. when same bit _id_ is compared both with ONE and ZERO
                // i.e. <bbcd> vs <01__>
                case (_, ANY):
                case (ANY, _):
                case (ZERO, ZERO):
                case (ONE, ONE):
                    continue;

                case (ZERO, ONE):
                case (ONE, ZERO):
                    return false;

                case (BitType x1, ONE) when x1.IsPrivateBit():
                case (BitType x2, ZERO) when x2.IsPrivateBit():
                case (ONE, BitType y1) when y1.IsPrivateBit():
                case (ZERO, BitType y2) when y2.IsPrivateBit():
                    inconclusive = true; // but still maybe false if bit is compared with inverted self in other cases
                    continue;

                case (BitType x, BitType y) when x.IsPrivateBit() && y.IsPrivateBit():
                    if (x == y)
                        continue;
                    if (x == -y)
                        return false;
                    inconclusive = true; // different bit ids
                    break;
            }
        }
        return inconclusive ? UnknownValue.Create(TypeDB.Bool) : true;
    }

}
