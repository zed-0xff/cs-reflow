public class UnknownValueRanges : UnknownValueRangeBase
{
    readonly LongRangeSet _rangeSet = new(); // should be immutable!

    public UnknownValueRanges(TypeDB.IntType type, LongRangeSet rangeSet) : base(type)
    {
        _rangeSet = rangeSet;
    }

    public UnknownValueRanges(TypeDB.IntType type, long min, long max) : base(type)
    {
        _rangeSet.Add(new LongRange(min, max));
    }

    public UnknownValueRanges(TypeDB.IntType type, IEnumerable<LongRange> ranges) : base(type)
    {
        _rangeSet.Add(ranges);
    }

    public override UnknownValueBase WithTag(string key, object? value) => HasTag(key, value) ? this : new(type, _rangeSet) { _tags = add_tag(key, value) };
    public override UnknownValueBase WithVarID(int id) => Equals(_var_id, id) ? this : new(type, _rangeSet) { _var_id = id };
    public override UnknownTypedValue WithType(TypeDB.IntType type) => new UnknownValueRanges(type, _rangeSet); // TODO: type conversion

    public override bool Equals(object? obj)
    {
        if (obj is UnknownValueRanges r)
        {
            return type == r.type && _rangeSet.Equals(r._rangeSet);
        }
        return false;
    }

    public override long Min() => _rangeSet.Min;
    public override long Max() => _rangeSet.Max;
    public override bool IsFullRange() => _rangeSet.Count == 1 && _rangeSet.First().Equals(type.Range);
    public override BitSpan BitSpan() => _rangeSet.BitSpan();

    // TODO: DRY with UnknownValueRange.Cast()
    public override object TypedCast(TypeDB.IntType toType)
    {
        if (type == TypeDB.UInt && toType == TypeDB.Int) // uint -> int
        {
            if (IsFullRange())
                return new UnknownValueRange(this, toType, toType.Range);
            if (_rangeSet.Min >= 0 && _rangeSet.Max <= int.MaxValue)
                return new UnknownValueRanges(TypeDB.Int, _rangeSet);
            if (_rangeSet.Min > int.MaxValue)
                return new UnknownValueRanges(TypeDB.Int, _rangeSet.Ranges.Select(r => new LongRange((int)r.Min, (int)r.Max)));
            return new UnknownValueRange(TypeDB.Int);
        }

        if (type == TypeDB.Int && toType == TypeDB.UInt) // int -> uint
        {
            if (IsFullRange())
                return new UnknownValueRange(this, toType, toType.Range);
            if (_rangeSet.Min >= 0 && _rangeSet.Max >= 0)
                return new UnknownValueRanges(TypeDB.UInt, _rangeSet);
            if (_rangeSet.Max < 0)
                return new UnknownValueRanges(TypeDB.UInt, _rangeSet.Ranges.Select(r => new LongRange((uint)r.Min, (uint)r.Max)));
            return new UnknownValueRange(TypeDB.UInt);
        }
        return this;
    }

    public override UnknownValueBase TypedDiv(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(type);

        var newRangeSet = new LongRangeSet(_rangeSet.Ranges.Select(range => range / l));

        return (newRangeSet.Count == 1) ?
            new UnknownValueRange(type, newRangeSet.First()) :
            new UnknownValueRanges(type, newRangeSet);
    }

    public override UnknownTypedValue TypedAdd(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownTypedValue.Create(type);

        List<LongRange> newRanges = new();
        foreach (var range in _rangeSet.Ranges)
        {
            var newMin = MaskWithSign(range.Min + l);
            var newMax = MaskWithSign(range.Max + l);
            if (newMin <= newMax)
                newRanges.Add(new LongRange(newMin, newMax));
            else
            {
                if (type == TypeDB.ULong)
                    throw new NotImplementedException();

                newRanges.Add(new LongRange(newMin, type.MaxSignedValue));
                newRanges.Add(new LongRange(type.MinValue, newMax));
            }
        }
        var newRangeSet = new LongRangeSet(newRanges);

        return (newRangeSet.Count == 1) ?
            new UnknownValueRange(type, newRangeSet.First()) :
            new UnknownValueRanges(type, newRangeSet);
    }

    public override UnknownValueBase TypedSub(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(type);

        List<LongRange> newRanges = new();
        foreach (var range in _rangeSet.Ranges)
        {
            var newMin = MaskWithSign(range.Min - l);
            var newMax = MaskWithSign(range.Max - l);
            if (newMin <= newMax)
                newRanges.Add(new LongRange(newMin, newMax));
            else
            {
                if (type == TypeDB.ULong)
                    throw new NotImplementedException();

                newRanges.Add(new LongRange(newMin, type.MaxSignedValue));
                newRanges.Add(new LongRange(type.MinValue, newMax));
            }
        }
        var newRangeSet = new LongRangeSet(newRanges);

        return (newRangeSet.Count == 1) ?
            new UnknownValueRange(type, newRangeSet.First()) :
            new UnknownValueRanges(type, newRangeSet);
    }

    public override UnknownTypedValue TypedShiftLeft(object right)
    {
        throw new NotImplementedException($"{this}.TypedShiftLeft({right}): not implemented.");
    }

    public override UnknownValueBase TypedSignedShiftRight(object right) // '>>'
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(type);

        var newRangeSet = new LongRangeSet(_rangeSet.Ranges.Select(range => range >> (int)l));

        return (newRangeSet.Count == 1) ?
            new UnknownValueRange(type, newRangeSet.First()) :
            new UnknownValueRanges(type, newRangeSet);
    }

    public override UnknownValueBase TypedUnsignedShiftRight(object right) // '>>>'
    {
        throw new NotImplementedException();
    }

    public override UnknownValueBase TypedMod(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(type);

        // TODO: case when left is not full range
        if (l > 0)
            return new UnknownValueRange(type, new LongRange(0, l - 1));
        else
            return new UnknownValueRange(type, new LongRange(l + 1, 0));

    }

    public override UnknownValueBase TypedXor(object right)
    {
        if (!TryConvertToLong(right, out long l) || Cardinality() > MAX_DISCRETE_CARDINALITY)
            return UnknownValue.Create(type);

        return new UnknownValueSet(type, Values().Select(v => v ^ l));
    }

    public override UnknownValueBase Negate()
    {
        if (IsFullRange())
            return new UnknownValueRange(type);

        List<LongRange> newRanges = new();
        int nskip = 0;
        if (type.signed && Contains(type.MinValue))
        {
            var firstRange = _rangeSet.First();
            newRanges.Add(new LongRange(type.MinValue, type.MinValue)); // edge case: -128 for sbyte, -32768 for short, etc.
            if (firstRange.Min != type.MinValue)
                throw new InvalidOperationException($"Unexpected range {firstRange} for type {type} in Negate().");
            if (firstRange.Min != firstRange.Max)
                newRanges.Add(new LongRange(-firstRange.Max, Math.Min(-firstRange.Min, type.MaxSignedValue)));
            nskip = 1;
        }
        newRanges.AddRange(_rangeSet.Ranges.Skip(nskip).Select(range => new LongRange(-range.Max, -range.Min)));

        var newRangeSet = new LongRangeSet(newRanges);
        return (newRangeSet.Count == 1) ?
            new UnknownValueRange(type, newRangeSet.First()) :
            new UnknownValueRanges(type, newRangeSet);
    }

    public override IEnumerable<long> Values() => _rangeSet.Values();
    public override CardInfo Cardinality() => _rangeSet.Cardinality();
    public override int GetHashCode() => HashCode.Combine(type, _rangeSet);
    public override bool TypedContains(long value) => _rangeSet.Contains(value);

    public override string ToString()
    {
        if (_rangeSet.Count <= 3)
            return $"UnknownValueRanges<{type}>{{{string.Join(", ", _rangeSet.Ranges.Select(r => r.ToString()))}}}";
        else
            return $"UnknownValueRanges<{type}>[{_rangeSet.Count}]";
    }

    public override bool Typed_IntersectsWith(UnknownTypedValue right)
    {
        return right switch
        {
            UnknownValueRange r => _rangeSet.Ranges.Any(range => range.IntersectsWith(r.Range)),
            UnknownValueRanges rr => _rangeSet.Ranges.Any(range => rr._rangeSet.Ranges.Any(r2 => range.IntersectsWith(r2))),
            UnknownValueSet l => l.Values().Any(v => Contains(v)),
            _ => throw new NotImplementedException($"{ToString()}.Typed_IntersectsWith({right}): not implemented.")
        };
    }

    public override UnknownValueBase BitwiseNot()
    {
        var newRanges = new List<LongRange>();
        foreach (var range in _rangeSet.Ranges)
        {
            long min = MaskWithSign(~range.Max);
            long max = MaskWithSign(~range.Min);
            if (max < min)
                (min, max) = (max, min);
            newRanges.Add(new LongRange(min, max));
        }

        var newRangeSet = new LongRangeSet(newRanges);
        return (newRangeSet.Count == 1) ?
            new UnknownValueRange(type, newRangeSet.First()) :
            new UnknownValueRanges(type, newRangeSet);
    }

    public override UnknownValueBase Merge(object other)
    {
        return base.Merge(other);
    }

    public override UnknownTypedValue Normalize()
    {
        if (_rangeSet.Count == 1)
            return new UnknownValueRange(type, type.Range).Normalize();

        return base.Normalize();
    }
}

