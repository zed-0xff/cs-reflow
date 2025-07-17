using Xunit;

public partial class ExpressionTests
{
    [Fact]
    public void Test_arr_len_int()
    {
        var a = new int[10];
        check_expr("var a = new int[10]; a.Length", a.Length);

        a = new int[] { 1, 2, 3 };
        check_expr("var a = new int[] { 1, 2, 3 }; a.Length", a.Length);
    }

    [Fact]
    public void Test_arr_len_string()
    {
        var a = new string[10];
        check_expr("var a = new string[10]; a.Length", a.Length);

        a = new string[] { "foo", "bar", "baz" };
        check_expr("var a = new string[] { \"foo\", \"bar\", \"baz\" }; a.Length", a.Length);
    }

    [Fact]
    public void Test_arr_len_unk()
    {
        check_expr("var a = new Foo[10]; a.Length", 10);
        check_expr("var a = new Foo[] { asd, fgh, jkl }; a.Length", 3);
    }

    [Fact]
    public void Test_arr_get()
    {
        check_expr("var a = new int[3]; a[0]", 0);
        check_expr("var a = new int[3]; a[1]", 0);
        check_expr("var a = new int[3]; a[2]", 0);

        check_expr("var a = new int[] { 1, 1+1, 3 }; a[0]", 1);
        check_expr("var a = new int[] { 1, 1+1, 3 }; a[1]", 2);
        check_expr("var a = new int[] { 1, 1+1, 3 }; a[2]", 3);
    }

    [Fact]
    public void Test_arr_set()
    {
        check_expr("var a = new int[3]; a[1] = 4; a[0]", 0);
        check_expr("var a = new int[3]; a[1] = 4; a[1]", 4);
        check_expr("var a = new int[3]; a[1] = 4; a[2]", 0);

        check_expr("var a = new int[3]; a[1] += 4; a[0]", 0);
        check_expr("var a = new int[3]; a[1] += 4; a[1]", 4);
        check_expr("var a = new int[3]; a[1] += 4; a[2]", 0);

        check_expr("var a = new int[]{ 1, 2, 3 }; a[1] += 4; a[0]", 1);
        check_expr("var a = new int[]{ 1, 2, 3 }; a[1] += 4; a[1]", 6);
        check_expr("var a = new int[]{ 1, 2, 3 }; a[1] += 4; a[2]", 3);
    }

    [Fact]
    public void Test_arr_set_unk_idx()
    {
        Eval("var a = new int[3]; a[x] = 4");

        var V = _varDB.FindByName("a");
        var arr = _varDict[V!.id] as ArrayWrap;
        Assert.NotNull(arr);
        Assert.Equal(3, arr.Length);
        Assert.Equal(new UnknownValueSet(TypeDB.Int, new() { 0, 4 }), arr[0]);
    }

    [Fact]
    public void Test_arr_set_with_cast_known_idx()
    {
        Eval("var a = new char[10]; int i=4; ((short[])a)[i] = 123;");

        var V = _varDB.FindByName("a");
        var arr = _varDict[V!.id] as ArrayWrap;
        Assert.NotNull(arr);
        Assert.Equal((char)123, arr[4]);
    }

    [Fact]
    public void Test_arr_set_with_cast_unknown_idx()
    {
        Eval("var a = new char[4]; int i; ((short[])a)[i] = 123;");

        var V = _varDB.FindByName("a");
        var arr = _varDict[V!.id] as ArrayWrap;
        Assert.NotNull(arr);
        Assert.Equal(4, arr.Length);
        Assert.Equal(new UnknownValueSet(TypeDB.Char, new() { 0, 123 }), arr[0]);
    }

    [Fact]
    public void Test_arr_unk_len()
    {
        check_expr("var a = new int[n]; a", UnknownValue.Create());
    }
}
