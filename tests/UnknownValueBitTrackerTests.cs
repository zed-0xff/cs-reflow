using Xunit;

public class UnknownValueBitTrackerTests
{
    IEnumerable<int> gen_range(int start, int end) => Enumerable.Range(start, end - start + 1);

    [Fact]
    public void Test_ctor()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal("UnknownValueBitTracker<byte>[hgfedcba]", a.ToString());

        a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(0, 254));
        Assert.Equal("UnknownValueBitTracker<byte>[hgfedcb0]", a.ToString());

        a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(1, 255));
        Assert.Equal("UnknownValueBitTracker<byte>[hgfedcb1]", a.ToString());

        a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(1, 127));
        Assert.Equal("UnknownValueBitTracker<byte>[0gfedcb1]", a.ToString());
    }

    [Fact]
    public void Test_cr_from_signed_range()
    {
        var a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.SByte).SignedShiftRight(1).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<int>[…gfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.SByte).Upcast(TypeDB.Int).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<int>[…hgfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.Int, -256, 255).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<int>[…ihgfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.Short).Upcast(TypeDB.Int).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<int>[…ponmlkjihgfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.Int).Upcast(TypeDB.Int).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<int>[едгвбаzyxwvutsrqponmlkjihgfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.Int).Upcast(TypeDB.Long).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<long>[…едгвбаzyxwvutsrqponmlkjihgfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.Long).Upcast(TypeDB.Long).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<long>[ûôîêâяюэьыъщшчцхфутсрпонмлкйизжёедгвбаzyxwvutsrqponmlkjihgfedcba]", a.ToString());
    }

    [Fact]
    public void Test_cr_from_unsigned_range()
    {
        var a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.Byte).UnsignedShiftRight(1).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<int>[…0gfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.Byte).Upcast(TypeDB.Int).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.Int, 0, 511).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<int>[…0ihgfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.UShort).Upcast(TypeDB.Int).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<int>[…0ponmlkjihgfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.UInt).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<uint>[едгвбаzyxwvutsrqponmlkjihgfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.UInt).Upcast(TypeDB.Long).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<long>[…0едгвбаzyxwvutsrqponmlkjihgfedcba]", a.ToString());

        a = new UnknownValueBitTracker((UnknownTypedValue)new UnknownValueRange(TypeDB.ULong).WithVarID(0));
        Assert.Equal("UnknownValueBitTracker<ulong>[ûôîêâяюэьыъщшчцхфутсрпонмлкйизжёедгвбаzyxwvutsrqponmlkjihgfedcba]", a.ToString());
    }

    [Fact]
    public void Test_Bits()
    {
        // lists are reversed to keep visually natural order, i.e. "hgfedcba" instead of "hgfedcba"

        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(gen_range(2, 9), a.Bits);
        a = a.BitwiseNot();
        Assert.Equal(gen_range(2, 9).Select(x => -x), a.Bits);

        a = new UnknownValueBitTracker(TypeDB.Byte, 1);
        Assert.Equal(gen_range(10, 17), a.Bits);

        a = new UnknownValueBitTracker(TypeDB.Int, 0);
        Assert.Equal(gen_range(2, 33), a.Bits);

        a = new UnknownValueBitTracker(TypeDB.Int, 1);
        Assert.Equal(gen_range(34, 65), a.Bits);
    }

    [Fact]
    public void Test_ToString()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal("UnknownValueBitTracker<byte>[hgfedcba]", a.ToString());

        a = a.BitwiseNot();
        Assert.Equal("UnknownValueBitTracker<byte>[HGFEDCBA]", a.ToString());

        a = new UnknownValueBitTracker(TypeDB.Byte, 1);
        Assert.Equal("UnknownValueBitTracker<byte>[ponmlkji]", a.ToString());

        a = new UnknownValueBitTracker(TypeDB.UShort, 0);
        Assert.Equal("UnknownValueBitTracker<ushort>[ponmlkjihgfedcba]", a.ToString());
    }

    [Fact]
    public void Test_ShiftLeft()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        var b = a.ShiftLeft(1);
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba0]", b.ToString());

        b = a.ShiftLeft(2);
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba00]", b.ToString());
    }

    [Fact]
    public void Test_SignedShiftRight_Byte()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        var b = a.SignedShiftRight(1);
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcb]", b.ToString());

        b = a.SignedShiftRight(2);
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedc]", b.ToString());
    }

    [Fact]
    public void Test_SignedShiftRight_SByte()
    {
        var a = new UnknownValueBitTracker(TypeDB.SByte, 0);
        var b = a.SignedShiftRight(1);
        Assert.Equal("UnknownValueBitTracker<int>[…hgfedcb]", b.ToString());

        b = a.SignedShiftRight(2);
        Assert.Equal("UnknownValueBitTracker<int>[…hgfedc]", b.ToString());
    }

    [Fact]
    public void Test_UnsignedShiftRight_Byte()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        var b = a.UnsignedShiftRight(1);
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcb]", b.ToString());

        b = a.UnsignedShiftRight(2);
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedc]", b.ToString());
    }

    [Fact]
    public void Test_UnsignedShiftRight_SByte()
    {
        var a = new UnknownValueBitTracker(TypeDB.SByte, 0);
        var b = a.UnsignedShiftRight(1);
        Assert.Equal("UnknownValueBitTracker<int>[0hhhhhhhhhhhhhhhhhhhhhhhhhgfedcb]", b.ToString());

        b = a.UnsignedShiftRight(2);
        Assert.Equal("UnknownValueBitTracker<int>[00hhhhhhhhhhhhhhhhhhhhhhhhhgfedc]", b.ToString());
    }

    [Fact]
    public void Test_BitwiseNot()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        var b = a.BitwiseNot();
        Assert.Equal("UnknownValueBitTracker<byte>[HGFEDCBA]", b.ToString());

        b = b.BitwiseNot();
        Assert.Equal("UnknownValueBitTracker<byte>[hgfedcba]", b.ToString());
        Assert.Equal(a, b);

        a = new UnknownValueBitTracker(TypeDB.Byte, 0).SetBit(1, -1).SetBit(7, 0).SetBit(0, 1);
        Assert.Equal("UnknownValueBitTracker<byte>[0gfedc_1]", a.ToString());

        b = a.BitwiseNot();
        Assert.Equal("UnknownValueBitTracker<byte>[1GFEDC_0]", b.ToString());

        b = b.BitwiseNot();
        Assert.Equal(a, b);
    }

    [Fact]
    public void Test_Xor()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Int), a.Xor(a));
        var b = a.Xor(129);
        Assert.Equal("UnknownValueBitTracker<int>[…0HgfedcbA]", b.ToString());
        b = b.Xor(129);
        Assert.Equal(a.Upcast(TypeDB.Int), b);

        Assert.Equal(a.Upcast(TypeDB.Int), a.Xor(129).Xor(1).Xor(128));

        b = new UnknownValueBitTracker(TypeDB.Byte, 1);
        Assert.Equal("UnknownValueBitTracker<int>[…0________]", a.Xor(b).ToString());

        b = a.SetBit(0, -1).SetBit(1, 1).SetBit(2, 0);
        Assert.Equal("UnknownValueBitTracker<byte>[hgfed01_]", b.ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0cB_]", a.Xor(b).ToString());
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
        var a = new UnknownValueBitTracker(TypeDB.Int, 0);
        Assert.Equal(a.ShiftLeft(1), a.TypedAdd(a));

        var b = new UnknownValueBitTracker(TypeDB.Int, 0);
        Assert.Equal(a.ShiftLeft(1), a.TypedAdd(b));

        var c = new UnknownValueBitTracker(TypeDB.Int, 1);
        Assert.True(a.TypedAdd(c).IsFullRange());
    }

    [Fact]
    public void Test_TypedAdd_const()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(a, a.TypedAdd(0));
        Assert.Equal("UnknownValueBitTracker<byte>[_______A]", a.TypedAdd(1).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[______Ba]", a.TypedAdd(2).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[_______A]", a.TypedAdd(3).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[_____Cba]", a.TypedAdd(4).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[_______A]", a.TypedAdd(5).ToString());

        var b = a.BitwiseAnd(0b11100111) as UnknownValueBitTracker;
        Assert.NotNull(b);
        Assert.Equal("UnknownValueBitTracker<int>[…0hgf00cba]", b.ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgf0___A]", b.TypedAdd(1).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgf0__Ba]", b.TypedAdd(2).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgf0___A]", b.TypedAdd(3).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgf0cCba]", b.TypedAdd(4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgf0___A]", b.TypedAdd(5).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgf01cba]", b.TypedAdd(8).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgf10cba]", b.TypedAdd(16).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0___F00cba]", b.TypedAdd(32).ToString());
    }

    [Fact]
    public void Test_TypedMul_byte_const()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(a, a.TypedMul(1));
        Assert.Equal("UnknownValueBitTracker<byte>[gfedcba0]", a.TypedMul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[fedcba00]", a.TypedMul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[a0000000]", a.TypedMul(128).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[00000000]", a.TypedMul(256).ToString());

        Assert.Equal("UnknownValueBitTracker<byte>[_______a]", a.TypedMul(3).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[_____cba]", a.TypedMul(25).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[___cba00]", a.TypedMul(100).ToString());

        Assert.Equal("UnknownValueBitTracker<byte>[______a0]", a.TypedMul(2 + 4).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[______a0]", a.TypedMul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBitTracker<byte>[______a0]", a.TypedMul(2 + 4 + 8 + 16).ToString());
    }

    [Fact]
    public void Test_Mul_byte_const()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba]", a.Mul(1).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba0]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba00]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba0000000]", a.Mul(128).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0hgfedcba00000000]", a.Mul(256).ToString());

        Assert.Equal("UnknownValueBitTracker<int>[…0_________a]", a.Mul(3).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0__________cba]", a.Mul(25).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0__________cba00]", a.Mul(100).ToString());

        Assert.Equal("UnknownValueBitTracker<int>[…0_________a0]", a.Mul(2 + 4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0__________a0]", a.Mul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0___________a0]", a.Mul(2 + 4 + 8 + 16).ToString());
    }

    [Fact]
    public void Test_TypedMul_int_const()
    {
        var a = new UnknownValueBitTracker(TypeDB.Int, 0);
        Assert.Equal(a, a.TypedMul(1));
        Assert.Equal("UnknownValueBitTracker<int>[дгвбаzyxwvutsrqponmlkjihgfedcba0]", a.TypedMul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[гвбаzyxwvutsrqponmlkjihgfedcba00]", a.TypedMul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[a0000000000000000000000000000000]", a.TypedMul(0x80000000).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0]", a.TypedMul(0x100000000L).ToString()); // not normalized

        Assert.Equal("UnknownValueBitTracker<int>[…_a]", a.TypedMul(3).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…_cba]", a.TypedMul(25).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…_cba00]", a.TypedMul(100).ToString());

        Assert.Equal("UnknownValueBitTracker<int>[…_a0]", a.TypedMul(2 + 4).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…_a0]", a.TypedMul(2 + 4 + 8).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…_a0]", a.TypedMul(2 + 4 + 8 + 16).ToString());

        Assert.Equal("UnknownValueBitTracker<int>[…_a000000000000000000000]", a.TypedMul(0xB5E00000).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…_a000000000000000000000]", a.TypedMul(-1243611136).ToString());
    }

    [Fact]
    public void Test_Mul_int_const()
    {
        var a = new UnknownValueBitTracker(TypeDB.Int, 0);
        Assert.Equal(a, a.Mul(1));
        Assert.Equal("UnknownValueBitTracker<int>[дгвбаzyxwvutsrqponmlkjihgfedcba0]", a.Mul(2).ToString());
        Assert.Equal("UnknownValueBitTracker<int>[гвбаzyxwvutsrqponmlkjihgfedcba00]", a.Mul(4).ToString());
        Assert.Equal("UnknownValueBitTracker<long>[еедгвбаzyxwvutsrqponmlkjihgfedcba0000000000000000000000000000000]", a.Mul(0x80000000).ToString()); // int * uint => long
        Assert.Equal(UnknownTypedValue.Zero(TypeDB.Long), a.Mul(0x100).Mul(0x4000_0000_0000_0000L));

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
    public void Test_Negate()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        var b = a.Negate();
        Assert.Equal("UnknownValueBitTracker<int>[…0________a]", b.ToString());
        var c = b.Negate();
        Assert.Equal("UnknownValueBitTracker<int>[…_a]", c.ToString()); // XXX actually 24 top bits would all be same value here, like 'xxxxxxxxxxx'

        a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(1, 127));
        Assert.Equal("UnknownValueBitTracker<byte>[0gfedcb1]", a.ToString());
        b = a.Negate();
        Assert.Equal("UnknownValueBitTracker<int>[…01GFEDCB1]", b.ToString());
        c = b.Negate();
        Assert.Equal("UnknownValueBitTracker<int>[…10gfedcb1]", c.ToString());
    }

    [Fact]
    public void Test_Upcast()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(1, 127));
        Assert.Equal("UnknownValueBitTracker<byte>[0gfedcb1]", a.ToString());
        Assert.Equal("UnknownValueBitTracker<int>[…0gfedcb1]", a.Upcast(TypeDB.Int).ToString());
        Assert.Equal("UnknownValueBitTracker<uint>[…0gfedcb1]", a.Upcast(TypeDB.UInt).ToString());

        var b = new UnknownValueBitTracker(TypeDB.Int, 0);
        Assert.Equal("UnknownValueBitTracker<long>[…едгвбаzyxwvutsrqponmlkjihgfedcba]", b.Upcast(TypeDB.Long).ToString());
    }

    [Fact]
    public void Test_Cardinality()
    {
        Assert.Equal(256UL, new UnknownValueBitTracker(TypeDB.Byte, 0).Cardinality().ulValue);
        Assert.Equal(256UL, new UnknownValueBitTracker(TypeDB.SByte, 0).Cardinality().ulValue);
        Assert.Equal(1UL + uint.MaxValue, new UnknownValueBitTracker(TypeDB.Int, 0).Cardinality().ulValue);
        Assert.Equal(1UL + uint.MaxValue, new UnknownValueBitTracker(TypeDB.UInt, 0).Cardinality().ulValue);

        Assert.Equal(1UL, new UnknownValueBitTracker(TypeDB.Int, 0, new BitSpan(0, 0)).Cardinality().ulValue);
        Assert.Equal(2UL, new UnknownValueBitTracker(TypeDB.Int, 0, new BitSpan(0, 1)).Cardinality().ulValue);
        Assert.Equal(2UL, new UnknownValueBitTracker(TypeDB.Int, 0, new BitSpan(0, 2)).Cardinality().ulValue);
        Assert.Equal(4UL, new UnknownValueBitTracker(TypeDB.Int, 0, new BitSpan(0, 3)).Cardinality().ulValue);

        var a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(0, 1));
        var b = a.BitwiseOr(a.ShiftLeft(1));
        Assert.Equal("UnknownValueBitTracker<int>[…0aa]", b.ToString());
        Assert.Equal(2UL, b.Cardinality().ulValue); // same bit repeated

        var c = new UnknownValueBitTracker(TypeDB.Byte, 1, new BitSpan(0, 1)); // same bits, different variable
        var d = a.BitwiseOr(c.ShiftLeft(1));
        Assert.Equal("UnknownValueBitTracker<int>[…0ia]", d.ToString());
        Assert.Equal(4UL, d.Cardinality().ulValue); // bits from both variables
    }

    [Fact]
    public void Test_Values()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(0, 3));
        Assert.Equal(new[] { 0L, 1, 2, 3 }, a.Values().ToArray());

        a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(0, 1));
        var b = a.BitwiseOr(a.ShiftLeft(1));
        Assert.Equal(new[] { 0L, 3 }, b.Values().ToArray().OrderBy(x => x));

        b = a.BitwiseOr(a.BitwiseNot().BitwiseAnd(1).ShiftLeft(1));
        Assert.Equal(new[] { 1L, 2 }, b.Values().ToArray().OrderBy(x => x));

        b = b.BitwiseOr(0x80);
        Assert.Equal("UnknownValueBitTracker<int>[…0100000Aa]", b.ToString());
        Assert.Equal(new[] { 0x81L, 0x82 }, b.Values().ToArray().OrderBy(x => x));

        b = b.BitwiseOr(new UnknownValueSet(TypeDB.Int, new[] { 0L, 0x100 }));
        Assert.Equal("UnknownValueBitTracker<int>[…0_100000Aa]", b.ToString());
        Assert.Equal(new[] { 0x81L, 0x82, 0x181, 0x182 }, b.Values().ToArray().OrderBy(x => x));

        var c = new UnknownValueBitTracker(TypeDB.Byte, 0);
        Assert.Equal(0, c.Values().OrderBy(x => x).First());
        Assert.Equal(255, c.Values().OrderBy(x => x).Last());
        Assert.Equal(c.Cardinality().ulValue, (ulong)c.Values().Count());

        c = new UnknownValueBitTracker(TypeDB.SByte, 0);
        Assert.Equal(-128, c.Values().OrderBy(x => x).First());
        Assert.Equal(127, c.Values().OrderBy(x => x).Last());
        Assert.Equal(c.Cardinality().ulValue, (ulong)c.Values().Count());
    }

    [Fact]
    public void Test_Contains()
    {
        var a = new UnknownValueBitTracker(TypeDB.Byte, 0, new BitSpan(0, 1));
        var b = a.BitwiseOr(a.ShiftLeft(1));
        Assert.True(b.Contains(0));
        Assert.False(b.Contains(1));
        Assert.False(b.Contains(2));
        Assert.True(b.Contains(3));

        var c = a.BitwiseOr(a.BitwiseNot().BitwiseAnd(1).ShiftLeft(1));
        Assert.False(c.Contains(0));
        Assert.True(c.Contains(1));
        Assert.True(c.Contains(2));
        Assert.False(c.Contains(3));

        var d = c.BitwiseOr(0x80);
        Assert.False(d.Contains(0));
        Assert.False(d.Contains(1));
        Assert.False(d.Contains(2));
        Assert.False(d.Contains(3));

        Assert.False(d.Contains(0x80));
        Assert.True(d.Contains(0x81));
        Assert.True(d.Contains(0x82));
        Assert.False(d.Contains(0x83));

        a = new UnknownValueBitTracker(TypeDB.Byte, 0);
        for (int i = byte.MinValue; i <= byte.MaxValue; i++)
            Assert.True(a.Contains(i));
        Assert.False(a.Contains(byte.MinValue - 1));
        Assert.False(a.Contains(byte.MaxValue + 1));
        Assert.False(a.Contains(-1));

        a = new UnknownValueBitTracker(TypeDB.SByte, 0);
        for (int i = sbyte.MinValue; i <= sbyte.MaxValue; i++)
            Assert.True(a.Contains(i));
        Assert.False(a.Contains(sbyte.MinValue - 1));
        Assert.False(a.Contains(sbyte.MaxValue + 1));
    }
}
