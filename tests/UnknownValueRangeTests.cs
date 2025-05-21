using Xunit;

public class UnknownValueRangeTests
{
    [Fact]
    public void Test_ToString()
    {
        UnknownValueRange a = new("uint");
        Assert.Equal("UnknownValue<uint>", a.ToString());
        Assert.Equal("UnknownValue<uint>", $"{a}");

        a = a.Div(0x100);
        Assert.Equal("UnknownValue<uint>[0..16777215]", a.ToString());
        Assert.Equal("UnknownValue<uint>[0..16777215]", $"{a}");

        UnknownValueRange b = new("int");
        Assert.Equal("UnknownValue<int>", b.ToString());
        Assert.Equal("UnknownValue<int>", $"{b}");
    }

    [Fact]
    public void Test_int()
    {
        UnknownValueRange a = new("int");
        Assert.NotNull(a.Range);
        Assert.Equal(int.MinValue, a.Range.Min);
        Assert.Equal(int.MaxValue, a.Range.Max);
    }

    [Fact]
    public void Test_int_div()
    {
        UnknownValueRange a = new("int");
        a = a.Div(10);
        Assert.NotNull(a.Range);
        Assert.Equal(int.MinValue / 10, a.Range.Min);
        Assert.Equal(int.MaxValue / 10, a.Range.Max);
    }

    [Fact]
    public void Test_int_mod()
    {
        UnknownValueRange a = new("int");
        a = a.Mod(100);
        Assert.NotNull(a.Range);
        Assert.Equal(0, a.Range.Min);
        Assert.Equal(99, a.Range.Max);
    }

    [Fact]
    public void Test_int_mod_negative()
    {
        UnknownValueRange a = new("int");
        a = a.Mod(-100);
        Assert.NotNull(a.Range);
        Assert.Equal(-99, a.Range.Min);
        Assert.Equal(0, a.Range.Max);
    }

