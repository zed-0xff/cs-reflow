using Xunit;

public class DefaultDictTests
{
    class Foo
    {
    }

    [Fact]
    public void DefaultDict_int()
    {
        var dict = new DefaultDict<int, int>();
        Assert.NotNull(dict);
        Assert.Empty(dict);
        Assert.Equal(0, dict[111]);
        Assert.Equal(0, dict[222]);
        dict[111]++;
        Assert.Equal(1, dict[111]);
        Assert.Equal(0, dict[222]); // still 0
    }

    [Fact]
    public void DefaultDict_class()
    {
        var dict = new DefaultDict<int, Foo>();
        Assert.NotNull(dict);
        Assert.Empty(dict);
        Assert.IsType<Foo>(dict[111]);
        Assert.NotEqual(dict[111], dict[222]); // different instances
    }
}
