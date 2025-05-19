using Xunit;

public class UnknownValueTests
{
    [Fact]
    public void Test_Add()
    {
        UnknownValue a = new();
        UnknownValue b = new();

        Assert.True((a + b) is UnknownValue);
    }

    [Fact]
    public void Test_ToString()
    {
        UnknownValue a = new();
        Assert.Equal("UnknownValue<>", a.ToString());
    }
}
