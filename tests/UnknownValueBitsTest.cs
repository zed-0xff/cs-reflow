using Xunit;

public class UnknownValueBitsTest
{
    [Fact]
    public void Test_ToString()
    {
        var a = new UnknownValueBits("byte", new sbyte[] { -1, 1, 0, -1, -1, -1, -1, -1 });
        Assert.Equal("UnknownValueBits<byte>[01_]", a.ToString());

        a = new UnknownValueBits("byte");
        Assert.Equal("UnknownValueBits<byte>[]", a.ToString());
    }

    [Fact]
    public void Test_Min_byte()
    {
        var a = new UnknownValueBits("byte");
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

        a = new UnknownValueBits("byte", new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(0, a.Min());

        a = new UnknownValueBits("byte", new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(255, a.Min());
    }

    [Fact]
    public void Test_Min_sbyte()
    {
        var a = new UnknownValueBits("sbyte");
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

        a = new UnknownValueBits("sbyte");
        a.SetBit(7, 0);
        Assert.Equal(0, a.Min());

        a = new UnknownValueBits("sbyte", new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(0, a.Min());

        a = new UnknownValueBits("sbyte", new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(-1, a.Min());

        a = new UnknownValueBits("sbyte", new sbyte[] { -1, -1, -1, -1, -1, -1, -1, 1 });
        Assert.Equal(-128, a.Min());

        a = new UnknownValueBits("sbyte", new sbyte[] { -1, -1, -1, -1, -1, -1, -1, 0 });
        Assert.Equal(0, a.Min());
    }

    [Fact]
    public void Test_Max_byte()
    {
        var a = new UnknownValueBits("byte");
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

        a = new UnknownValueBits("byte", new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(0, a.Max());

        a = new UnknownValueBits("byte", new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(255, a.Max());
    }

    [Fact]
    public void Test_Max_sbyte()
    {
        var a = new UnknownValueBits("sbyte");
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

        a = new UnknownValueBits("sbyte", new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(0, a.Max());

        a = new UnknownValueBits("sbyte", new sbyte[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Assert.Equal(-1, a.Max());

        a = new UnknownValueBits("sbyte", new sbyte[] { -1, -1, -1, -1, -1, -1, -1, 1 });
        Assert.Equal(-1, a.Max());

        a = new UnknownValueBits("sbyte", new sbyte[] { -1, -1, -1, -1, -1, -1, -1, 0 });
        Assert.Equal(127, a.Max());
    }

    [Fact]
    public void Test_ShiftLeft()
    {
        var a = new UnknownValueBits("byte");
        var b = a.ShiftLeft(2);
        Assert.Equal("UnknownValueBits<byte>[00]", b.ToString());
    }

    [Fact]
    public void Test_BitwiseAnd()
    {
        var a = new UnknownValueBits("byte", new sbyte[] { 0, 1, -1, -1, -1, -1, -1, -1 });
        var b = a.BitwiseAnd(7);
        Assert.Equal("UnknownValueBits<byte>[00000_10]", b.ToString());
    }

    [Fact]
    public void Test_Add()
    {
        var a = new UnknownValueBits("byte");
        Assert.Equal(a, a.Add(0));
        Assert.Equal(a, a.Add(7));
        Assert.Equal(a, a.Add(255));

        a = new UnknownValueBits("byte", new sbyte[] { 0, 1, -1, -1, 1, 0, 1, -1 });
        Assert.Equal(a, a.Add(0));
        Assert.Equal("UnknownValueBits<byte>[11]", a.Add(1).ToString());
        Assert.Equal("UnknownValueBits<byte>[00]", a.Add(2).ToString());
        Assert.Equal("UnknownValueBits<byte>[01]", a.Add(3).ToString());
        Assert.Equal("UnknownValueBits<byte>[01]", a.Add(7).ToString());
    }

    [Fact]
    public void Test_And_Or()
    {
        var a = UnknownValueBits.CreateFromAnd(UnknownTypedValue.GetType("int"), -265);
        Assert.Equal("UnknownValueBits<int>[0____0___]", a.ToString());

        var b = a.BitwiseOr(0x82);
        Assert.Equal("UnknownValueBits<int>[01___0_1_]", b.ToString());
    }
}
