using Xunit;

public class UnknownValueTests
{
    [Fact]
    public void Test_Add()
    {
        UnknownValue a = new();
        UnknownValue b = new();

        Assert.True((a + b) is UnknownValue);
    }

    [Fact]
    public void Test_ToString()
    {
        UnknownValue a = new();
        Assert.Equal("UnknownValue<>", a.ToString());
    }

    [Fact]
    public void Test_expr()
    {
        UnknownValue a = UnknownValue.Create("int");
        a = a.Cast("uint");
        a = a.Mod(1949u);
        Assert.Equal("UnknownValue<uint>[0..1948]", a.ToString());
        //        a = a.Xor(0x70EF1C76);
    }
}
