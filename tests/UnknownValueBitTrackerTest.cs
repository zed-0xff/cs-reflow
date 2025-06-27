using Xunit;

public class UnknownValueBitTrackerTest
{
    IEnumerable<int> gen_range(int start, int end) => Enumerable.Range(start, end - start + 1);

    [Fact]
    public void Test_ctor()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal("UnknownValueBitTracker<byte>[abcdefgh]", a.ToString());

        a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(0, 254));
        Assert.Equal("UnknownValueBitTracker<byte>[abcdefg0]", a.ToString());

        a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(1, 255));
        Assert.Equal("UnknownValueBitTracker<byte>[abcdefg1]", a.ToString());

        a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(1, 127));
        Assert.Equal("UnknownValueBitTracker<byte>[0bcdefg1]", a.ToString());
    }

    [Fact]
    public void Test_Bits()
    {
        // lists are reversed to keep visually natural order, i.e. "abcdefgh" instead of "hgfedcba"

        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(gen_range(2, 9).Reverse(), a.Bits);
        a = a.BitwiseNot();
        Assert.Equal(gen_range(2, 9).Reverse().Select(x => -x), a.Bits);

        a = new UnknownValueBitTracker(TypeDB.Byte, 1);
        Assert.Equal(gen_range(10, 17).Reverse(), a.Bits);

        a = new UnknownValueBitTracker(TypeDB.Int, 0);
        Assert.Equal(gen_range(2, 33).Reverse(), a.Bits);

        a = new UnknownValueBitTracker(TypeDB.Int, 1);
        Assert.Equal(gen_range(34, 65).Reverse(), a.Bits);
    }

    [Fact]
    public void Test_ToString()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal("UnknownValueBitTracker<byte>[abcdefgh]", a.ToString());

        a = a.BitwiseNot();
        Assert.Equal("UnknownValueBitTracker<byte>[ABCDEFGH]", a.ToString());

        a = new UnknownValueBitTracker(TypeDB.Byte, 1);
        Assert.Equal("UnknownValueBitTracker<byte>[ijklmnop]", a.ToString());

        a = new UnknownValueBitTracker(TypeDB.UShort, 0);
        Assert.Equal("UnknownValueBitTracker<ushort>[abcdefghijklmnop]", a.ToString());
    }

    [Fact]
    public void Test_ShiftLeft()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        var b = a.ShiftLeft(1);
        Assert.Equal("UnknownValueBitTracker<byte>[bcdefgh0]", b.ToString());

        b = a.ShiftLeft(2);
        Assert.Equal("UnknownValueBitTracker<byte>[cdefgh00]", b.ToString());
    }

    [Fact]
    public void Test_SignedShiftRight_Byte()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        var b = a.SignedShiftRight(1);
        Assert.Equal("UnknownValueBitTracker<byte>[0abcdefg]", b.ToString());

        b = a.SignedShiftRight(2);
        Assert.Equal("UnknownValueBitTracker<byte>[00abcdef]", b.ToString());
    }

    [Fact]
    public void Test_SignedShiftRight_SByte()
    {
        var a = new UnknownValueBitTracker(TypeDB.SByte, 0);
        var b = a.SignedShiftRight(1);
        Assert.Equal("UnknownValueBitTracker<sbyte>[aabcdefg]", b.ToString());

        b = a.SignedShiftRight(2);
        Assert.Equal("UnknownValueBitTracker<sbyte>[aaabcdef]", b.ToString());
    }

    [Fact]
    public void Test_UnsignedShiftRight_Byte()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        var b = a.UnsignedShiftRight(1);
        Assert.Equal("UnknownValueBitTracker<byte>[0abcdefg]", b.ToString());

        b = a.UnsignedShiftRight(2);
        Assert.Equal("UnknownValueBitTracker<byte>[00abcdef]", b.ToString());
    }

    [Fact]
    public void Test_UnsignedShiftRight_SByte()
    {
        var a = new UnknownValueBitTracker(TypeDB.SByte, 0);
        var b = a.UnsignedShiftRight(1);
        Assert.Equal("UnknownValueBitTracker<sbyte>[0abcdefg]", b.ToString());

        b = a.UnsignedShiftRight(2);
        Assert.Equal("UnknownValueBitTracker<sbyte>[00abcdef]", b.ToString());
    }

    [Fact]
    public void Test_BitwiseNot()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        var b = a.BitwiseNot();
        Assert.Equal("UnknownValueBitTracker<byte>[ABCDEFGH]", b.ToString());

        b = b.BitwiseNot();
        Assert.Equal("UnknownValueBitTracker<byte>[abcdefgh]", b.ToString());
        Assert.Equal(a, b);

        a = new UnknownValueBitTracker(TypeDB.Byte, 0).SetBit(1, -1).SetBit(7, 0).SetBit(0, 1);
        Assert.Equal("UnknownValueBitTracker<byte>[0bcdef_1]", a.ToString());

        b = a.BitwiseNot();
        Assert.Equal("UnknownValueBitTracker<byte>[1BCDEF_0]", b.ToString());

        b = b.BitwiseNot();
        Assert.Equal(a, b);
    }

    [Fact]
    public void Test_Xor()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Byte), a.Xor(a));
        var b = a.Xor(129);
        Assert.Equal("UnknownValueBitTracker<byte>[AbcdefgH]", b.ToString());
        b = b.Xor(129);
        Assert.Equal(a, b);

        Assert.Equal(a, a.Xor(129).Xor(1).Xor(128));

        b = new UnknownValueBitTracker(TypeDB.Byte, 1);
        Assert.Equal("UnknownValueBitTracker<byte>[…]", a.Xor(b).ToString());

        b = a.SetBit(0, -1).SetBit(1, 1).SetBit(2, 0);
        Assert.Equal("UnknownValueBitTracker<byte>[abcde01_]", b.ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[00000fG_]", a.Xor(b).ToString());
    }

    [Fact]
    public void Test_Add()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(a.ShiftLeft(1), a.Add(a));

        var a2 = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(a.ShiftLeft(1), a.Add(a2));
        Assert.Equal(a.ShiftLeft(1), a2.Add(a));

        var b = new UnknownValueBitTracker(TypeDB.Byte, 1);
        Assert.NotEqual(a.ShiftLeft(1), a.Add(b));
        Assert.NotEqual(a.ShiftLeft(1), b.Add(a));
    }

    [Fact]
    public void Test_TypedAdd_BT()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(a.ShiftLeft(1), a.TypedAdd(a));

        var b = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(a.ShiftLeft(1), a.TypedAdd(b));

        var c = new UnknownValueBitTracker(TypeDB.Byte, 1);
        Assert.True(a.TypedAdd(c).IsFullRange());
    }

    [Fact]
    public void Test_TypedAdd_const()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(a, a.TypedAdd(0));
        Assert.Equal("UnknownValueBitTracker<byte>[…H]", a.TypedAdd(1).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…Gh]", a.TypedAdd(2).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…H]", a.TypedAdd(3).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…Fgh]", a.TypedAdd(4).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…H]", a.TypedAdd(5).ToString());

        var b = a.BitwiseAnd(0b11100111) as UnknownValueBitTracker;
        Assert.NotNull(b);
        Assert.Equal("UnknownValueBitTracker<byte>[abc00fgh]", b.ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[abc0___H]", b.TypedAdd(1).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[abc0__Gh]", b.TypedAdd(2).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[abc0___H]", b.TypedAdd(3).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[abc0fFgh]", b.TypedAdd(4).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[abc0___H]", b.TypedAdd(5).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[abc01fgh]", b.TypedAdd(8).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[abc10fgh]", b.TypedAdd(16).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…C00fgh]", b.TypedAdd(32).ToString());
    }

    [Fact]
    public void Test_TypedMul_byte_const()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(a, a.TypedMul(1));
        Assert.Equal("UnknownValueBitTracker<byte>[bcdefgh0]", a.TypedMul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[cdefgh00]", a.TypedMul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[h0000000]", a.TypedMul(128).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[00000000]", a.TypedMul(256).ToString());

        Assert.Equal("UnknownValueBitTracker<byte>[…h]", a.TypedMul(3).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…fgh]", a.TypedMul(25).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…fgh00]", a.TypedMul(100).ToString());

        Assert.Equal("UnknownValueBitTracker<byte>[…h0]", a.TypedMul(2 + 4).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…h0]", a.TypedMul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…h0]", a.TypedMul(2 + 4 + 8 + 16).ToString());
    }

    [Fact]
    public void Test_Mul_byte_const()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(a, a.Mul(1));
        Assert.Equal("UnknownValueBitTracker<byte>[bcdefgh0]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[cdefgh00]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[h0000000]", a.Mul(128).ToString());
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Byte), a.Mul(256));

        Assert.Equal("UnknownValueBitTracker<byte>[…h]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…fgh]", a.Mul(25).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…fgh00]", a.Mul(100).ToString());

        Assert.Equal("UnknownValueBitTracker<byte>[…h0]", a.Mul(2 + 4).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…h0]", a.Mul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[…h0]", a.Mul(2 + 4 + 8 + 16).ToString());
    }

    [Fact]
    public void Test_TypedMul_int_const()
    {
        var a = new UnknownValueBitTracker(TypeDB.Int, 0);
        Assert.Equal(a, a.TypedMul(1));
        Assert.Equal("UnknownValueBitTracker<int>[bcdefghijklmnopqrstuvwxyzабвгде0]", a.TypedMul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[cdefghijklmnopqrstuvwxyzабвгде00]", a.TypedMul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[е0000000000000000000000000000000]", a.TypedMul(0x80000000).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[00000000000000000000000000000000]", a.TypedMul(0x100000000L).ToString());

        Assert.Equal("UnknownValueBitTracker<int>[…е]", a.TypedMul(3).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…где]", a.TypedMul(25).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…где00]", a.TypedMul(100).ToString());

        Assert.Equal("UnknownValueBitTracker<int>[…е0]", a.TypedMul(2 + 4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…е0]", a.TypedMul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…е0]", a.TypedMul(2 + 4 + 8 + 16).ToString());

        Assert.Equal("UnknownValueBitTracker<int>[…е000000000000000000000]", a.TypedMul(0xB5E00000).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…е000000000000000000000]", a.TypedMul(-1243611136).ToString());
    }

    [Fact]
    public void Test_Mul_int_const()
    {
        var a = new UnknownValueBitTracker(TypeDB.Int, 0);
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

    [Fact]
    public void Test_Negate()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        var b = a.Negate();
        Assert.Equal("UnknownValueBitTracker<byte>[…h]", b.ToString());
        var c = b.Negate();
        Assert.Equal("UnknownValueBitTracker<byte>[…h]", c.ToString());

        a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(1, 127));
        Assert.Equal("UnknownValueBitTracker<byte>[0bcdefg1]", a.ToString());
        b = a.Negate();
        Assert.Equal("UnknownValueBitTracker<byte>[1BCDEFG1]", b.ToString());
        c = b.Negate();
        Assert.Equal("UnknownValueBitTracker<byte>[0bcdefg1]", c.ToString());
    }
}
