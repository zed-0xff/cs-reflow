using Xunit;

public class UnknownTypedValueTests
{
    [Fact]
    public void Test_Mul_byte_const()
    {
        var a = UnknownTypedValue.Create(TypeDB.Byte);
        Assert.Equal(a, a.Mul(1));
        Assert.Equal("UnknownValueSet<byte>[128]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueSet<byte>[64]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueSet<byte>{0, 128}", a.Mul(128).ToString());
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Byte), a.Mul(256));

        Assert.Equal(a, a.Mul(3));
        Assert.Equal(a, a.Mul(25));
        Assert.Equal("UnknownValueBits<byte>[…00]", a.Mul(100).ToString());

        Assert.Equal("UnknownValueBits<byte>[…0]", a.Mul(2 + 4).ToString());
        Assert.Equal("UnknownValueBits<byte>[…0]", a.Mul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBits<byte>[…0]", a.Mul(2 + 4 + 8 + 16).ToString());
    }

    [Fact]
    public void Test_Mul_int_const()
    {
        var a = UnknownTypedValue.Create(TypeDB.Int);
        Assert.Equal(a, a.Mul(1));
        Assert.Equal("UnknownValueBits<int>[…0]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<int>[…00]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<int>[…0000000]", a.Mul(128).ToString());
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Int), a.Mul(0x100000000L));

        Assert.Equal(a, a.Mul(3));
        Assert.Equal(a, a.Mul(25));
        Assert.Equal("UnknownValueBits<int>[…00]", a.Mul(100).ToString());

        Assert.Equal("UnknownValueBits<int>[…0]", a.Mul(2 + 4).ToString());
        Assert.Equal("UnknownValueBits<int>[…0]", a.Mul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBits<int>[…0]", a.Mul(2 + 4 + 8 + 16).ToString());

        Assert.Equal("UnknownValueBits<int>[…000000000000000000000]", a.Mul(0xB5E00000).ToString());
        Assert.Equal("UnknownValueBits<int>[…000000000000000000000]", a.Mul(-1243611136).ToString());
    }

    [Fact]
    public void Test_var_Mul_int_const()
    {
        var a = UnknownTypedValue.Create(TypeDB.Int).WithVarID(0);
        Assert.Equal(a, a.Mul(1));
        Assert.Equal("UnknownValueBitTracker<int>[bcdefghijklmnopqrstuvwxyzабвгде0]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[cdefghijklmnopqrstuvwxyzабвгде00]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[е0000000000000000000000000000000]", a.Mul(0x80000000).ToString());
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Int), a.Mul(0x100000000L));

        Assert.Equal("UnknownValueBitTracker<int>[…е]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…где]", a.Mul(25).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…где00]", a.Mul(100).ToString());

        Assert.Equal("UnknownValueBitTracker<int>[…е0]", a.Mul(2 + 4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…е0]", a.Mul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…е0]", a.Mul(2 + 4 + 8 + 16).ToString());

        Assert.Equal("UnknownValueBitTracker<int>[…е000000000000000000000]", a.Mul(0xB5E00000).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…е000000000000000000000]", a.Mul(-1243611136).ToString());
    }
}