    [Fact]
    public void Test_int_xor()
    {
        UnknownValueRange a = new("int", new LongRange(1, 5));
        var b = a.Xor(0x10);
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 0x11, 0x12, 0x13, 0x14, 0x15 }, values);
    }

    [Fact]
    public void Test_int_add()
    {
        UnknownValueRange a = new("int", new LongRange(1, 5));
        var b = a.Add(0x10);
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 0x11, 0x12, 0x13, 0x14, 0x15 }, values);
    }

    [Fact]
    public void Test_int_sub()
    {
        UnknownValueRange a = new("int", new LongRange(10, 15));
        var b = a.Sub(10);
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 0, 1, 2, 3, 4, 5 }, values);
    }

    [Fact]
    public void Test_int_mul_int()
    {
        UnknownValueRange a = new("int", new LongRange(1, 3));
        var b = a.Mul(5);
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 5, 10, 15 }, values);
    }

    [Fact]
    public void Test_int_mul_unk()
    {
        UnknownValueRange a = new("int", new LongRange(1, 3));
        UnknownValueRange b = new("int", new LongRange(5, 7));
        var r = a.Mul(b);
        List<long> values = r.Values().ToList();
        Assert.Equal(new List<long> { 5, 6, 7, 10, 12, 14, 15, 18, 21 }, values);
    }

    [Fact]
    public void Test_int_lt()
    {
        UnknownValueRange a = new("int");
        Assert.Equal(new UnknownValueRange("bool"), a.Lt(0));

        Assert.Equal(new UnknownValueRange("bool"), a.Lt(int.MaxValue));
        Assert.Equal(true, a.Lt(int.MaxValue + 1L));

        Assert.Equal(new UnknownValueRange("bool"), a.Lt(int.MinValue));
        Assert.Equal(false, a.Lt(int.MinValue - 1L));
    }

    [Fact]
    public void Test_int_gt()
    {
        UnknownValueRange a = new("int");
        Assert.Equal(new UnknownValueRange("bool"), a.Gt(0));

        Assert.Equal(new UnknownValueRange("bool"), a.Gt(int.MaxValue));
        Assert.Equal(false, a.Gt(int.MaxValue + 1L));

        Assert.Equal(new UnknownValueRange("bool"), a.Gt(int.MinValue));
        Assert.Equal(true, a.Gt(int.MinValue - 1L));
    }

    [Fact]
    public void Test_uint()
    {
        UnknownValueRange a = new("uint");
        Assert.NotNull(a.Range);
        Assert.Equal(uint.MinValue, a.Range.Min);
        Assert.Equal(uint.MaxValue, a.Range.Max);
    }

    [Fact]
    public void Test_Cast()
    {
        UnknownValueRange a = new("uint");
        UnknownValueRange b = a.Div(0x100).Cast("int") as UnknownValueRange;
        Assert.Equal(new LongRange(0, 16777215), b.Range);
    }

    [Fact]
    public void Test_Cast_int2uint()
    {
        string src = "int";
        string dst = "uint";

        // trivial if values are within 0..0x7fff_ffff (int.MaxValue)
        Assert.Equal(new LongRange(0, 100), (new UnknownValueRange(src, 0, 100).Cast(dst) as UnknownValueRange).Range);
        Assert.Equal(new LongRange(200, 300), (new UnknownValueRange(src, 200, 300).Cast(dst) as UnknownValueRange).Range);

        // trivial
        Assert.Equal(new LongRange(4294967096, 4294967196), (new UnknownValueRange(src, -200, -100).Cast(dst) as UnknownValueRange).Range);

        // [0, 1, 4294967295]
        Assert.Equal(new LongRange(uint.MinValue, uint.MaxValue), (new UnknownValueRange(src, -1, 1).Cast(dst) as UnknownValueRange).Range);

        // [4294967196..uint.MaxValue, 0..100]
        Assert.Equal(new LongRange(uint.MinValue, uint.MaxValue), (new UnknownValueRange(src, -100, 100).Cast(dst) as UnknownValueRange).Range);
    }

    [Fact]
    public void Test_Cast_uint2int()
    {
        string src = "uint";
        string dst = "int";

        // trivial if values are within 0..0x7fff_ffff (int.MaxValue)
        Assert.Equal(new LongRange(0, 100), (new UnknownValueRange(src, 0, 100).Cast(dst) as UnknownValueRange).Range);
        Assert.Equal(new LongRange(200, 300), (new UnknownValueRange(src, 200, 300).Cast(dst) as UnknownValueRange).Range);

        // trivial
        Assert.Equal(new LongRange(-200, -100), (new UnknownValueRange(src, 4294967096, 4294967196).Cast(dst) as UnknownValueRange).Range);

        // [0, 1, 4294967295]
        Assert.Equal(new LongRange(int.MinValue, int.MaxValue), (new UnknownValueRange(src, int.MaxValue, int.MaxValue + 1L).Cast(dst) as UnknownValueRange).Range);

        // [4294967196..uint.MaxValue, 0..100]
        Assert.Equal(new LongRange(int.MinValue, int.MaxValue), (new UnknownValueRange(src, int.MaxValue - 100L, int.MaxValue + 100L).Cast(dst) as UnknownValueRange).Range);
    }

    [Fact]
    public void Test_Cast_bool()
    {
        UnknownValueRange a = new("int", 1, 3);
        var b = a.Cast("bool");
        Assert.True(b is bool);
        Assert.True(b as bool?);

        a = new("int", 0, 0);
        b = a.Cast("bool");
        Assert.True(b is bool);
        Assert.False(b as bool?);

        a = new("int", 0, 5);
        Assert.Equal(UnknownValue.Create("bool"), a.Cast("bool"));
    }

    [Fact]
    public void Test_Cardinality()
    {
        UnknownValueRange a = new("uint");
        Assert.Equal(4294967296UL, a.Cardinality());

        UnknownValueRange b = new("int");
        Assert.Equal(4294967296UL, b.Cardinality());

        UnknownValueRange c = new("byte");
        Assert.Equal(256UL, c.Cardinality());
    }

    [Fact]
    public void Test_ShiftLeft_uint()
    {
        UnknownValueRange a = new("uint");
        var b = a.ShiftLeft(1);
        Assert.Equal(a, b);

        b = a.ShiftLeft(30);
        Assert.Equal(4UL, b.Cardinality());
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 0, 1L << 30, 2L << 30, 3L << 30 }, values);
    }

    [Fact]
    public void Test_ShiftLeft_int()
    {
        UnknownValueRange a = new("int");
        var b = a.ShiftLeft(1);
        Assert.Equal(a, b);

        b = a.ShiftLeft(30);
        Assert.Equal(4UL, b.Cardinality());
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { 2 << 30, 3 << 30, 0, 1073741824 }, values);
    }

    [Fact]
    public void Test_ShiftLeft_sbyte()
    {
        UnknownValueRange a = new("sbyte");
        var b = a.ShiftLeft(5);
        Assert.Equal(8UL, b.Cardinality());
        List<long> values = b.Values().ToList();
        Assert.Equal(new List<long> { -128, -96, -64, -32, 0, 32, 64, 96 }, values);
    }
}
