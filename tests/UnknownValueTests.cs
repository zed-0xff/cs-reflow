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

        var b = a.WithTag("foo", "bar");
        Assert.Equal("UnknownValue{foo=bar}", b.ToString());

        b = b.WithTag("x", null);
        Assert.Equal("UnknownValue{foo=bar}", b.ToString());

        b = b.WithTag("x", "y");
        Assert.Equal("UnknownValue{foo=bar, x=y}", b.ToString());

        var c = b.WithTag("foo", null);
        Assert.Equal("UnknownValue{x=y}", c.ToString());

        var d = c.WithoutTag("x");
        Assert.Equal("UnknownValue", d.ToString());
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

        TypeDB.IntType? intType = null;
        var c = UnknownValue.Create(intType);
        Assert.True(c is UnknownValue);
    }

    [Fact]
    public void Test_create_int()
    {
        var a = UnknownValue.Create(TypeDB.Int);
        Assert.True(a is UnknownValueRange);
        Assert.Equal("UnknownValue<int>", a.ToString());
    }

#pragma warning disable CS8602 // Dereference of a possibly null reference.

    [Fact]
    public void Test_expr()
    {
        var a = UnknownValue.Create(TypeDB.Int);
        a = a.Cast(TypeDB.UInt) as UnknownValueBase;
        a = a.Div(1024u);
        Assert.Equal("UnknownValue<uint>[0..4194303]", a.ToString());
        a = a.Cast(TypeDB.Int) as UnknownValueBase;
        a = a.Sub(67108864);
        Assert.Equal("UnknownValue<int>[-67108864..-62914561]", a.ToString());

        var b = UnknownValue.Create(TypeDB.Int);
        b = b.Cast(TypeDB.UInt) as UnknownValueBase;
        b = b.Mod(1949u);
        Assert.Equal("UnknownValue<uint>[0..1948]", b.ToString());
        b = b.Xor(0x70EF1C76);
        Assert.Equal("UnknownValueSet<long>[1949]", b.ToString());
        b = b.Mul(2048);
        Assert.Equal("UnknownValueSet<long>[1949]", b.ToString());
        b = b.Cast(TypeDB.Int) as UnknownValueBase;
        Assert.Equal("UnknownValueSet<int>[1949]", b.ToString());
        Assert.Equal(2025848832, b.Min());
        Assert.Equal(2030041088, b.Max());

        Assert.NotNull(a);
        Assert.Equal(false, a.Eq(b));
    }
}
