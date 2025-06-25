using Xunit;

public class LongRangeTests
{
    [Fact]
    public void Test_new()
    {
        LongRange a = new LongRange(1, 1000);
        Assert.Equal(1, a.Min);
        Assert.Equal(1000, a.Max);
    }

    [Fact]
    public void Test_new_fail()
    {
        Assert.Throws<ArgumentException>(() => new LongRange(1, -1));
    }

    [Fact]
    public void Test_new_nbits()
    {
        LongRange a = new LongRange(1, false);
        Assert.Equal(0, a.Min);
        Assert.Equal(1, a.Max);

        a = new LongRange(8, false);
        Assert.Equal(byte.MinValue, a.Min);
        Assert.Equal(byte.MaxValue, a.Max);

        a = new LongRange(8, true);
        Assert.Equal(sbyte.MinValue, a.Min);
        Assert.Equal(sbyte.MaxValue, a.Max);

        a = new LongRange(32, false);
        Assert.Equal(uint.MinValue, a.Min);
        Assert.Equal(uint.MaxValue, a.Max);

        a = new LongRange(32, true);
        Assert.Equal(int.MinValue, a.Min);
        Assert.Equal(int.MaxValue, a.Max);
    }

    [Fact]
    public void Test_ToString()
    {
        LongRange a = new LongRange(1, 1000);
        Assert.Equal("[1..1000]", a.ToString());
    }

    [Fact]
    public void Test_div()
    {
        LongRange a = new LongRange(100, 1000) / 10;
        Assert.Equal(10, a.Min);
        Assert.Equal(100, a.Max);
    }

    [Fact]
    public void Test_mul()
    {
        LongRange a = new LongRange(100, 1000) * 2;
        Assert.Equal(200, a.Min);
        Assert.Equal(2000, a.Max);
    }

    [Fact]
    public void Test_add()
    {
        LongRange a = new LongRange(100, 1000) + 10;
        Assert.Equal(110, a.Min);
        Assert.Equal(1010, a.Max);
    }

    [Fact]
    public void Test_sub()
    {
        LongRange a = new LongRange(10, 1000) - 100;
        Assert.Equal(-90, a.Min);
        Assert.Equal(900, a.Max);
    }

    [Fact]
    public void Test_IntersectsWith()
    {
        LongRange a = new LongRange(1, 1000);
        LongRange b = new LongRange(500, 1500);
        Assert.True(a.IntersectsWith(b));

        b = new LongRange(1001, 2000);
        Assert.False(a.IntersectsWith(b));
    }

    [Fact]
    public void Test_BitSpan()
    {
        LongRange a = new LongRange(100, 200);
        var (min, max) = a.BitSpan();
        Assert.Equal(0, min);
        Assert.Equal(255, max);

        a = new LongRange(0b00001111, 0b11110000);
        (min, max) = a.BitSpan();
        Assert.Equal(0b00000000, min);
        Assert.Equal(0b11111111, max);

        a = new LongRange(0xff, 0x1ff);
        Assert.Equal((0, 0x1ff), a.BitSpan());

        a = new LongRange(0x100, 0x1ff);
        Assert.Equal((0x100, 0x1ff), a.BitSpan());
    }
}
