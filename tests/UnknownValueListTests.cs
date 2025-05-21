using Xunit;

public class UnknownValueListTests
{
    [Fact]
    public void Test_ToString()
    {
        UnknownValueList a = new("uint");
        Assert.Equal("UnknownValue<uint>[0]", a.ToString());

        UnknownValueList b = new("int", new List<long> { 1, 2, 3 });
        Assert.Equal("UnknownValue<int>[3]", b.ToString());
    }

    [Fact]
    public void Test_Cardinality()
    {
        UnknownValueList a = new("uint");
        Assert.Equal(0UL, a.Cardinality());

        UnknownValueList b = new("int", new List<long> { 1, 2, 3 });
        Assert.Equal(3UL, b.Cardinality());
    }

    [Fact]
    public void Test_Cast()
    {
        UnknownValueList a = new("uint", new List<long> { uint.MaxValue, uint.MaxValue - 1, uint.MaxValue - 2 });
        a = a.Cast("int") as UnknownValueList;
        Assert.Equal(new List<long> { -1, -2, -3 }, a.Values().ToList());
    }

    [Fact]
    public void Test_Cast_bool()
    {
        UnknownValueList a = new("int", new List<long> { 1, 2, 3 });
        var b = a.Cast("bool");
        Assert.True(b is bool);
        Assert.True(b as bool?);

        a = new("int", new List<long> { 0 });
        b = a.Cast("bool");
        Assert.True(b is bool);
        Assert.False(b as bool?);

        a = new("int", new List<long> { 0, 5 });
        Assert.Equal(UnknownValue.Create("bool"), a.Cast("bool"));
    }
}
