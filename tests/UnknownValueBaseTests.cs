using Xunit;

public class UnknownValueBaseTests
{
    long? Convert(object? value)
    {
        if (UnknownValueBase.TryConvertToLong(value, out long result))
            return result;
        return null;
    }

    [Fact]
    public void Test_TryConvertToLong()
    {
        Assert.Equal((long)42, Convert(42));
        Assert.Equal((long)-1, Convert(-1));
        Assert.Equal((long)-1, Convert((sbyte)-1));
        Assert.Equal((long)-1, Convert((long)-1));
        Assert.Equal((long)-1, Convert((short)-1));
        Assert.Null(Convert("test"));
        Assert.Null(Convert(null));
    }
}
