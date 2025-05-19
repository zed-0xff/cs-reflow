using Xunit;

public class UnknownValueTests
{
    [Fact]
    public void TestAdd()
    {
        UnknownValue a = new();
        UnknownValue b = new();

        Assert.True((a + b) is UnknownValue);
    }

    [Fact]
    public void TestRange_int()
    {
        UnknownValue a = new("int");
        Assert.NotNull(a.Range);
        Assert.Equal(int.MinValue, a.Range.Min);
        Assert.Equal(int.MaxValue, a.Range.Max);
    }

    [Fact]
    public void TestRange_int_div()
    {
        UnknownValue a = new("int");
        a /= 10;
        Assert.NotNull(a.Range);
        Assert.Equal(int.MinValue / 10, a.Range.Min);
        Assert.Equal(int.MaxValue / 10, a.Range.Max);
    }

    [Fact]
    public void TestRange_int_mod()
    {
        UnknownValue a = new("int");
        a %= 100;
        Assert.NotNull(a.Range);
        Assert.Equal(0, a.Range.Min);
        Assert.Equal(99, a.Range.Max);
    }

    [Fact]
    public void TestRange_int_mod_negative()
    {
        UnknownValue a = new("int");
        a %= -100;
        Assert.NotNull(a.Range);
        Assert.Equal(-99, a.Range.Min);
        Assert.Equal(0, a.Range.Max);
    }


    [Fact]
    public void TestRange_uint()
    {
        UnknownValue a = new("uint");
        Assert.NotNull(a.Range);
        Assert.Equal(uint.MinValue, a.Range.Min);
        Assert.Equal(uint.MaxValue, a.Range.Max);
    }

    [Fact]
    public void TestCast()
    {
        UnknownValue a = new("uint");
        UnknownValue b = (a / 0x100).Cast("int");
        Assert.Equal(0, b.Range.Min);
        Assert.Equal(16777215, b.Range.Max);
    }
}
