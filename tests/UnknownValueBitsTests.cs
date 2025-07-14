using Xunit;

public class UnknownValueBitsTests
{
    [Fact]
    public void Test_ctor()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new BitSpan(0, 255));
        Assert.Equal("UnknownValueBits<byte>[________]", a.ToString());

        a = new UnknownValueBits(TypeDB.Byte, new BitSpan(0, 254));
        Assert.Equal("UnknownValueBits<byte>[_______0]", a.ToString());

        a = new UnknownValueBits(TypeDB.Byte, new BitSpan(1, 255));
        Assert.Equal("UnknownValueBits<byte>[_______1]", a.ToString());
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
        Assert.Equal("UnknownValueBits<byte>[_____01_]", a.ToString());

        a = new UnknownValueBits(TypeDB.Byte);
        Assert.Equal("UnknownValueBits<byte>[________]", a.ToString());
    }

    [Fact]
    public void Test_Cardinality()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(1UL, a.Cardinality().ulValue);

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(1UL, a.Cardinality().ulValue);

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(1UL, a.Cardinality().ulValue);

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(2UL, a.Cardinality().ulValue);

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 0, 0, 0, 0, 0, -1 });
        Assert.Equal(4UL, a.Cardinality().ulValue);

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(256UL, a.Cardinality().ulValue);
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
        Assert.Equal("UnknownValueBits<int>[…0________00]", b.ToString());
    }

    [Fact]
    public void Test_TypedShiftLeft()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var b = a.TypedShiftLeft(2);
        Assert.Equal("UnknownValueBits<byte>[______00]", b.ToString());
    }

    [Fact]
    public void Test_TypedBitwiseAnd()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, -1, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[00000_10]", a.TypedBitwiseAnd(7).ToString());
        Assert.Equal("UnknownValueBits<byte>[00000000]", a.TypedBitwiseAnd(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[00000010]", a.TypedBitwiseAnd(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[______10]", a.TypedBitwiseAnd(255).ToString());
    }

    [Fact]
    public void Test_BitwiseAnd()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, -1, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<int>[…0_10]", a.BitwiseAnd(7).ToString());
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Int), a.BitwiseAnd(1));
        Assert.Equal("UnknownValueBits<int>[…010]", a.BitwiseAnd(3).ToString());
        Assert.Equal("UnknownValueBits<int>[…0______10]", a.BitwiseAnd(255).ToString());
    }

    [Fact]
    public void Test_Add()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        Assert.Equal(a.Upcast(TypeDB.Int), a.Add(0));
        Assert.Equal("UnknownValueBits<int>[…0_________]", a.Add(7).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_________]", a.Add(255).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, -1, -1, 1, 0, 1, -1 });
        Assert.Equal(a.Upcast(TypeDB.Int), a.Add(0));
        Assert.Equal("UnknownValueBits<int>[…0_101__11]", a.Add(1).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_1____00]", a.Add(2).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_1____01]", a.Add(3).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_1____01]", a.Add(7).ToString());

        // no-unknown-carry add
        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, 1, 0, 1, -1 });
        Assert.Equal("UnknownValueBits<int>[…0_101__01]", a.Add(1).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 0, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<int>[…011__]", a.Add(0b1100).ToString());
    }

    [Fact]
    public void Test_TypedAdd()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var fullrange = UnknownTypedValue.Create(TypeDB.Byte);
        Assert.Equal(a, a.TypedAdd(0));
        Assert.Equal(fullrange, a.TypedAdd(7));
        Assert.Equal(fullrange, a.TypedAdd(255));

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, -1, -1, 1, 0, 1, -1 });
        Assert.Equal(a, a.TypedAdd(0));
        Assert.Equal("UnknownValueBits<byte>[_101__11]", a.TypedAdd(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[_1____00]", a.TypedAdd(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[_1____01]", a.TypedAdd(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[_1____01]", a.TypedAdd(7).ToString());

        // no-unknown-carry add
        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, 1, 0, 1, -1 });
        Assert.Equal("UnknownValueBits<byte>[_101__01]", a.TypedAdd(1).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 0, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<byte>[000011__]", a.TypedAdd(0b1100).ToString());
    }

    [Fact]
    public void Test_Add_self()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<int>[…0______000]", a.Add(a).ToString());
        Assert.NotEqual(a, a.Add(a));

        // not self but same
        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<int>[…0_______00]", a.Add(b).ToString());
    }

    [Fact]
    public void Test_Xor()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var fullrange = UnknownTypedValue.Create(TypeDB.Byte).Upcast(TypeDB.Int);
        Assert.Equal(a.Upcast(TypeDB.Int), a.Xor(0));
        Assert.Equal(fullrange, a.Xor(7));
        Assert.Equal(fullrange, a.Xor(255));

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, -1, -1, 1, 0, 1, -1 });
        Assert.Equal(a.Upcast(TypeDB.Int), a.Xor(0));
        Assert.Equal("UnknownValueBits<int>[…0_101__11]", a.Xor(1).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_101__00]", a.Xor(2).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_101__01]", a.Xor(3).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_101__01]", a.Xor(7).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, 1, 0, 1, -1 });
        Assert.Equal("UnknownValueBits<int>[…0_101__01]", a.Xor(1).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 0, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<int>[…011__]", a.Xor(0b1100).ToString());
        Assert.Equal("UnknownValueBits<int>[…011__]", a.Xor(0b1111).ToString());

        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 1, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<int>[…01__]", a.Xor(b).ToString());
        b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, 1, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<int>[…01__]", a.Xor(b).ToString());
    }

    [Fact]
    public void Test_TypedXor()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var fullrange = UnknownTypedValue.Create(TypeDB.Byte);
        Assert.Equal(a, a.TypedXor(0));
        Assert.Equal(fullrange, a.TypedXor(7));
        Assert.Equal(fullrange, a.TypedXor(255));

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, -1, -1, 1, 0, 1, -1 });
        Assert.Equal(a, a.TypedXor(0));
        Assert.Equal("UnknownValueBits<byte>[_101__11]", a.TypedXor(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[_101__00]", a.TypedXor(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[_101__01]", a.TypedXor(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[_101__01]", a.TypedXor(7).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 0, -1, -1, 1, 0, 1, -1 });
        Assert.Equal("UnknownValueBits<byte>[_101__01]", a.TypedXor(1).ToString());

        a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 0, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<byte>[000011__]", a.TypedXor(0b1100).ToString());
        Assert.Equal("UnknownValueBits<byte>[000011__]", a.TypedXor(0b1111).ToString());

        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, -1, 1, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<byte>[000001__]", a.TypedXor(b).ToString());
        b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 0, 1, 1, 0, 0, 0, 0, 0 });
        Assert.Equal("UnknownValueBits<byte>[000001__]", a.TypedXor(b).ToString());
    }

    [Fact]
    public void Test_Xor_self()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Int), a.Xor(a));
        Assert.NotEqual(a, a.Xor(a));

        // not self but same => unknown bits should remain unknown
        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.NotEqual(a.Xor(a), a.Xor(b));
        Assert.Equal("UnknownValueBits<byte>[______00]", a.TypedXor(b).ToString());
        Assert.Equal("UnknownValueBits<int>[…0______00]", a.Xor(b).ToString());
    }

    [Fact]
    public void Test_Xor_const()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[______01]", a.TypedXor(0b00).ToString());
        Assert.Equal("UnknownValueBits<byte>[______00]", a.TypedXor(0b01).ToString());
        Assert.Equal("UnknownValueBits<byte>[______11]", a.TypedXor(0b10).ToString());
        Assert.Equal("UnknownValueBits<byte>[______10]", a.TypedXor(0b11).ToString());
        Assert.Equal("UnknownValueBits<byte>[______10]", a.TypedXor(0b111).ToString());
    }

    [Fact]
    public void Test_Xor_unk()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, 0, 1, -1, -1, -1, -1 });
        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 1, 1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[____010_]", a.TypedXor(b).ToString());
    }

    [Fact]
    public void Test_And_const()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(a.Upcast(TypeDB.Int), a.BitwiseAnd(a));
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Int), a.BitwiseAnd(0b00));
        Assert.Equal(UnknownTypedValue.One(TypeDB.Int), a.BitwiseAnd(0b01));
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Int), a.BitwiseAnd(0b10));
        Assert.Equal(UnknownTypedValue.One(TypeDB.Int), a.BitwiseAnd(0b11));
        Assert.Equal("UnknownValueBits<int>[…0_01]", a.BitwiseAnd(0b111).ToString());
    }

    [Fact]
    public void Test_TypedAnd_const()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(a, a.TypedBitwiseAnd(a));
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
        Assert.Equal("UnknownValueBits<byte>[____100_]", a.TypedBitwiseAnd(b).ToString());
    }

    [Fact]
    public void Test_TypedBitwiseOr_const()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal(a, a.TypedBitwiseOr(a));
        Assert.Equal("UnknownValueBits<byte>[______01]", a.TypedBitwiseOr(0b00).ToString());
        Assert.Equal("UnknownValueBits<byte>[______01]", a.TypedBitwiseOr(0b01).ToString());
        Assert.Equal("UnknownValueBits<byte>[______11]", a.TypedBitwiseOr(0b10).ToString());
        Assert.Equal("UnknownValueBits<byte>[______11]", a.TypedBitwiseOr(0b11).ToString());
        Assert.Equal("UnknownValueBits<byte>[_____111]", a.TypedBitwiseOr(0b111).ToString());
    }

    [Fact]
    public void Test_BitwiseOr_const()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, -1, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<int>[…0______01]", a.BitwiseOr(a).ToString());
        Assert.Equal("UnknownValueBits<int>[…0______01]", a.BitwiseOr(0b00).ToString());
        Assert.Equal("UnknownValueBits<int>[…0______01]", a.BitwiseOr(0b01).ToString());
        Assert.Equal("UnknownValueBits<int>[…0______11]", a.BitwiseOr(0b10).ToString());
        Assert.Equal("UnknownValueBits<int>[…0______11]", a.BitwiseOr(0b11).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_____111]", a.BitwiseOr(0b111).ToString());
    }

    [Fact]
    public void Test_Or_unk()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { 1, 0, 0, -1, -1, -1, -1, -1 });
        var b = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 0, 1, 1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[____1101]", a.TypedBitwiseOr(b).ToString());
    }

    [Fact]
    public void Test_Mul()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var u = UnknownValue.Create(TypeDB.Byte);
        // Assert.Equal("", a.Mul(u).ToString()); // TODO: byte x byte should be limited to word range
        Assert.Equal(UnknownValue.Create(TypeDB.Int), a.Mul(u));
        Assert.Equal(a.Upcast(TypeDB.Int), a.Mul(1));
        Assert.Equal("UnknownValueBits<int>[…0________0]", a.Mul(2).ToString());

        a = a.SetBit(0, 0);
        Assert.Equal(UnknownValue.Create(TypeDB.Int), a.Mul(u));
        Assert.Equal("UnknownValueBits<int>[…0_______0]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_______00]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_________0]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_______000]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<int>[…0__________0]", a.Mul(5).ToString());

        a = a.SetBit(0, 1);
        Assert.Equal(UnknownValue.Create(TypeDB.Int), a.Mul(u));
        Assert.Equal("UnknownValueBits<int>[…0_______1]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_______10]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_________1]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_______100]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<int>[…0__________1]", a.Mul(5).ToString());

        a = a.SetBit(0, 0);
        a = a.SetBit(1, 1);
        Assert.Equal(UnknownValue.Create(TypeDB.Int), a.Mul(u));
        Assert.Equal("UnknownValueBits<int>[…0______10]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBits<int>[…0______100]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBits<int>[…0________10]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBits<int>[…0______1000]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBits<int>[…0_________10]", a.Mul(5).ToString());
    }

    [Fact]
    public void Test_TypedMul()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var u = UnknownValue.Create(TypeDB.Byte);
        Assert.Equal(a, a.TypedMul(u));
        Assert.Equal(a, a.TypedMul(1));
        Assert.Equal("UnknownValueBits<byte>[_______0]", a.TypedMul(2).ToString());

        a = a.SetBit(0, 0);
        Assert.Equal(u, a.TypedMul(u));
        Assert.Equal("UnknownValueBits<byte>[_______0]", a.TypedMul(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[______00]", a.TypedMul(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[_______0]", a.TypedMul(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[_____000]", a.TypedMul(4).ToString());
        Assert.Equal("UnknownValueBits<byte>[_______0]", a.TypedMul(5).ToString());

        a = a.SetBit(0, 1);
        Assert.Equal(u, a.TypedMul(u));
        Assert.Equal("UnknownValueBits<byte>[_______1]", a.TypedMul(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[______10]", a.TypedMul(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[_______1]", a.TypedMul(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[_____100]", a.TypedMul(4).ToString());
        Assert.Equal("UnknownValueBits<byte>[_______1]", a.TypedMul(5).ToString());

        a = a.SetBit(0, 0);
        a = a.SetBit(1, 1);
        Assert.Equal(u, a.TypedMul(u));
        Assert.Equal("UnknownValueBits<byte>[______10]", a.TypedMul(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[_____100]", a.TypedMul(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[______10]", a.TypedMul(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[____1000]", a.TypedMul(4).ToString());
        Assert.Equal("UnknownValueBits<byte>[______10]", a.TypedMul(5).ToString());
    }

    [Fact]
    public void Test_Mod()
    {
        var a = new UnknownValueBits(TypeDB.Byte);
        var zero = UnknownTypedValue.Zero(TypeDB.Int);
        Assert.Equal(zero, a.Mod(a));
        Assert.Equal(zero, a.Mod(1));
        Assert.Equal(new UnknownValue(), a.Mod(0));
        Assert.Equal(zero, a.Mod(-1));
        Assert.Equal(a.Mod(10), a.Mod(-10));

        Assert.Equal("UnknownValueBits<int>[…0____]", a.Mod(10).ToString());
        Assert.Equal("UnknownValueBits<int>[…0____]", a.Mod(-10).ToString());

        Assert.Equal("UnknownValueBits<byte>[0000000_]", a.TypedMod(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[000000__]", a.TypedMod(4).ToString());

        Assert.Equal("UnknownValueBits<int>[…0_]", a.Mod(2).ToString());
        Assert.Equal("UnknownValueBits<int>[…0__]", a.Mod(4).ToString());

        a = new UnknownValueBits(TypeDB.Int);
        Assert.Equal(new UnknownValueRange(TypeDB.Int, -5, 5), a.Mod(6));
    }

    [Fact]
    public void Test_Eq()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new BitSpan(123, 123));
        Assert.Equal(1UL, a.Cardinality().ulValue);
        Assert.Equal(true, a.Eq(123));
        Assert.Equal(false, a.Ne(123));

        a = new UnknownValueBits(TypeDB.Int, new BitSpan(0x24da, 0x24da)); // all bits are known
        Assert.Equal(1UL, a.Cardinality().ulValue);
        Assert.Equal(true, a.Eq(0x24DA));
        Assert.Equal(false, a.Ne(0x24DA));
    }

    [Fact]
    public void Test_Upcast()
    {
        var a = new UnknownValueBits(TypeDB.Byte, new sbyte[] { -1, 1, 0, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[_____01_]", a.ToString());
        Assert.Equal("UnknownValueBits<int>[…0_____01_]", a.Upcast(TypeDB.Int).ToString());
        Assert.Equal("UnknownValueBits<uint>[…0_____01_]", a.Upcast(TypeDB.UInt).ToString());
    }
}
