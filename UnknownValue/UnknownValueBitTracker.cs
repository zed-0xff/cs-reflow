using System.Collections.ObjectModel;

using BitType = int;
using UnkBits = UnknownValueBits;

public partial class UnknownValueBitTracker : UnknownValueBitsBase
{
    public const BitType ANY = -1;
    public const BitType ONE = 1;
    public const BitType ZERO = 0;

    // rough visual representation of the _bits
    // requirements:
    //  - at least 32 unique chars (better 64, but for now most ints are 32-bit)
    //  - each char should have upper/lower (on/off) visually distinct representation
    //  - ideally a prime number of chars for better distribution
    private static readonly string BITMAP = // 69 total
        "abcdefghijklmnopqrstuvwxyz" + // 26
        "абвгдеёжзийклмнопрстуфхцчшщъыьэюя" + // 33
        "âêîôû" + // 5
        "ãẽĩõũ"; // 5

    // every bit is an int, with possible values:
    //   -1  any bit (unknown)
    //    0  zero bit
    //    1  one bit
    //    2+ unique bit id (2**30 values, so 16M 64-bit vars)
    //  <-2  inverted bit id
    readonly ReadOnlyCollection<BitType> _bits;
    public ReadOnlyCollection<BitType> Bits => _bits; // for tests only?

    public UnknownValueBitTracker(UnknownTypedValue parent, IEnumerable<BitType>? bits = null)
        : base(parent.type, bits == null ? parent.BitSpan() : bits2span(parent.type, bits))
    {
        if (parent._var_id is null)
            throw new ArgumentException("Parent must have a var_id set.", nameof(parent));

        _var_id = parent._var_id;

        int required_bits = 0;
        bool need_sign_extend = false;

        if (parent is UnknownValueRange range)
        {
            required_bits = range.RequiredBits();
            need_sign_extend = type.signed && range.Min() < 0;
        }
        _bits = new(init(bits, required_bits, need_sign_extend));
    }

    public UnknownValueBitTracker(TypeDB.IntType type, int var_id, IEnumerable<BitType>? bits = null)
        : base(type, bits2span(type, bits))
    {
        _var_id = var_id;
        _bits = new(init(bits));
    }

    public UnknownValueBitTracker(TypeDB.IntType type, int var_id, BitSpan bitspan) : base(type, bitspan)
    {
        _var_id = var_id;
        _bits = new(span2bits(init(null), bitspan, ZERO, ONE));
    }

    // expects that _var_id is already set
    List<BitType> init(IEnumerable<BitType>? bits, int required_bits = 0, bool need_sign_extend = false)
    {
        if (required_bits == 0)
            required_bits = type.nbits;

        if (bits is null)
        {
            if (_var_id is null)
                throw new ArgumentException("Cannot initialize bits without var_id set.", nameof(bits));
            var list = Enumerable.Range(2 + _var_id.Value * required_bits, required_bits).ToList();

            if (required_bits < type.nbits)
                list.AddRange(Enumerable.Repeat(need_sign_extend ? list.Last() : ZERO, type.nbits - required_bits));
            else if (required_bits > type.nbits)
                throw new ArgumentException($"Cannot initialize bits with {required_bits} bits for type with {type.nbits} bits.");

            return list;
        }

        if (bits.Count() != type.nbits)
            throw new ArgumentException($"Expected {type.nbits} bits, but got {bits.Count()} bits.");

        return new List<BitType>(bits);
    }

    public override UnknownValueBitTracker Create(BitSpan bitspan) => new(this, span2bits(_bits, bitspan, ZERO, ONE));

    protected static BitSpan bits2span(TypeDB.IntType type, IEnumerable<BitType>? bits)
    {
        if (bits is null)
            return type.BitSpan;

        ulong min = 0, max = 0;
        ulong v = 1;

        int i = 0;
        using var enumerator = bits.GetEnumerator();
        while (i < type.nbits)
        {
            if (!enumerator.MoveNext())
                throw new ArgumentException($"bits is shorter than expected: expected {type.nbits}, got {i}");

            var bit = enumerator.Current;
            switch (bit)
            {
                case ZERO:
                    break;
                case ONE:
                    min |= v;
                    max |= v;
                    break;
                default:
                    max |= v;
                    break;
            }

            v <<= 1;
            i++;
        }

        if (enumerator.MoveNext())
            throw new ArgumentException($"bits is longer than expected: expected {type.nbits}");

        return new BitSpan(min, max);
    }

    public override UnknownValueBase WithTag(string key, object? value) =>
        HasTag(key, value) ? this : new UnknownValueBitTracker(this, _bits) { _tags = add_tag(key, value) };

    public override UnknownValueBase WithVarID(int id) =>
        _var_id == id ? this : new UnknownValueBitTracker(type, id, _bits);

    public override UnknownTypedValue WithType(TypeDB.IntType newType) =>
        new UnknownValueBitTracker(newType, _var_id!.Value, _bits.ToList().GetRange(0, newType.nbits));

    public bool HasPrivateBits() => _bits.Any(b => b.IsPrivateBit());

    int count_dynamic_bits()
    {
        int anyBits = 0;
        HashSet<BitType> uniqueBits = new();
        foreach (var bit in _bits)
        {
            switch (bit)
            {
                case ZERO:
                case ONE:
                    continue; // const bits do not contribute to cardinality
                case ANY:
                    anyBits++;
                    break;
                default:
                    uniqueBits.Add(bit < 0 ? -bit : bit); // negated value of the same bit does not contribute to cardinality
                    break;
            }
        }
        return anyBits + uniqueBits.Count;
    }

    public override CardInfo Cardinality() => CardInfo.FromBits(count_dynamic_bits());

