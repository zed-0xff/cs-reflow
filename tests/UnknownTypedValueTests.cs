using Xunit;

public class UnknownTypedValueTests
{
    [Fact]
    public void Test_Mul_byte_const()
    {
        var a = UnknownTypedValue.Create(TypeDB.Byte);
        Assert.Equal("UnknownValue<int>[0..255]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBits<int>[…0________0]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<int>[…0________00]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<int>[…0________0000000]", a.Mul(128).ToString());
        Assert.Equal("UnknownValueBits<int>[…0________00000000]", a.Mul(256).ToString());

        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(25).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(100).ToString());

        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(2 + 4).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(2 + 4 + 8 + 16).ToString());
    }

    [Fact]
    public void Test_Mul_byte_var_const()
    {
        var a = UnknownTypedValue.Create(TypeDB.Byte).WithVarID(0);
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Int), a.Mul(0));
        Assert.Equal("UnknownValue<int>[0..255]`0", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba0]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba00]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba0000000]", a.Mul(128).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba00000000]", a.Mul(256).ToString());

        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(25).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(100).ToString());

        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(2 + 4).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(2 + 4 + 8 + 16).ToString());
    }

    [Fact]
    public void Test_Mul_sbyte_var_const()
    {
        var a = UnknownTypedValue.Create(TypeDB.SByte).WithVarID(0);
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Int), a.Mul(0));
        Assert.Equal("UnknownValue<int>[-128..127]`0", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…hgfedcba0]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…hgfedcba00]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…hgfedcba0000000]", a.Mul(128).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…hgfedcba00000000]", a.Mul(256).ToString());

        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(25).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(100).ToString());

        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(2 + 4).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueSet<int>[256]", a.Mul(2 + 4 + 8 + 16).ToString());
    }

    [Fact]
    public void Test_Mul_int_const()
    {
        var a = UnknownTypedValue.Create(TypeDB.Int);
        Assert.Equal(a, a.Mul(1));
        Assert.Equal("UnknownValueBits<int>[…_0]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<int>[…_00]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<int>[…_0000000]", a.Mul(128).ToString());
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Long), a.Mul(0x10).Mul(0x1000_0000_0000_0000L));

        Assert.Equal(a, a.Mul(3));
        Assert.Equal(a, a.Mul(25));
        Assert.Equal("UnknownValueBits<int>[…_00]", a.Mul(100).ToString());

        Assert.Equal("UnknownValueBits<int>[…_0]", a.Mul(2 + 4).ToString());
        Assert.Equal("UnknownValueBits<int>[…_0]", a.Mul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBits<int>[…_0]", a.Mul(2 + 4 + 8 + 16).ToString());

        Assert.Equal("UnknownValueBits<long>[…_000000000000000000000]", a.Mul(0xB5E00000).ToString());
        Assert.Equal("UnknownValueBits<int>[…_000000000000000000000]", a.Mul(-1243611136).ToString());
    }

    [Fact]
    public void Test_var_Mul_int_const()
    {
        var a = UnknownTypedValue.Create(TypeDB.Int).WithVarID(0);
        Assert.Equal(a, a.Mul(1));
        Assert.Equal("UnknownValueBitTracker<int>[дгвбаzyxwvutsrqponmlkjihgfedcba0]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[гвбаzyxwvutsrqponmlkjihgfedcba00]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[ba000000000000000000000000000000]", a.Mul(0x40000000).ToString());
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Long), a.Mul(0x100).Mul(0x1000_0000_0000_0000L));

        Assert.Equal("UnknownValueBitTracker<int>[…_a]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…_cba]", a.Mul(25).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…_cba00]", a.Mul(100).ToString());

        Assert.Equal("UnknownValueBitTracker<int>[…_a0]", a.Mul(2 + 4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…_a0]", a.Mul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…_a0]", a.Mul(2 + 4 + 8 + 16).ToString());

        Assert.Equal("UnknownValueBitTracker<long>[…_a000000000000000000000]", a.Mul(0xB5E00000).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…_a000000000000000000000]", a.Mul(-1243611136).ToString());
    }

    [Fact]
    public void Test_long_And()
    {
        var a = UnknownTypedValue.Create(TypeDB.Long);
        var b = a.BitwiseAnd(1);
        Assert.Equal("UnknownValueBits<long>[…0_]", b.ToString());
    }
}
