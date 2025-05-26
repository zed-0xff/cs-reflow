using Xunit;

public class TypeDBTests
{
    [Fact]
    public void Test_int()
    {
        var type = TypeDB.Int;
        Assert.Equal("int", type.Name);
        Assert.Equal(32, type.nbits);
        Assert.True(type.signed);
        Assert.Equal("int", type.ToString());
        Assert.Equal("int", TypeDB.ShortType("System.Int32"));
        Assert.Equal(int.MinValue, type.MinValue);
        Assert.Equal(int.MaxValue, type.MaxSignedValue);
        Assert.Equal((ulong)int.MaxValue, type.MaxUnsignedValue);

        Assert.True(type.CanFit(209));
        Assert.True(type.CanFit(-209));
        Assert.True(type.CanFit(int.MaxValue));
        Assert.True(type.CanFit(short.MaxValue));
        Assert.True(type.CanFit(int.MinValue));
        Assert.False(type.CanFit(uint.MaxValue));
    }

    [Fact]
    public void Test_uint()
    {
        var type = TypeDB.UInt;
        Assert.Equal("uint", type.Name);
        Assert.Equal(32, type.nbits);
        Assert.False(type.signed);
        Assert.Equal("uint", type.ToString());
        Assert.Equal("uint", TypeDB.ShortType("System.UInt32"));
        Assert.Equal(uint.MinValue, type.MinValue);
        Assert.Equal(uint.MaxValue, type.MaxSignedValue);
        Assert.Equal(uint.MaxValue, type.MaxUnsignedValue);

        Assert.True(type.CanFit(209));
        Assert.False(type.CanFit(-209));
        Assert.True(type.CanFit(int.MaxValue));
        Assert.True(type.CanFit(short.MaxValue));
        Assert.False(type.CanFit(int.MinValue));
        Assert.True(type.CanFit(uint.MaxValue));
    }

    [Fact]
    public void Test_long()
    {
        var type = TypeDB.Int64;
        Assert.Equal("long", type.Name);
        Assert.Equal(64, type.nbits);
        Assert.True(type.signed);
        Assert.Equal("long", type.ToString());
        Assert.Equal("long", TypeDB.ShortType("System.Int64"));
        Assert.Equal(long.MinValue, type.MinValue);
        Assert.Equal(long.MaxValue, type.MaxSignedValue);
        Assert.Equal((ulong)long.MaxValue, type.MaxUnsignedValue);

        Assert.True(type.CanFit(long.MaxValue));
        Assert.True(type.CanFit(int.MaxValue));
        Assert.True(type.CanFit(long.MinValue));
        Assert.False(type.CanFit(ulong.MaxValue));
    }

    [Fact]
    public void Test_ulong()
    {
        var type = TypeDB.UInt64;
        Assert.Equal("ulong", type.Name);
        Assert.Equal(64, type.nbits);
        Assert.False(type.signed);
        Assert.Equal("ulong", type.ToString());
        Assert.Equal("ulong", TypeDB.ShortType("System.UInt64"));
        Assert.Equal(ulong.MinValue, (ulong)type.MinValue);
        Assert.Equal(long.MaxValue, type.MaxSignedValue);
        Assert.Equal(ulong.MaxValue, type.MaxUnsignedValue);

        Assert.True(type.CanFit(ulong.MaxValue));
        Assert.False(type.CanFit(-1));
        Assert.False(type.CanFit(long.MinValue));
    }
}
