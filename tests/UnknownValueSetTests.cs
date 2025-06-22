using Xunit;

public class UnknownValueSetTests
{
    [Fact]
    public void Test_ToString()
    {
        UnknownValueSet a = new(TypeDB.UInt);
        Assert.Equal("UnknownValueSet<uint>{}", a.ToString());

        UnknownValueSet b = new(TypeDB.Int, new List<long> { 1, 2, 3 });
        Assert.Equal("UnknownValueSet<int>{1, 2, 3}", b.ToString());
    }

    [Fact]
    public void Test_Cardinality()
    {
        UnknownValueSet a = new(TypeDB.UInt);
        Assert.Equal(0, a.Cardinality());

        UnknownValueSet b = new(TypeDB.Int, new List<long> { 1, 2, 3 });
        Assert.Equal(3, b.Cardinality());
    }

    [Fact]
    public void Test_Cast()
    {
        UnknownValueSet a = new(TypeDB.UInt, new List<long> { uint.MaxValue, uint.MaxValue - 1, uint.MaxValue - 2 });
        var b = a.Cast(TypeDB.Int) as UnknownValueSet;
        Assert.Equal(new List<long> { -1, -2, -3 }, b?.Values()?.ToList());
    }

    [Fact]
    public void Test_Cast_bool()
    {
        UnknownValueSet a = new(TypeDB.Int, new List<long> { 1, 2, 3 });
        var b = a.Cast(TypeDB.Bool);
        Assert.True(b is bool);
        Assert.True(b as bool?);

        a = new(TypeDB.Int, new List<long> { -1 });
        b = a.Cast(TypeDB.Bool);
        Assert.True(b is bool);
        Assert.True(b as bool?);

        a = new(TypeDB.Int, new List<long> { 0 });
        b = a.Cast(TypeDB.Bool);
        Assert.True(b is bool);
        Assert.False(b as bool?);

        a = new(TypeDB.Int, new List<long> { 0, 5 });
        Assert.Equal(UnknownValue.Create(TypeDB.Bool), a.Cast(TypeDB.Bool));

        a = new(TypeDB.Int, new List<long> { -1, 0 });
        Assert.Equal(UnknownValue.Create(TypeDB.Bool), a.Cast(TypeDB.Bool));
    }
}
