using Xunit;

public class UnknownValueRangeTests
{
    [Fact]
    public void Test_ToString()
    {
        UnknownValueRange a = new(TypeDB.UInt);
        Assert.Equal("UnknownValue<uint>", a.ToString());
        Assert.Equal("UnknownValue<uint>", $"{a}");

        a = a.Div(0x100);
        Assert.Equal("UnknownValue<uint>[0..16777215]", a.ToString());
        Assert.Equal("UnknownValue<uint>[0..16777215]", $"{a}");

        UnknownValueRange b = new(TypeDB.Int);
        Assert.Equal("UnknownValue<int>", b.ToString());
        Assert.Equal("UnknownValue<int>", $"{b}");
    }

    [Fact]
    public void Test_mul_uv()
    {
        UnknownValueRange a = new(TypeDB.UInt, 0, 1194);
        UnknownValueBits b = new(TypeDB.Int, 0, 0x3ffffff);
        Assert.Equal(76480, a.Sub(b).Cardinality());
    }

    [Fact]
    public void Test_int()
    {
        UnknownValueRange a = new(TypeDB.Int);
        Assert.NotNull(a.Range);
        Assert.Equal(int.MinValue, a.Range.Min);
        Assert.Equal(int.MaxValue, a.Range.Max);
    }

    [Fact]
    public void Test_int_div()
    {
        UnknownValueRange a = new(TypeDB.Int);
        a = a.Div(10);
        Assert.NotNull(a.Range);
        Assert.Equal(int.MinValue / 10, a.Range.Min);
        Assert.Equal(int.MaxValue / 10, a.Range.Max);
    }

    [Fact]
    public void Test_int_mod()
    {
        UnknownValueRange a = new(TypeDB.Int);
        a = a.Mod(100);
        Assert.NotNull(a.Range);
        Assert.Equal(0, a.Range.Min);
        Assert.Equal(99, a.Range.Max);
    }

    [Fact]
    public void Test_int_mod_negative()
    {
        UnknownValueRange a = new(TypeDB.Int);
        a = a.Mod(-100);
        Assert.NotNull(a.Range);
        Assert.Equal(-99, a.Range.Min);
        Assert.Equal(0, a.Range.Max);
    }

