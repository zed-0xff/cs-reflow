using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

public partial class ExpressionTests
{
    VarDB _varDB = new();
    VarDict _varDict;
    VarProcessor _processor;

    public ExpressionTests()
    {
        _varDict = new VarDict(_varDB);
        _processor = new VarProcessor(_varDB, varDict: _varDict);
        var v = Environment.GetEnvironmentVariable("REFLOW_VERBOSITY");
        if (v != null)
        {
            _processor.Verbosity = int.Parse(v);
        }
    }

    void AddVar(string name, object value)
    {
        string typeName = value switch
        {
            UnknownTypedValue ut => ut.Type.Name,
            _ => value.GetType().Name
        };
        var V = _varDB.Add(name, typeName);
        _varDict.Set(V.id, value);
    }

    object? GetVar(string name)
    {
        var V = _varDB.FindByName(name);
        // XXX should still query the _processor if the variable is not found?
        return V == null ? UnknownValue.Create() : _processor.VariableValues()[V.id];
    }

    object? Eval(string expr_str) => _processor.EvaluateString(expr_str);

    [Fact]
    public void Test_get_known_var_from_decl()
    {
        Eval("int x");
        Assert.Equal(UnknownValue.Create(TypeDB.Int), Eval("+x"));
    }

    [Fact]
    public void Test_get_known_var_from_db()
    {
        AddVar("x", UnknownValue.Create(TypeDB.Int));
        Assert.Equal(UnknownValue.Create(TypeDB.Int), Eval("+x"));
    }

    [Fact]
    public void Test_int_decl()
    {
        string stmt_str = "int x = 123";
        var result = Eval(stmt_str);
        Assert.IsType<int>(GetVar("x"));
        Assert.Equal(123, GetVar("x"));
    }

    [Fact]
    public void Test_int_unk_init()
    {
        string stmt_str = "int x = Foo.bar";
        try
        {
            Eval(stmt_str);
        }
        catch (NotSupportedException)
        {
        }
        Assert.Equal(UnknownValue.Create(TypeDB.Int), GetVar("x"));
    }

    [Fact]
    public void Test_int_unk_assign()
    {
        Eval("int x = 123");
        try
        {
            Eval("x = Foo.bar");
        }
        catch (NotSupportedException)
        {
        }
        Assert.Equal(UnknownValue.Create(TypeDB.Int), GetVar("x"));
    }

    [Fact]
    public void Test_unk_int_unk_assign()
    {
        string stmt_str = "x = Foo.bar";
        AddVar("x", UnknownValue.Create(TypeDB.Int));
        try
        {
            Eval(stmt_str);
        }
        catch (NotSupportedException)
        {
        }
        Assert.Equal(UnknownValue.Create(TypeDB.Int), GetVar("x"));
    }

    [Fact]
    public void Test_int_negate()
    {
        int x = 123;
        int y = ~x;
        Assert.Equal(-124, y);
        Assert.Equal(123, x);

        Eval("int x = 123");
        string stmt_str = "int y = ~x";
        var result = Eval(stmt_str);
        Assert.IsType<int>(GetVar("y"));
        Assert.Equal(-124, GetVar("y"));
        Assert.IsType<int>(GetVar("x"));
        Assert.Equal(123, GetVar("x"));
    }

    [Fact]
    public void Test_int_assign_existing()
    {
        string expr_str = "int x = 0; x = 123";

        var result = Eval(expr_str);
        Assert.IsType<int>(GetVar("x"));
        Assert.Equal(123, GetVar("x"));
    }

    [Fact]
    public void Test_int_or_assign_existing()
    {
        string expr_str = "int x=0; x |= 123";

        var result = Eval(expr_str);
        Assert.IsType<int>(GetVar("x"));
        Assert.Equal(123, GetVar("x"));
    }

    [Fact]
    public void Test_unk_assign()
    {
        string expr_str = "x = 123"; // x type is not known, maybe int, uint, long, etc.

        var result = Eval(expr_str);
        Assert.Equal(UnknownValue.Create(), result);
        Assert.Equal(UnknownValue.Create(), GetVar("x"));
    }

    [Fact]
    public void Test_uint_decl()
    {
        string stmt_str = "uint x = 123";

        var result = Eval(stmt_str);
        Assert.IsType<uint>(GetVar("x"));
        Assert.Equal(123U, GetVar("x"));
    }

    [Fact]
    public void Test_uint_assign_existing()
    {
        string expr_str = "var x = 0U; x = 123";

        var result = Eval(expr_str);
        Assert.IsType<uint>(GetVar("x"));
        Assert.Equal(123U, GetVar("x"));
    }

