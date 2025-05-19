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
}
