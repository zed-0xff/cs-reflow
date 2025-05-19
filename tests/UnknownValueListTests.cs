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
}