    [Fact]
    public void Test_uint_assign_new()
    {
        string expr_str = "var x = 123U";

        var result = Eval(expr_str);
        Assert.IsType<uint>(GetVar("x"));
        Assert.Equal(123U, GetVar("x"));
    }

    [Fact]
    public void Test_postIncr()
    {
        Eval("int x = 1");
        var result = Eval("x++");
        Assert.IsType<int>(result);
        Assert.Equal(1, result);
        Assert.IsType<int>(GetVar("x"));
        Assert.Equal(2, GetVar("x"));
    }

    [Fact]
    public void Test_preIncr()
    {
        string expr_str = "++x";

        AddVar("x", 1);
        var result = Eval(expr_str);
        Assert.IsType<int>(result);
        Assert.Equal(2, result);
        Assert.IsType<int>(GetVar("x"));
        Assert.Equal(2, GetVar("x"));
    }

    [Fact]
    public void Test_exprA()
    {
        string expr_str = "(0x359 ^ ((0x82E & num4) * (int)(num9 << 14))) == 0"; // always false

        AddVar("num4", UnknownValue.Create("int"));
        AddVar("num9", UnknownValue.Create("uint"));
        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprB()
    {
        string expr_str = "(num32 << 9) + 182816 == (int)(16 * (3036 + (num11 << 7)))";
        //string expr_str = "(num32 << 9) + 0x2ca20 == 0xbdc0 + (num11 << 11)";
        //string expr_str = "(num32 << 9) + 0x20c60 == num11 << 11";
        //string expr_str = "(num32 << 5) + 0x20c6  == num11 << 7";
        //                                    00110 == 0

        AddVar("num32", UnknownValue.Create("int"));
        AddVar("num11", UnknownValue.Create("uint"));
        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprC()
    {
        string expr_str = "((4 * num252 + num252 * 4) & 4) == 0";

        AddVar("num252", UnknownValue.Create("int"));
        var result = Eval(expr_str);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprD()
    {
        string expr_str = "0 == ((4 * (num10 & (num10 << 2))) & 4)";

        AddVar("num10", UnknownValue.Create("int"));
        var result = Eval(expr_str);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprE()
    {
        string expr_str = "(((0x300 & ((num13 * 1939 + 109 * num13) ^ ((0xC86 & num12) >>> 5))) == 0) ? (-5579) : ((num14 << 5) - -48224 != 32 * (16402 * idat2 - (num14 & 0x1A85)))";

        AddVar("num12", UnknownValue.Create("int"));
        AddVar("num13", UnknownValue.Create("int"));
        AddVar("num14", 0);
        var result = Eval(expr_str);
        Assert.IsType<int>(result);
        Assert.Equal(-5579, result);
    }

    [Fact]
    public void Test_exprF1()
    {
        string expr_str = "!(~num11 == num11)";

        AddVar("num11", UnknownValue.Create("int"));
        var result = Eval(expr_str);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprF2()
    {
        string expr_str = "!(~num11 != num11)";

        AddVar("num11", UnknownValue.Create("int"));
        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprF3()
    {
        string expr_str = "!(num11 == ~num11)";

        AddVar("num11", UnknownValue.Create("int"));
        var result = Eval(expr_str);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprF4()
    {
        string expr_str = "!(num11 != ~num11)";

        AddVar("num11", UnknownValue.Create("int"));
        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprG()
    {
        string expr_str = "int num12, num3; (((-(num12 + num12) << 1) ^ (10792 * num3 - 8444)) & 2) == 0";
        Assert.Equal(true, Eval(expr_str));
    }

    [Fact]
    public void Test_exprH()
    {
        string expr_str = "int num116, num117; ~(num116 + num116) != num117 * 3 + num117 - 708559999 >>> 1";
        Assert.Equal(true, Eval(expr_str));
    }

    [Fact]
    public void Test_exprI1()
    {
        string expr_str = "QRR3 * 134217728 - 1508900864 != (QRR2 * 22 + 10 * QRR2) * 262144";

        AddVar("QRR2", UnknownValue.Create("int"));
        AddVar("QRR3", UnknownValue.Create("int"));
        var result = Eval(expr_str);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprI2()
    {
        string expr_str = "QRR3 * 134217728 - 1508900864 != (QRR2 * 22 + 10 * QRR2) * 262144";

        AddVar("QRR2", UnknownValue.Create("int"));
        AddVar("QRR3", 0);
        var result = Eval(expr_str);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprJ()
    {
        string expr_str = "342177280 + (x >>> 11 >>> 1) != QRR11 >>> 4";

        AddVar("x", UnknownValue.Create("int"));
        Eval("int QRR11 = 0");
        var result = Eval(expr_str);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprK()
    {
        string expr_str = "(int)num8 - x * -1275068416 != -1135615528";

        AddVar("num8", new UnknownValueRange(TypeDB.UInt, 0, 1194));
        AddVar("x", UnknownValue.Create("int"));
        var result = Eval(expr_str);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprL()
    {
        string expr_str = "(((uint)(64 * (int)num) % 40u) | 0xABA40BBFu) != 2879654847u";

        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprM()
    {
        string expr_str = "(3 & ((uint)e093257623e347d2a45b8e4d5fa2 % 4558u << 15 >> 4)) != (uint)((124656 * (int)e093257623e347d2a45b8e4d5fa2) & 3)";

        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprN()
    {
        string expr_str = "int num,x; (int)((uint)x / 4u) - int.MinValue == (858510224 + (num << 8)) * int.MinValue";

        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprO_BitTracker()
    {
        string expr_str = "int num6; ((num6 ^ ((num6 * -1788084224) | (num6 - 400) | (num6 + num6))) & 1) != 0";

        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprP_BitTracker()
    {
        string expr_str = "int num6; (0x1DC240 & ((num6 * 1024 >>> 10) ^ num6)) != 0";

        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprQ_BitTracker()
    {
        string expr_str = "int x; -101875712 + 16384 * 1541962368 * x == -(x | -6684)";

        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprR_BitTracker()
    {
        string expr_str = "int x; ~(x ^ -1785936142) == x * 6 + x * 2 >>> 3";

        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprS()
    {
        string expr_str = "int x; (((uint)(x & 0x23D0) | ((uint)x / 7u)) & 0xC0000000u) != 0";

        var result = Eval(expr_str);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_sizeof_ulong()
    {
        string expr_str = "sizeof(ulong)";

        var result = Eval(expr_str);
        Assert.Equal(sizeof(ulong), result);
    }

    [Fact]
    public void Test_sizeof_Guid()
    {
        string expr_str = "sizeof(Guid)";

        var result = Eval(expr_str);
        Assert.Equal(16, result);
    }

    // "if either operand is of type uint and the other operand is of type sbyte, short, or int, both operands are converted to type long."
    //
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#12473-binary-numeric-promotions
    [Fact]
    public void Test_expr_mod_var()
    {
        uint x = 1653725481;
        int y = 4;
        var res = x % y; // long

        Eval("uint x = 1653725481");
        Eval("int y = 4");
        var result = Eval("x % y");

        Assert.Equal(res.GetType(), result!.GetType());
        Assert.Equal(res, result);
    }

    [Fact]
    public void Test_expr_int_mod()
    {
        int x = 1653725481;
        uint y = 4;
        var res = x % y; // long

        Eval("int x = 1653725481");
        Eval("uint y = 4");
        var result = Eval("x % y");

        Assert.Equal(res.GetType(), result!.GetType());
        Assert.Equal(res, result);
    }

    void check0_err(string res_type, object value1, string op, string value2)
    {
        // TBD
    }

    void check1_err(string res_type, object value1, string op, string value2)
    {
        // TBD
    }

    void check0(string exprected_res_type, int expected_res, string type1, object value1, string op, string value2)
    {
        var decl = $"{type1} x = {value1}";
        var expr = $"x {op} {value2}";

        Eval(decl);
        var result = Eval(expr);
        var short_res_type = TypeDB.ShortType(result!.GetType().Name);
        Assert.True(exprected_res_type == short_res_type, $"{decl}; {expr} == ({exprected_res_type}) {expected_res}; // got ({short_res_type}) {result}");
        Assert.Equal(expected_res.ToString(), result.ToString());
    }

    void check1(string exprected_res_type, int expected_res, string type1, object value1, string op, string value2)
    {
        var decl = $"{type1} x = {value1}";
        var expr = $"{value2} {op} x";

        Eval(decl);
        var result = Eval(expr);
        var short_res_type = TypeDB.ShortType(result!.GetType().Name);
        Assert.True(exprected_res_type == short_res_type, $"{decl}; {expr} == ({exprected_res_type}) {expected_res}; // got ({short_res_type}) {result}");
        Assert.Equal(expected_res.ToString(), result.ToString());
    }
}
