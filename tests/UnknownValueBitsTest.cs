using Xunit;

public class UnknownValueBitsTest
{
    [Fact]
    public void Test_ctor()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new BitSpan(0, 255));
        Assert.Equal("UnknownValueBits<byte>[…]", a.ToString());

        a = new UnknownValueBits(TypeDB.Byte, new BitSpan(0, 254));
        Assert.Equal("UnknownValueBits<byte>[…0]", a.ToString());

        a = new UnknownValueBits(TypeDB.Byte, new BitSpan(1, 255));
        Assert.Equal("UnknownValueBits<byte>[…1]", a.ToString());
    }

    [Fact]
    public void Test_bits()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 1, 0, -1, -1, -1, -1, -1 });
        Assert.False(a.IsZeroBit(0));
        Assert.False(a.IsZeroBit(1));
        Assert.True(a.IsZeroBit(2));

        Assert.False(a.IsOneBit(0));
        Assert.True(a.IsOneBit(1));
        Assert.False(a.IsOneBit(2));
    }

    [Fact]
    public void Test_BitSpan()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        Assert.Equal(new BitSpan(0, 255), a.BitSpan());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(new BitSpan(0, 0), a.BitSpan());
    }

    [Fact]
    public void Test_ToString()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 1, 0, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[…01_]", a.ToString());

        a = new UnknownValueBits(TypeDB.Byte);
        Assert.Equal("UnknownValueBits<byte>[…]", a.ToString());
    }

    [Fact]
    public void Test_Cardinality()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(1UL, a.Cardinality());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(1UL, a.Cardinality());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(1UL, a.Cardinality());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(2UL, a.Cardinality());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 0, 0, 0, 0, 0, -1 });
        Assert.Equal(4UL, a.Cardinality());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(256UL, a.Cardinality());
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

        a = a.SetBit(0, 1);
        Assert.Equal(1, a.Min());

        a = a.SetBit(0, 0);
        Assert.Equal(0, a.Min());

        a = a.SetBit(1, 1);
        Assert.Equal(2, a.Min());

        a = a.SetBit(0, 1);
        Assert.Equal(3, a.Min());

        a = a.SetBit(7, 1);
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

        a = a.SetBit(0, 1);
        Assert.Equal(-127, a.Min());

        a = a.SetBit(0, 0);
        Assert.Equal(-128, a.Min());

        a = a.SetBit(1, 1);
        Assert.Equal(-126, a.Min());

        a = a.SetBit(0, 1);
        Assert.Equal(-125, a.Min());

        a = a.SetBit(7, 1);
        Assert.Equal(-125, a.Min());

        a = new UnknownValueBits(TypeDB.SByte);
        a = a.SetBit(7, 0);
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

        a = a.SetBit(0, 1);
        Assert.Equal(255, a.Max());

        a = a.SetBit(0, 0);
        Assert.Equal(254, a.Max());

        a = a.SetBit(1, 1);
        Assert.Equal(254, a.Max());

        a = a.SetBit(0, 1);
        Assert.Equal(255, a.Max());

        a = a.SetBit(7, 1);
        Assert.Equal(255, a.Max());

        a = a.SetBit(7, 0);
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

        a = a.SetBit(0, 1);
        Assert.Equal(127, a.Max());

        a = a.SetBit(0, 0);
        Assert.Equal(126, a.Max());

        a = a.SetBit(1, 1);
        Assert.Equal(126, a.Max());

        a = a.SetBit(0, 1);
        Assert.Equal(127, a.Max());

        a = a.SetBit(7, 0);
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
        Assert.Equal("UnknownValueBits<byte>[…00]", b.ToString());
    }

    [Fact]
    public void Test_BitwiseAnd()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, -1, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[00000_10]", a.BitwiseAnd(7).ToString());
        Assert.Equal("UnknownValueBits<byte>[00000000]", a.BitwiseAnd(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[00000010]", a.BitwiseAnd(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[…10]", a.BitwiseAnd(255).ToString());
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
        Assert.Equal("UnknownValueBits<byte>[…101__11]", a.Add(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[…1____00]", a.Add(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[…1____01]", a.Add(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[…1____01]", a.Add(7).ToString());

        // no-unknown-carry add
        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, 1, 0, 1, -1 });
        Assert.Equal("UnknownValueBits<byte>[…101__01]", a.Add(1).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 0, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<byte>[000011__]", a.Add(0b1100).ToString());
    }

    [Fact]
    public void Test_Add_self()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[…000]", a.Add(a).ToString());
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
        Assert.Equal("UnknownValueBits<byte>[…101__11]", a.Xor(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[…101__00]", a.Xor(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[…101__01]", a.Xor(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[…101__01]", a.Xor(7).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, 1, 0, 1, -1 });
        Assert.Equal("UnknownValueBits<byte>[…101__01]", a.Xor(1).ToString());

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
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Byte), a.Xor(a));
        Assert.NotEqual(a, a.Xor(a));

        // not self but same => unknown bits should remain unknown
        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.NotEqual(a.Xor(a), a.Xor(b));
        Assert.Equal("UnknownValueBits<byte>[…00]", a.Xor(b).ToString());
    }

    [Fact]
    public void Test_Xor_const()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[…01]", a.TypedXor(0b00).ToString());
        Assert.Equal("UnknownValueBits<byte>[…00]", a.TypedXor(0b01).ToString());
        Assert.Equal("UnknownValueBits<byte>[…11]", a.TypedXor(0b10).ToString());
        Assert.Equal("UnknownValueBits<byte>[…10]", a.TypedXor(0b11).ToString());
        Assert.Equal("UnknownValueBits<byte>[…10]", a.TypedXor(0b111).ToString());
    }

    [Fact]
    public void Test_Xor_unk()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, 0, 1, -1, -1, -1, -1 });
        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 1, 1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[…010_]", a.TypedXor(b).ToString());
    }

    [Fact]
    public void Test_And_const()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(a, a.BitwiseAnd(a));
        Assert.Equal("UnknownValueBits<byte>[00000000]", a.TypedBitwiseAnd(0b00).ToString());
        Assert.Equal("UnknownValueBits<byte>[00000001]", a.TypedBitwiseAnd(0b01).ToString());
        Assert.Equal("UnknownValueBits<byte>[00000000]", a.TypedBitwiseAnd(0b10).ToString());
        Assert.Equal("UnknownValueBits<byte>[00000001]", a.TypedBitwiseAnd(0b11).ToString());
        Assert.Equal("UnknownValueBits<byte>[00000_01]", a.TypedBitwiseAnd(0b111).ToString());
    }

    [Fact]
    public void Test_And_unk()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, 0, 1, -1, -1, -1, -1 });
        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 1, 1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[…100_]", a.TypedBitwiseAnd(b).ToString());
    }

    [Fact]
    public void Test_Or_const()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(a, a.BitwiseOr(a));
        Assert.Equal("UnknownValueBits<byte>[…01]", a.TypedBitwiseOr(0b00).ToString());
        Assert.Equal("UnknownValueBits<byte>[…01]", a.TypedBitwiseOr(0b01).ToString());
        Assert.Equal("UnknownValueBits<byte>[…11]", a.TypedBitwiseOr(0b10).ToString());
        Assert.Equal("UnknownValueBits<byte>[…11]", a.TypedBitwiseOr(0b11).ToString());
        Assert.Equal("UnknownValueBits<byte>[…111]", a.TypedBitwiseOr(0b111).ToString());
    }

    [Fact]
    public void Test_Or_unk()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, 0, -1, -1, -1, -1, -1 });
        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 1, 1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[…1101]", a.TypedBitwiseOr(b).ToString());
    }

    [Fact]
    public void Test_CreateFromAnd()
    {
        var a = UnknownValueBits.CreateFromAnd(TypeDB.Int, -130);
        Assert.Equal("UnknownValueBits<int>[…0______0]", a.ToString());
    }

    [Fact]
    public void Test_CreateFromOr()
    {
        var a = UnknownValueBits.CreateFromOr(TypeDB.Int, 129);
        Assert.Equal("UnknownValueBits<int>[…1______1]", a.ToString());
    }

    [Fact]
    public void Test_Mul()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var u = UnknownValue.Create(TypeDB.Byte);
        Assert.Equal(u, a.Mul(u));
        Assert.Equal(a, a.Mul(1));
        Assert.Equal("UnknownValueBits<byte>[…0]", a.Mul(2).ToString());

        a = a.SetBit(0, 0);
        Assert.Equal(u, a.Mul(u));
        Assert.Equal("UnknownValueBits<byte>[…0]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[…00]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[…0]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[…000]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<byte>[…0]", a.Mul(5).ToString());

        a = a.SetBit(0, 1);
        Assert.Equal(u, a.Mul(u));
        Assert.Equal("UnknownValueBits<byte>[…1]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[…10]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[…1]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[…100]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<byte>[…1]", a.Mul(5).ToString());

        a = a.SetBit(0, 0);
        a = a.SetBit(1, 1);
        Assert.Equal(u, a.Mul(u));
        Assert.Equal("UnknownValueBits<byte>[…10]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[…100]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[…10]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[…1000]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<byte>[…10]", a.Mul(5).ToString());
    }

    [Fact]
    public void Test_Mod()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var zero = UnknownTypedValue.Zero(TypeDB.Byte);
        Assert.Equal(zero, a.Mod(a));
        Assert.Equal(zero, a.Mod(1));
        Assert.Equal(new UnknownValue(), a.Mod(0));
        Assert.Equal(zero, a.Mod(-1));
        Assert.Equal(new UnknownValue(), a.Mod(-10));

        Assert.Equal(new UnknownValueRange(TypeDB.Byte, 0, 1), a.Mod(2));
        Assert.Equal(new UnknownValueRange(TypeDB.Byte, 0, 3), a.Mod(4));

        a = new UnknownValueBits(TypeDB.Int);
        Assert.Equal(new UnknownValueRange(TypeDB.Int, -5, 5), a.Mod(6));
    }

    [Fact]
    public void Test_Eq()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new BitSpan(123, 123));
        Assert.Equal(1UL, a.Cardinality());
        Assert.Equal(true, a.Eq(123));
        Assert.Equal(false, a.Ne(123));

        a = new UnknownValueBits(TypeDB.Int, new BitSpan(0x24da, 0x24da)); // all bits are known
        Assert.Equal(1UL, a.Cardinality());
        Assert.Equal(true, a.Eq(0x24DA));
        Assert.Equal(false, a.Ne(0x24DA));
    }
}