    [Fact]
    public void Test_int_xor()
    {
        UnknownValueRange a = new(TypeDB.Int, new LongRange(1, 5));
        var b = a.Xor(0x10);
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 0x11, 0x12, 0x13, 0x14, 0x15 }, values);
    }

    [Fact]
    public void Test_int_add()
    {
        UnknownValueRange a = new(TypeDB.Int, new LongRange(1, 5));
        var b = a.Add(0x10);
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 0x11, 0x12, 0x13, 0x14, 0x15 }, values);
    }

    [Fact]
    public void Test_int_sub()
    {
        UnknownValueRange a = new(TypeDB.Int, new LongRange(10, 15));
        var b = a.Sub(10);
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 0, 1, 2, 3, 4, 5 }, values);
    }

    [Fact]
    public void Test_int_mul_int()
    {
        UnknownValueRange a = new(TypeDB.Int, new LongRange(1, 3));
        var b = a.Mul(5);
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 5, 10, 15 }, values);
    }

    [Fact]
    public void Test_int_mul_unk()
    {
        UnknownValueRange a = new(TypeDB.Int, new LongRange(1, 3));
        UnknownValueRange b = new(TypeDB.Int, new LongRange(5, 7));
        var r = a.Mul(b);
        List<long> values = r.Values().ToList();
        Assert.Equal(new List<long> { 5, 6, 7, 10, 12, 14, 15, 18, 21 }, values);
    }

    [Fact]
    public void Test_int_mul_zero()
    {
        UnknownValueRange a = new(TypeDB.Int);
        Assert.Equal(new UnknownValueList(TypeDB.Int, new List<long> { 0 }), a.Mul(0));
    }

    [Fact]
    public void Test_int_mul_one()
    {
        UnknownValueRange a = new(TypeDB.Int);
        Assert.Equal(a, a.Mul(1));
    }

    [Fact]
    public void Test_int_mul_pow2()
    {
        UnknownValueRange a = new(TypeDB.Int);
        Assert.Equal("UnknownValueBits<int>[0]", a.Mul(2).ToString());
        Assert.Equal(a, a.Mul(3));
        Assert.Equal("UnknownValueBits<int>[00]", a.Mul(4).ToString());
        Assert.Equal(a, a.Mul(5));
        Assert.Equal("UnknownValueBits<int>[0]", a.Mul(6).ToString());
        Assert.Equal(a, a.Mul(7));
        Assert.Equal("UnknownValueBits<int>[000]", a.Mul(8).ToString());
        Assert.Equal(a, a.Mul(0x0f));
        Assert.Equal("UnknownValueBits<int>[0000]", a.Mul(0x10).ToString());
        Assert.Equal(a, a.Mul(0x11));
        Assert.Equal("UnknownValueBits<int>[0000]", a.Mul(0xab0).ToString());
    }

    [Fact]
    public void Test_int_lt()
    {
        UnknownValueRange a = new(TypeDB.Int);
        Assert.Equal(new UnknownValueRange(TypeDB.Bool), a.Lt(0));

        Assert.Equal(new UnknownValueRange(TypeDB.Bool), a.Lt(int.MaxValue));
        Assert.Equal(true, a.Lt(int.MaxValue + 1L));

        Assert.Equal(new UnknownValueRange(TypeDB.Bool), a.Lt(int.MinValue));
        Assert.Equal(false, a.Lt(int.MinValue - 1L));
    }

    [Fact]
    public void Test_int_gt()
    {
        UnknownValueRange a = new(TypeDB.Int);
        Assert.Equal(new UnknownValueRange(TypeDB.Bool), a.Gt(0));

        Assert.Equal(new UnknownValueRange(TypeDB.Bool), a.Gt(int.MaxValue));
        Assert.Equal(false, a.Gt(int.MaxValue + 1L));

        Assert.Equal(new UnknownValueRange(TypeDB.Bool), a.Gt(int.MinValue));
        Assert.Equal(true, a.Gt(int.MinValue - 1L));
    }

    [Fact]
    public void Test_uint()
    {
        UnknownValueRange a = new(TypeDB.UInt);
        Assert.NotNull(a.Range);
        Assert.Equal(uint.MinValue, a.Range.Min);
        Assert.Equal(uint.MaxValue, a.Range.Max);
    }

    [Fact]
    public void Test_Cast()
    {
        UnknownValueRange a = new(TypeDB.UInt);
        UnknownValueRange? b = a.Div(0x100).Cast(TypeDB.Int) as UnknownValueRange;
        Assert.Equal(new LongRange(0, 16777215), b?.Range);
    }

    [Fact]
    public void Test_Cast_int2uint()
    {
        var src = TypeDB.Int;
        var dst = TypeDB.UInt;

        // trivial if values are within 0..0x7fff_ffff (int.MaxValue)
        Assert.Equal(new LongRange(0, 100), (new UnknownValueRange(src, new(0, 100)).Cast(dst) as UnknownValueRange)?.Range);
        Assert.Equal(new LongRange(200, 300), (new UnknownValueRange(src, new(200, 300)).Cast(dst) as UnknownValueRange)?.Range);

        // trivial
        Assert.Equal(new LongRange(4294967096, 4294967196), (new UnknownValueRange(src, new(-200, -100)).Cast(dst) as UnknownValueRange)?.Range);

        // [0, 1, 4294967295]
        Assert.Equal(new LongRange(uint.MinValue, uint.MaxValue), (new UnknownValueRange(src, new(-1, 1)).Cast(dst) as UnknownValueRange)?.Range);

        // [4294967196..uint.MaxValue, 0..100]
        Assert.Equal(new LongRange(uint.MinValue, uint.MaxValue), (new UnknownValueRange(src, new(-100, 100)).Cast(dst) as UnknownValueRange)?.Range);
    }

    [Fact]
    public void Test_Cast_uint2int()
    {
        var src = TypeDB.UInt;
        var dst = TypeDB.Int;

        // trivial if values are within 0..0x7fff_ffff (int.MaxValue)
        Assert.Equal(new LongRange(0, 100), (new UnknownValueRange(src, new(0, 100)).Cast(dst) as UnknownValueRange)?.Range);
        Assert.Equal(new LongRange(200, 300), (new UnknownValueRange(src, new(200, 300)).Cast(dst) as UnknownValueRange)?.Range);

        // trivial
        Assert.Equal(new LongRange(-200, -100), (new UnknownValueRange(src, new(4294967096, 4294967196)).Cast(dst) as UnknownValueRange)?.Range);

        // [0, 1, 4294967295]
        Assert.Equal(new LongRange(int.MinValue, int.MaxValue), (new UnknownValueRange(src, new(int.MaxValue, int.MaxValue + 1L)).Cast(dst) as UnknownValueRange)?.Range);

        // [4294967196..uint.MaxValue, 0..100]
        Assert.Equal(new LongRange(int.MinValue, int.MaxValue), (new UnknownValueRange(src, new(int.MaxValue - 100L, int.MaxValue + 100L)).Cast(dst) as UnknownValueRange)?.Range);
    }

    [Fact]
    public void Test_Cast_bool()
    {
        UnknownValueRange a = new(TypeDB.Int, new(1, 3));
        var b = a.Cast(TypeDB.Bool);
        Assert.True(b is bool);
        Assert.True(b as bool?);

        a = new(TypeDB.Int, new(0, 0));
        b = a.Cast(TypeDB.Bool);
        Assert.True(b is bool);
        Assert.False(b as bool?);

        a = new(TypeDB.Int, new(0, 5));
        Assert.Equal(UnknownValue.Create(TypeDB.Bool), a.Cast(TypeDB.Bool));
    }

    [Fact]
    public void Test_Cardinality()
    {
        UnknownValueRange a = new(TypeDB.UInt);
        Assert.Equal(4294967296L, a.Cardinality());

        UnknownValueRange b = new(TypeDB.Int);
        Assert.Equal(4294967296L, b.Cardinality());

        UnknownValueRange c = new(TypeDB.Byte);
        Assert.Equal(256L, c.Cardinality());
    }

    [Fact]
    public void Test_ShiftLeft_uint()
    {
        UnknownValueRange a = new(TypeDB.UInt);
        var b = a.ShiftLeft(1);
        Assert.Equal("UnknownValueBits<uint>[0]", b.ToString());

        b = a.ShiftLeft(30);
        Assert.Equal(4, b.Cardinality());
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 0, 1L << 30, 2L << 30, 3L << 30 }, values);
    }

    [Fact]
    public void Test_ShiftLeft_int()
    {
        UnknownValueRange a = new(TypeDB.Int);
        var b = a.ShiftLeft(1);
        Assert.Equal("UnknownValueBits<int>[0]", b.ToString());

        b = a.ShiftLeft(30);
        Assert.Equal(4, b.Cardinality());
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 2 << 30, 3 << 30, 0, 1073741824 }, values);
    }

    [Fact]
    public void Test_ShiftLeft_sbyte()
    {
        UnknownValueRange a = new(TypeDB.SByte);
        var b = a.ShiftLeft(5);
        Assert.Equal(8, b.Cardinality());
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { -128, -96, -64, -32, 0, 32, 64, 96 }, values);
    }

    [Fact]
    public void Test_Negate_int()
    {
        UnknownValueRange a = new(TypeDB.Int, -50, 100);
        var b = a.Negate();
        Assert.NotEqual(a, b);
        Assert.Equal(new UnknownValueRange(TypeDB.Int, -100, 50), b);
        var c = b.Negate();
        Assert.Equal(a, c);
    }

    [Fact]
    public void Test_BitwiseNot_int()
    {
        UnknownValueRange a = new(TypeDB.Int, -50, 100);
        var b = a.BitwiseNot();
        Assert.NotEqual(a, b);
        Assert.Equal(new UnknownValueRange(TypeDB.Int, -101, 49), b);
        var c = b.BitwiseNot();
        Assert.Equal(a, c);
    }
}