    public override bool TypedContains(long value)
    {
        Dictionary<BitType, bool> sharedValues = new();

        long v = value;
        for (int i = 0; i < _bits.Count; i++, v >>>= 1)
        {
            bool bitIsSet = (v & 1) != 0;
            var bit = _bits[i];

            switch (bit)
            {
                case ZERO:
                    if (bitIsSet)
                        return false;
                    break;

                case ONE:
                    if (!bitIsSet)
                        return false;
                    break;

                case ANY:
                    // unconstrained — always matches
                    break;

                default:
                    var abs = bit < 0 ? -bit : bit;

                    if (!sharedValues.TryGetValue(abs, out var expected))
                    {
                        expected = bitIsSet;
                        sharedValues[abs] = expected;
                    }

                    // for negative shared bits, the expected value is inverted
                    bool expectedMatch = bit < 0 ? !expected : expected;
                    if (bitIsSet != expectedMatch)
                        return false;

                    break;
            }
        }

        if (!type.signed)
            return (v == 0);
        else if (value >= 0 || type.nbits == 64)
            return (v == 0);
        else
            return v == (long)(ulong.MaxValue >> type.nbits); // if value is negative, expect that all extra leading bits are set
    }

    public override IEnumerable<long> Values()
    {
        // Identify dynamic bit positions and how many combinations we must generate
        List<int> dynamicBitPositions = new();
        Dictionary<BitType, int> sharedBitIndexes = new();
        int dynamicIndex = 0;

        for (int i = 0; i < _bits.Count; i++)
        {
            var bit = _bits[i];
            switch (bit)
            {
                case ZERO:
                case ONE:
                    break; // fixed, do nothing
                case ANY:
                    dynamicBitPositions.Add(i); // unconstrained
                    break;
                default:
                    var abs = bit < 0 ? -bit : bit;
                    if (!sharedBitIndexes.ContainsKey(abs))
                    {
                        sharedBitIndexes[abs] = dynamicIndex++;
                    }
                    break;
            }
        }

        // Map absolute BitType -> first index in _bits
        var bitIndices = new Dictionary<BitType, int>();
        for (int i = 0; i < _bits.Count; i++)
        {
            if (_bits[i].IsPrivateBit())
            {
                var absBit = _bits[i] < 0 ? -_bits[i] : _bits[i];
                if (!bitIndices.ContainsKey(absBit))
                    bitIndices[absBit] = i;
            }
        }

        var card = Cardinality();
        if (card > MAX_DISCRETE_CARDINALITY)
            throw new InvalidOperationException($"{this}.Values(): Too many values to enumerate.");

        ulong total = card.ulValue;
        for (ulong combo = 0; combo < total; combo++)
        {
            ulong result = 0;

            // Resolve shared bit values
            Dictionary<BitType, bool> sharedValues = new();
            foreach (var kv in sharedBitIndexes)
            {
                bool value = ((combo >> kv.Value) & 1) != 0;
                sharedValues[kv.Key] = value;
            }

            // Resolve value from _bits
            int anyIndex = 0;
            for (int i = 0; i < type.nbits; i++)
            {
                var bit = _bits[i];
                bool value;

                switch (bit)
                {
                    case ZERO:
                        value = false;
                        break;
                    case ONE:
                        value = true;
                        break;
                    case ANY:
                        value = ((combo >> (sharedBitIndexes.Count + anyIndex)) & 1) != 0;
                        anyIndex++;
                        break;
                    default:
                        var abs = bit < 0 ? -bit : bit;
                        value = sharedValues[abs];
                        if (bit < 0)
                            value = !value;
                        break;
                }

                if (value)
                    result |= 1UL << i; // Maintain arithmetic order: LSB is lowest bit index
            }

            yield return SignExtend((long)result);
        }
    }

    // returns new UnknownValueBitTracker with the bit at idx set to value
    public UnknownValueBitTracker SetBit(int idx, BitType value)
    {
        if (idx < 0 || idx >= type.nbits)
            throw new ArgumentOutOfRangeException($"Index {idx} out of range for {type.nbits} _bits.");

        return new UnknownValueBitTracker(this, _bits.Select((b, i) => i == idx ? value : b));
    }

    public override object TypedCast(TypeDB.IntType toType)
    {
        if (toType.nbits == type.nbits)
            return new UnknownValueBitTracker(toType!, _var_id!.Value, _bits);

        return this;
    }

    public override UnknownTypedValue Upcast(TypeDB.IntType toType)
    {
        var newBits = new List<BitType>(_bits);
        BitType b = (type.signed && toType.signed) ? _bits[type.nbits - 1] : ZERO;
        newBits.AddRange(Enumerable.Repeat(b, toType.nbits - newBits.Count));
        return new UnknownValueBitTracker(toType, _var_id!.Value, newBits);
    }

    public override bool Equals(object? obj) => (obj is UnknownValueBitTracker other) && type.Equals(other.type) && _var_id == other._var_id && _bits.SequenceEqual(other._bits);
    public override int GetHashCode() => base.GetHashCode(); // just to silent warning

    public override string ToString()
    {
        string result = $"UnknownValueBitTracker<{type}>[";
        int start = type.nbits - 1;

        if (type.nbits > 8 && start > 2 && _bits[start] == _bits[start - 1] && _bits[start] == _bits[start - 2])
        {
            result += "…";

            while (start > 0 && _bits[start] == _bits[start - 1])
                start--;
        }

        for (int i = start; i >= 0; i--)
        {
            BitType bit = _bits[i];
            result += bit switch
            {
                ANY => "_",
                ONE => "1",
                ZERO => "0",
                _ => bit > 0 ? BITMAP[(bit - 2) % BITMAP.Length] : BITMAP[(-bit - 2) % BITMAP.Length].ToString().ToUpperInvariant()
            };
        }

        result += "]";
        return result;
    }
}
