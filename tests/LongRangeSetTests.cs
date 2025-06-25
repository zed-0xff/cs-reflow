using Xunit;

public class LongRangeSetTests
{
    [Fact]
    public void Test_BitSpan()
    {
        var set = new LongRangeSet(new[] {
            new LongRange(0x100, 0x1ff),
        });
        Assert.Equal((0x100, 0x1ff), set.BitSpan());

        set = new LongRangeSet(new[] {
            new LongRange(0x00100, 0x001ff),
            new LongRange(0x10100, 0x1010f),
        });
        Assert.Equal((0x00100, 0x101ff), set.BitSpan());
    }
}
