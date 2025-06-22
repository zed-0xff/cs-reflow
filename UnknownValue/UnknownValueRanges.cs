public class UnknownValueRanges : UnknownTypedValue
{
    readonly LongRangeSet _rangeSet = new(); // should be immutable!

    public UnknownValueRanges(TypeDB.IntInfo type, LongRangeSet rangeSet) : base(type)
    {
        _rangeSet = rangeSet;
    }

    public UnknownValueRanges(TypeDB.IntInfo type, long min, long max) : base(type)
    {
        _rangeSet.Add(new LongRange(min, max));
    }

    public UnknownValueRanges(TypeDB.IntInfo type, IEnumerable<LongRange> ranges) : base(type)
    {
        _rangeSet.Add(ranges);
    }

    public override UnknownValueBase WithTag(object? tag) => Equals(_tag, tag) ? this : new(type, _rangeSet) { _tag = tag };
    public override UnknownValueBase WithVarID(int id) => Equals(_var_id, id) ? this : new(type, _rangeSet) { _var_id = id };

    public override bool Equals(object obj)
    {
        if (obj is UnknownValueRanges r)
        {
            return type == r.type && _rangeSet.Equals(r._rangeSet);
        }
        return false;
    }

    public override long Min() => _rangeSet.Min;
    public override long Max() => _rangeSet.Max;
    public bool IsFullRange() => _rangeSet.Count == 1 && _rangeSet.First().Equals(type.Range);

    // TODO: DRY with UnknownValueRange.Cast()
    public override object Cast(TypeDB.IntInfo toType)
    {
        if (type == TypeDB.UInt && toType == TypeDB.Int) // uint -> int
        {
            if (_rangeSet.Min >= 0 && _rangeSet.Max <= int.MaxValue)
                return new UnknownValueRanges(TypeDB.Int, _rangeSet);
            if (_rangeSet.Min > int.MaxValue)
                return new UnknownValueRanges(TypeDB.Int, _rangeSet.Ranges.Select(r => new LongRange((int)r.Min, (int)r.Max)));
            return new UnknownValueRange(TypeDB.Int);
        }

        if (type == TypeDB.Int && toType == TypeDB.UInt) // int -> uint
        {
            if (_rangeSet.Min >= 0 && _rangeSet.Max >= 0)
                return new UnknownValueRanges(TypeDB.UInt, _rangeSet);
            if (_rangeSet.Max < 0)
                return new UnknownValueRanges(TypeDB.UInt, _rangeSet.Ranges.Select(r => new LongRange((uint)r.Min, (uint)r.Max)));
            return new UnknownValueRange(TypeDB.UInt);
        }
        return base.Cast(toType);
    }

    public override UnknownValueBase Div(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(type);

        var newRangeSet = new LongRangeSet(_rangeSet.Ranges.Select(range => range / l));

        return (newRangeSet.Count == 1) ?
            new UnknownValueRange(type, newRangeSet.First()) :
            new UnknownValueRanges(type, newRangeSet);
    }

    public override UnknownValueBase Add(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(type);

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

    public override UnknownValueBase Sub(object right)
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

    public override UnknownValueBase ShiftLeft(object right)
    {
        return new UnknownValueRange(type);
    }

    public override UnknownValueBase SignedShiftRight(object right) // '>>'
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(type);

        var newRangeSet = new LongRangeSet(_rangeSet.Ranges.Select(range => range >> (int)l));

        return (newRangeSet.Count == 1) ?
            new UnknownValueRange(type, newRangeSet.First()) :
            new UnknownValueRanges(type, newRangeSet);
    }

    public override UnknownValueBase UnsignedShiftRight(object right) // '>>>'
    {
        return UnknownValue.Create(type);
    }

    public override UnknownValueBase Mod(object right)
    {
        if (!TryConvertToLong(right, out long l))
            return UnknownValue.Create(type);

        if (l == 0)
            throw new DivideByZeroException();

        // TODO: case when left is not full range
        if (l > 0)
            return new UnknownValueRange(type, new LongRange(0, l - 1));
        else
            return new UnknownValueRange(type, new LongRange(l + 1, 0));

    }

    public override UnknownValueBase Xor(object right)
    {
        if (!TryConvertToLong(right, out long l) || Cardinality() > (long)MAX_DISCRETE_CARDINALITY)
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

    public override IEnumerable<long> Values()
    {
        return _rangeSet.Values();
    }

    public override long Cardinality()
    {
        return _rangeSet.Ranges.Sum(r => r.Max - r.Min + 1); // because both are inclusive
    }

    public override string ToString()
    {
        if (_rangeSet.Count <= 3)
            return $"UnknownValueRanges<{type}>{{{string.Join(", ", _rangeSet.Ranges.Select(r => r.ToString()))}}}";
        else
            return $"UnknownValueRanges<{type}>[{_rangeSet.Count}]";
    }

    public override bool Contains(long value) => _rangeSet.Contains(value);

    public override int GetHashCode() => HashCode.Combine(type, _rangeSet);

    public override bool IntersectsWith(UnknownTypedValue right)
    {
        return right switch
        {
            UnknownValueRange r => _rangeSet.Ranges.Any(range => range.IntersectsWith(r.Range)),
            UnknownValueRanges rr => _rangeSet.Ranges.Any(range => rr._rangeSet.Ranges.Any(r2 => range.IntersectsWith(r2))),
            UnknownValueSet l => l.Values().Any(v => Contains(v)),
            _ => throw new NotImplementedException($"{ToString()}.IntersectsWith({right}): not implemented.")
        };
    }

    public override UnknownValueBase BitwiseAnd(object right)
    {
        if (right is UnknownValueBits b && IsFullRange())
        {
            return new UnknownValueBits(type, b.Bits);
        }
        return base.BitwiseAnd(right);
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
}

