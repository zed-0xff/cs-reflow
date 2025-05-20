using Xunit;

public class UnknownValueTests
{
    [Fact]
    public void Test_Add()
    {
        UnknownValue a = new();
        UnknownValue b = new();

        Assert.True(a.Add(b) is UnknownValue);
    }

    [Fact]
    public void Test_ToString()
    {
        UnknownValue a = new();
        Assert.Equal("UnknownValue", a.ToString());
    }

    [Fact]
    public void Test_expr()
    {
        var a = UnknownValue.Create("int");
        a = a.Cast("uint");
        a = a.Div(1024u);
        Assert.Equal("UnknownValue<uint>[0..4194303]", a.ToString());
        a = a.Cast("int");
        a = a.Sub(67108864);
        Assert.Equal("UnknownValue<int>[−67108864..−62914561]", a.ToString());

        var b = UnknownValue.Create("int");
        b = b.Cast("uint");
        b = b.Mod(1949u);
        Assert.Equal("UnknownValue<uint>[0..1948]", b.ToString());
        b = b.Xor(0x70EF1C76);
        Assert.Equal("UnknownValue<uint>[1949]", b.ToString());
        b = b.Mul(2048);
        Assert.Equal("UnknownValue<uint>[1949]", b.ToString());
        b = b.Cast("int");
        Assert.Equal("UnknownValue<int>[1949]", b.ToString());
        Assert.Equal(2025848832, b.Min());
        Assert.Equal(2030041088, b.Max());

        Assert.Equal(false, a.Eq(b));
    }
}
