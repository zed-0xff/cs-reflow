using Xunit;

public class UnknownValueBitTrackerTest
{
    IEnumerable<int> gen_range(int start, int end) => Enumerable.Range(start, end - start + 1);

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
        Assert.Equal("UnknownValueBitTracker<byte>[00000000]", a.Xor(a).ToString());
        var b = a.Xor(129);
        Assert.Equal("UnknownValueBitTracker<byte>[AbcdefgH]", b.ToString());
        b = b.Xor(129);
        Assert.Equal(a, b);

        Assert.Equal(a, a.Xor(129).Xor(1).Xor(128));

        b = new UnknownValueBitTracker(TypeDB.Byte, 1);
        Assert.Equal("UnknownValueBitTracker<byte>[]", a.Xor(b).ToString());

        b = a.SetBit(0, -1).SetBit(1, 1).SetBit(2, 0);
        Assert.Equal("UnknownValueBitTracker<byte>[abcde01_]", b.ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[00000fG_]", a.Xor(b).ToString());
    }
}
