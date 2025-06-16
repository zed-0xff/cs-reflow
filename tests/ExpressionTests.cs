using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

public partial class ExpressionTests
{
    [Fact]
    public void Test_int_decl()
    {
        string stmt_str = "int x = 123";
        VarProcessor processor = new();
        var result = processor.EvaluateExpression(SyntaxFactory.ParseStatement(stmt_str));
        Assert.IsType<int>(processor.VariableValues["x"]);
        Assert.Equal(123, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_int_unk_assign()
    {
        string stmt_str = "int x = Foo.bar";
        VarProcessor processor = new();
        try
        {
            processor.EvaluateExpression(SyntaxFactory.ParseStatement(stmt_str));
        }
        catch (NotSupportedException)
        {
        }
        Assert.Equal(UnknownValue.Create(TypeDB.Int), processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_int_negate()
    {
        int x = 123;
        int y = ~x;
        Assert.Equal(-124, y);
        Assert.Equal(123, x);

        string stmt_str = "int y = ~x";
        VarProcessor processor = new();
        processor.VariableValues["x"] = 123;
        var result = processor.EvaluateExpression(SyntaxFactory.ParseStatement(stmt_str));
        Assert.IsType<int>(processor.VariableValues["y"]);
        Assert.Equal(-124, processor.VariableValues["y"]);
        Assert.IsType<int>(processor.VariableValues["x"]);
        Assert.Equal(123, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_int_assign_existing()
    {
        string expr_str = "x = 123";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["x"] = 0;
        var result = processor.EvaluateExpression(expr);
        Assert.IsType<int>(processor.VariableValues["x"]);
        Assert.Equal(123, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_int_or_assign_existing()
    {
        string expr_str = "x |= 123";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["x"] = 0;
        var result = processor.EvaluateExpression(expr);
        Assert.IsType<int>(processor.VariableValues["x"]);
        Assert.Equal(123, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_int_assign_new()
    {
        string expr_str = "x = 123";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        var result = processor.EvaluateExpression(expr);
        Assert.IsType<int>(processor.VariableValues["x"]);
        Assert.Equal(123, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_uint_decl()
    {
        string stmt_str = "uint x = 123";
        VarProcessor processor = new();
        var result = processor.EvaluateExpression(SyntaxFactory.ParseStatement(stmt_str));
        Assert.IsType<uint>(processor.VariableValues["x"]);
        Assert.Equal(123U, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_uint_assign_existing()
    {
        string expr_str = "x = 123";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["x"] = 0U;
        var result = processor.EvaluateExpression(expr);
        Assert.IsType<uint>(processor.VariableValues["x"]);
        Assert.Equal(123U, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_uint_assign_new()
    {
        string expr_str = "x = 123U";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        var result = processor.EvaluateExpression(expr);
        Assert.IsType<uint>(processor.VariableValues["x"]);
        Assert.Equal(123U, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_postIncr()
    {
        string expr_str = "x++";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["x"] = 1;
        var result = processor.EvaluateExpression(expr);
        Assert.IsType<int>(result);
        Assert.Equal(1, result);
        Assert.IsType<int>(processor.VariableValues["x"]);
        Assert.Equal(2, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_preIncr()
    {
        string expr_str = "++x";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["x"] = 1;
        var result = processor.EvaluateExpression(expr);
        Assert.IsType<int>(result);
        Assert.Equal(2, result);
        Assert.IsType<int>(processor.VariableValues["x"]);
        Assert.Equal(2, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_exprA()
    {
        string expr_str = "(0x359 ^ ((0x82E & num4) * (int)(num9 << 14))) == 0"; // always false
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num4"] = UnknownValue.Create("int");
        processor.VariableValues["num9"] = UnknownValue.Create("uint");
        var result = processor.EvaluateExpression(expr);
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
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);

        VarProcessor processor = new();
        processor.VariableValues["num32"] = UnknownValue.Create("int");
        processor.VariableValues["num11"] = UnknownValue.Create("uint");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprC()
    {
        string expr_str = "((4 * num252 + num252 * 4) & 4) == 0";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num252"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprD()
    {
        string expr_str = "0 == ((4 * (num10 & (num10 << 2))) & 4)";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num10"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprE()
    {
        string expr_str = "(((0x300 & ((num13 * 1939 + 109 * num13) ^ ((0xC86 & num12) >>> 5))) == 0) ? (-5579) : ((num14 << 5) - -48224 != 32 * (16402 * idat2 - (num14 & 0x1A85)))";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num12"] = UnknownValue.Create("int");
        processor.VariableValues["num13"] = UnknownValue.Create("int");
        processor.VariableValues["num14"] = 0;
        var result = processor.EvaluateExpression(expr);
        Assert.IsType<int>(result);
        Assert.Equal(-5579, result);
    }

    [Fact]
    public void Test_exprF1()
    {
        string expr_str = "!(~num11 == num11)";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num11"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprF2()
    {
        string expr_str = "!(~num11 != num11)";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num11"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprF3()
    {
        string expr_str = "!(num11 == ~num11)";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num11"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprF4()
    {
        string expr_str = "!(num11 != ~num11)";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num11"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprG()
    {
        string expr_str = "(((-(num12 + num12) << 1) ^ (10792 * num3 - 8444)) & 2) == 0";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num3"] = UnknownValue.Create("int");
        processor.VariableValues["num12"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprH()
    {
        string expr_str = "~(num116 + num116) != num117 * 3 + num117 - 708559999 >>> 1";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num116"] = UnknownValue.Create("int");
        processor.VariableValues["num117"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprI1()
    {
        string expr_str = "QRR3 * 134217728 - 1508900864 != (QRR2 * 22 + 10 * QRR2) * 262144";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["QRR2"] = UnknownValue.Create("int");
        processor.VariableValues["QRR3"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprI2()
    {
        string expr_str = "QRR3 * 134217728 - 1508900864 != (QRR2 * 22 + 10 * QRR2) * 262144";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["QRR2"] = UnknownValue.Create("int");
        processor.VariableValues["QRR3"] = 0;
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprJ()
    {
        string expr_str = "342177280 + (x >>> 11 >>> 1) != QRR11 >>> 4";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["x"] = UnknownValue.Create("int");
        processor.VariableValues["QRR11"] = 0;
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprK()
    {
        string expr_str = "(int)num8 - x * -1275068416 != -1135615528";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["num8"] = new UnknownValueRange(TypeDB.UInt, 0, 1194);
        processor.VariableValues["x"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
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

        string expr_str = "x % y";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["x"] = x;
        processor.VariableValues["y"] = y;
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(res.GetType(), result.GetType());
        Assert.Equal(res, result);
    }

    [Fact]
    public void Test_expr_int_mod()
    {
        int x = 1653725481;
        uint y = 4;
        var res = x % y; // long

        string expr_str = "x % y";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VarProcessor processor = new();
        processor.VariableValues["x"] = x;
        processor.VariableValues["y"] = y;
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(res.GetType(), result.GetType());
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

        VarProcessor processor = new();
        processor.EvaluateExpression(SyntaxFactory.ParseStatement(decl));
        var result = processor.EvaluateExpression(SyntaxFactory.ParseExpression(expr));
        var short_res_type = TypeDB.ShortType(result.GetType().Name);
        Assert.True(exprected_res_type == short_res_type, $"{decl}; {expr} == ({exprected_res_type}) {expected_res}; // got ({short_res_type}) {result}");
        Assert.Equal(expected_res.ToString(), result.ToString());
    }

    void check1(string exprected_res_type, int expected_res, string type1, object value1, string op, string value2)
    {
        var decl = $"{type1} x = {value1}";
        var expr = $"{value2} {op} x";

        VarProcessor processor = new();
        processor.EvaluateExpression(SyntaxFactory.ParseStatement(decl));
        var result = processor.EvaluateExpression(SyntaxFactory.ParseExpression(expr));
        var short_res_type = TypeDB.ShortType(result.GetType().Name);
        Assert.True(exprected_res_type == short_res_type, $"{decl}; {expr} == ({exprected_res_type}) {expected_res}; // got ({short_res_type}) {result}");
        Assert.Equal(expected_res.ToString(), result.ToString());
    }
}
