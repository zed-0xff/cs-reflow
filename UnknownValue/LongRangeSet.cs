public class LongRangeSet
{
    private readonly List<LongRange> _ranges = new();

    public int Count => _ranges.Count; // count of ranges, not individual values
    public IReadOnlyList<LongRange> Ranges => _ranges;
    public LongRange First() => _ranges.First();

    public LongRangeSet() { }

    public LongRangeSet(IEnumerable<LongRange> ranges)
    {
        Add(ranges);
    }

    public ulong Cardinality()
    {
        ulong count = 0;
        foreach (var range in _ranges)
        {
            count += range.Count;
        }
        return count;
    }

    public BitSpan BitSpan()
    {
        long min = ~0L; // all bits set initially
        long max = 0;

        foreach (var range in _ranges)
        {
            var (rmin, rmax) = range.BitSpan();
            min &= rmin;
            max |= rmax;
        }

        return (min, max);
    }

    public void Add(IEnumerable<LongRange> newRanges)
    {
        foreach (var range in newRanges)
        {
            _ranges.Add(range);
        }
        normalize();
    }

    public void Add(LongRange newRange)
    {
        _ranges.Add(newRange);
        normalize();
    }

    void normalize()
    {
        if (_ranges.Count == 0)
            return;

        _ranges.Sort((a, b) => a.Min.CompareTo(b.Min));

        var mergedRanges = new List<LongRange>();
        LongRange current = _ranges[0];

        for (int i = 1; i < _ranges.Count; i++)
        {
            if (current.Max + 1 >= _ranges[i].Min)
            {
                // Overlapping or adjacent ranges, merge them
                current = new LongRange(current.Min, Math.Max(current.Max, _ranges[i].Max));
            }
            else
            {
                // No overlap, add the current range and move to the next
                mergedRanges.Add(current);
                current = _ranges[i];
            }
        }

        // Add the last range
        mergedRanges.Add(current);
        _ranges.Clear();
        _ranges.AddRange(mergedRanges);
    }

    public IEnumerable<long> Values()
    {
        foreach (var range in _ranges)
        {
            for (long i = range.Min; i <= range.Max; i++)
            {
                yield return i;
            }
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is not LongRangeSet other)
            return false;

        if (_ranges.Count != other._ranges.Count)
            return false;

        for (int i = 0; i < _ranges.Count; i++)
        {
            if (!_ranges[i].Equals(other._ranges[i]))
                return false;
        }
        return true;
    }

    public long Min => _ranges[0].Min;
    public long Max => _ranges[^1].Max;

    public bool IntersectsWith(LongRangeSet other)
    {
        foreach (var range in _ranges)
        {
            if (other._ranges.Any(r => r.IntersectsWith(range)))
                return true;
        }
        return false;
    }

    public override int GetHashCode() =>
        _ranges.Aggregate(0, (hash, range) => hash ^ range.GetHashCode());

    public bool Contains(long value)
        => _ranges.Any(r => r.Contains(value));

    public override string ToString()
        => string.Join(", ", _ranges.Select(r => r.ToString()));
}

