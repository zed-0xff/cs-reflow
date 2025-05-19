using Xunit;

public class UnknownValueRangeTests
{
    [Fact]
    public void Test_ToString()
    {
        UnknownValueRange a = new("uint");
        Assert.Equal("UnknownValue<uint>[0..4294967295]", a.ToString());

        UnknownValueRange b = new("int");
        Assert.Equal("UnknownValue<int>[−2147483648..2147483647]", b.ToString());
    }

    [Fact]
    public void TestRange_int()
    {
        UnknownValueRange a = new("int");
        Assert.NotNull(a.Range);
        Assert.Equal(int.MinValue, a.Range.Min);
        Assert.Equal(int.MaxValue, a.Range.Max);
    }

    [Fact]
    public void TestRange_int_div()
    {
        UnknownValueRange a = new("int");
        a /= 10;
        Assert.NotNull(a.Range);
        Assert.Equal(int.MinValue / 10, a.Range.Min);
        Assert.Equal(int.MaxValue / 10, a.Range.Max);
    }

    [Fact]
    public void TestRange_int_mod()
    {
        UnknownValueRange a = new("int");
        a %= 100;
        Assert.NotNull(a.Range);
        Assert.Equal(0, a.Range.Min);
        Assert.Equal(99, a.Range.Max);
    }

    [Fact]
    public void TestRange_int_mod_negative()
    {
        UnknownValueRange a = new("int");
        a %= -100;
        Assert.NotNull(a.Range);
        Assert.Equal(-99, a.Range.Min);
        Assert.Equal(0, a.Range.Max);
    }


    [Fact]
    public void TestRange_uint()
    {
        UnknownValueRange a = new("uint");
        Assert.NotNull(a.Range);
        Assert.Equal(uint.MinValue, a.Range.Min);
        Assert.Equal(uint.MaxValue, a.Range.Max);
    }

    [Fact]
    public void TestCast()
    {
        UnknownValueRange a = new("uint");
        UnknownValueRange b = (a / 0x100).Cast("int");
        Assert.Equal(0, b.Range.Min);
        Assert.Equal(16777215, b.Range.Max);
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
}
