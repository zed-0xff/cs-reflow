using Xunit;

public class UnknownValueBitsTest
{
    [Fact]
    public void Test_ToString()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 1, 0, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[01_]", a.ToString());

        a = new UnknownValueBits(TypeDB.Byte);
        Assert.Equal("UnknownValueBits<byte>[]", a.ToString());
    }

    [Fact]
    public void Test_Cardinality()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(1, a.Cardinality());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(1, a.Cardinality());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(1, a.Cardinality());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(2, a.Cardinality());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 0, 0, 0, 0, 0, -1 });
        Assert.Equal(4, a.Cardinality());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(256, a.Cardinality());
    }

    [Fact]
    public void Test_Values()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(new List<long> { 0, 1, 2, 3 }, a.Values());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, 0, 0, 0, 0 });
        Assert.Equal(new List<long> { 0, 4, 8, 12 }, a.Values());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, 0, 0, 0, 0, 0, -1 });
        Assert.Equal(new List<long> { 0, 128 }, a.Values());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, 0, 0, 0, 0, 0, -1 });
        Assert.Equal(new List<long> { 1, 129 }, a.Values());
    }

    [Fact]
    public void Test_Min_byte()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        Assert.Equal(0, a.Min());

        a.SetBit(0, 1);
        Assert.Equal(1, a.Min());

        a.SetBit(0, 0);
        Assert.Equal(0, a.Min());

        a.SetBit(1, 1);
        Assert.Equal(2, a.Min());

        a.SetBit(0, 1);
        Assert.Equal(3, a.Min());

        a.SetBit(7, 1);
        Assert.Equal(131, a.Min());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(0, a.Min());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(255, a.Min());
    }

    [Fact]
    public void Test_Min_sbyte()
    {
        var a = new UnknownValueBits(TypeDB.SByte);
        Assert.Equal(-128, a.Min());

        a.SetBit(0, 1);
        Assert.Equal(-127, a.Min());

        a.SetBit(0, 0);
        Assert.Equal(-128, a.Min());

        a.SetBit(1, 1);
        Assert.Equal(-126, a.Min());

        a.SetBit(0, 1);
        Assert.Equal(-125, a.Min());

        a.SetBit(7, 1);
        Assert.Equal(-125, a.Min());

        a = new UnknownValueBits(TypeDB.SByte);
        a.SetBit(7, 0);
        Assert.Equal(0, a.Min());

        a = new UnknownValueBits(TypeDB.SByte, new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(0, a.Min());

        a = new UnknownValueBits(TypeDB.SByte, new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(-1, a.Min());

        a = new UnknownValueBits(TypeDB.SByte, new sbyte[] { -1, -1, -1, -1, -1, -1, -1, 1 });
        Assert.Equal(-128, a.Min());

        a = new UnknownValueBits(TypeDB.SByte, new sbyte[] { -1, -1, -1, -1, -1, -1, -1, 0 });
        Assert.Equal(0, a.Min());
    }

    [Fact]
    public void Test_Max_byte()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        Assert.Equal(255, a.Max());

        a.SetBit(0, 1);
        Assert.Equal(255, a.Max());

        a.SetBit(0, 0);
        Assert.Equal(254, a.Max());

        a.SetBit(1, 1);
        Assert.Equal(254, a.Max());

        a.SetBit(0, 1);
        Assert.Equal(255, a.Max());

        a.SetBit(7, 1);
        Assert.Equal(255, a.Max());

        a.SetBit(7, 0);
        Assert.Equal(127, a.Max());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(0, a.Max());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(255, a.Max());
    }

    [Fact]
    public void Test_Max_sbyte()
    {
        var a = new UnknownValueBits(TypeDB.SByte);
        Assert.Equal(127, a.Max());

        a.SetBit(0, 1);
        Assert.Equal(127, a.Max());

        a.SetBit(0, 0);
        Assert.Equal(126, a.Max());

        a.SetBit(1, 1);
        Assert.Equal(126, a.Max());

        a.SetBit(0, 1);
        Assert.Equal(127, a.Max());

        a.SetBit(7, 0);
        Assert.Equal(127, a.Max());

        a = new UnknownValueBits(TypeDB.SByte, new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(0, a.Max());

        a = new UnknownValueBits(TypeDB.SByte, new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(-1, a.Max());

        a = new UnknownValueBits(TypeDB.SByte, new sbyte[] { -1, -1, -1, -1, -1, -1, -1, 1 });
        Assert.Equal(-1, a.Max());

        a = new UnknownValueBits(TypeDB.SByte, new sbyte[] { -1, -1, -1, -1, -1, -1, -1, 0 });
        Assert.Equal(127, a.Max());
    }

    [Fact]
    public void Test_ShiftLeft()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var b = a.ShiftLeft(2);
        Assert.Equal("UnknownValueBits<byte>[00]", b.ToString());
    }

    [Fact]
    public void Test_BitwiseAnd()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, -1, -1, -1, -1, -1, -1 });
        var b = a.BitwiseAnd(7);
        Assert.Equal("UnknownValueBits<byte>[00000_10]", b.ToString());
    }

    [Fact]
    public void Test_Add()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        Assert.Equal(a, a.Add(0));
        Assert.Equal(a, a.Add(7));
        Assert.Equal(a, a.Add(255));

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, -1, -1, 1, 0, 1, -1 });
        Assert.Equal(a, a.Add(0));
        Assert.Equal("UnknownValueBits<byte>[101__11]", a.Add(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[00]", a.Add(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[01]", a.Add(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[01]", a.Add(7).ToString());

        // no-unknown-carry add
        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, 1, 0, 1, -1 });
        Assert.Equal("UnknownValueBits<byte>[101__01]", a.Add(1).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 0, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<byte>[000011__]", a.Add(0b1100).ToString());
    }

    [Fact]
    public void Test_Add_self()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[000]", a.Add(a).ToString());
        Assert.NotEqual(a, a.Add(a));

        // not self but same
        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(a, a.Add(b));
    }

    [Fact]
    public void Test_Xor()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        Assert.Equal(a, a.Xor(0));
        Assert.Equal(a, a.Xor(7));
        Assert.Equal(a, a.Xor(255));

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, -1, -1, 1, 0, 1, -1 });
        Assert.Equal(a, a.Xor(0));
        Assert.Equal("UnknownValueBits<byte>[101__11]", a.Xor(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[101__00]", a.Xor(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[101__01]", a.Xor(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[101__01]", a.Xor(7).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, 1, 0, 1, -1 });
        Assert.Equal("UnknownValueBits<byte>[101__01]", a.Xor(1).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 0, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<byte>[000011__]", a.Xor(0b1100).ToString());
        Assert.Equal("UnknownValueBits<byte>[000011__]", a.Xor(0b1111).ToString());

        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 1, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<byte>[000001__]", a.Xor(b).ToString());
        b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, 1, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<byte>[000001__]", a.Xor(b).ToString());
    }

    [Fact]
    public void Test_Xor_self()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(new UnknownValueList(TypeDB.Byte, new List<long> { 0 }), a.Xor(a));
        Assert.NotEqual(a, a.Xor(a));

        // not self but same
        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(a, a.Xor(b));
    }

    [Fact]
    public void Test_And_Or()
    {
        var a = UnknownValueBits.CreateFromAnd(TypeDB.Int, -265);
        Assert.Equal("UnknownValueBits<int>[0____0___]", a.ToString());

        var b = a.BitwiseOr(0x82);
        Assert.Equal("UnknownValueBits<int>[01___0_1_]", b.ToString());
    }

    [Fact]
    public void Test_Mul()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var u = UnknownValue.Create(TypeDB.Byte);
        Assert.Equal(u, a.Mul(u));
        Assert.Equal(a, a.Mul(1));
        Assert.Equal("UnknownValueBits<byte>[0]", a.Mul(2).ToString());

        a.SetBit(0, 0);
        Assert.Equal(u, a.Mul(u));
        Assert.Equal("UnknownValueBits<byte>[0]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[00]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[00]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[000]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<byte>[000]", a.Mul(5).ToString());

        a.SetBit(0, 1);
        Assert.Equal(u, a.Mul(u));
        Assert.Equal("UnknownValueBits<byte>[1]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[10]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[11]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[100]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<byte>[101]", a.Mul(5).ToString());

        a.SetBit(0, 0);
        a.SetBit(1, 1);
        Assert.Equal(u, a.Mul(u));
        Assert.Equal("UnknownValueBits<byte>[10]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[100]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[110]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[1000]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<byte>[1010]", a.Mul(5).ToString());
    }
}
