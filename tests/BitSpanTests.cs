using Xunit;

public class BitSpanTests
{
    [Fact]
    public void Test_Cardinality()
    {
        Assert.Equal(1UL, new BitSpan().Cardinality().ulValue);
        Assert.Equal(2UL, new BitSpan(0, 1).Cardinality().ulValue);
        Assert.Equal(2UL, new BitSpan(0, 2).Cardinality().ulValue);
        Assert.Equal(4UL, new BitSpan(0b10000, 0b10011).Cardinality().ulValue);
        Assert.Equal(4UL, new BitSpan(0b10000, 0b10101).Cardinality().ulValue);
        Assert.Equal(4UL, new BitSpan(0b10000, 0b11001).Cardinality().ulValue);
        Assert.Equal(256UL, new BitSpan(0, 255).Cardinality().ulValue);
    }

    [Fact]
    public void Test_SignedShiftRight()
    {
        var a = new BitSpan(0b00000010, 0b00000110);
        Assert.Equal(new BitSpan(0b00000001, 0b00000011), a.SignedShiftRight(1, 8));
        Assert.Equal(new BitSpan(0b00000000, 0b00000001), a.SignedShiftRight(2, 8));

        a = new BitSpan(0b00000010, 0b10000110);
        Assert.Equal(new BitSpan(0b00000001, 0b11000011), a.SignedShiftRight(1, 8));
        Assert.Equal(new BitSpan(0b00000000, 0b11100001), a.SignedShiftRight(2, 8));
    }
}
