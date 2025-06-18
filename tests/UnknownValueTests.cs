using System.Globalization;
using Xunit;

public class UnknownValueTests
{
    static UnknownValueTests()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture; // do not print unicode '−' for negative numbers
    }

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
    public void Test_create_null()
    {
        string? str = null;
        var a = UnknownValue.Create(str!);
        Assert.True(a is UnknownValue);

        Type? type = null;
        var b = UnknownValue.Create(type);
        Assert.True(b is UnknownValue);
    }

    [Fact]
    public void Test_create_int()
    {
        var a = UnknownValue.Create(TypeDB.Int);
        Assert.True(a is UnknownValueRange);
        Assert.Equal("UnknownValue<int>", a.ToString());
    }

    [Fact]
    public void Test_expr()
    {
        var a = UnknownValue.Create(TypeDB.Int);
        a = a.Cast(TypeDB.UInt) as UnknownValueBase;
        a = a?.Div(1024u);
        Assert.Equal("UnknownValue<uint>[0..4194303]", a?.ToString());
        a = a?.Cast(TypeDB.Int) as UnknownValueBase;
        a = a?.Sub(67108864);
        Assert.Equal("UnknownValue<int>[-67108864..-62914561]", a?.ToString());

        var b = UnknownValue.Create(TypeDB.Int);
        b = b.Cast(TypeDB.UInt) as UnknownValueBase;
        b = b?.Mod(1949u);
        Assert.Equal("UnknownValue<uint>[0..1948]", b?.ToString());
        b = b?.Xor(0x70EF1C76);
        Assert.Equal("UnknownValueList<uint>[1949]", b?.ToString());
        b = b?.Mul(2048);
        Assert.Equal("UnknownValueList<uint>[1949]", b?.ToString());
        b = b?.Cast(TypeDB.Int) as UnknownValueBase;
        Assert.Equal("UnknownValueList<int>[1949]", b?.ToString());
        Assert.Equal(2025848832, b?.Min());
        Assert.Equal(2030041088, b?.Max());

        Assert.NotNull(a);
        Assert.Equal(false, a.Eq(b!));
    }
}
