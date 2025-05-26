using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

public class ExpressionTests
{
    [Fact]
    public void Test_int_decl()
    {
        string stmt_str = "int x = 123";
        VariableProcessor processor = new();
        var result = processor.EvaluateExpression(SyntaxFactory.ParseStatement(stmt_str));
        Assert.IsType<int>(processor.VariableValues["x"]);
        Assert.Equal(123, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_int_assign_existing()
    {
        string expr_str = "x = 123";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
        var result = processor.EvaluateExpression(expr);
        Assert.IsType<int>(processor.VariableValues["x"]);
        Assert.Equal(123, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_uint_decl()
    {
        string stmt_str = "uint x = 123";
        VariableProcessor processor = new();
        var result = processor.EvaluateExpression(SyntaxFactory.ParseStatement(stmt_str));
        Assert.IsType<uint>(processor.VariableValues["x"]);
        Assert.Equal(123U, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_uint_assign_existing()
    {
        string expr_str = "x = 123";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
        var result = processor.EvaluateExpression(expr);
        Assert.IsType<uint>(processor.VariableValues["x"]);
        Assert.Equal(123U, processor.VariableValues["x"]);
    }

    [Fact]
    public void Test_postIncr()
    {
        string expr_str = "x++";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
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

        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
        processor.VariableValues["num252"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprD()
    {
        string expr_str = "0 == ((4 * (num10 & (num10 << 2))) & 4)";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VariableProcessor processor = new();
        processor.VariableValues["num10"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprE()
    {
        string expr_str = "(((0x300 & ((num13 * 1939 + 109 * num13) ^ ((0xC86 & num12) >>> 5))) == 0) ? (-5579) : ((num14 << 5) - -48224 != 32 * (16402 * idat2 - (num14 & 0x1A85)))";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
        processor.VariableValues["num11"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprF2()
    {
        string expr_str = "!(~num11 != num11)";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VariableProcessor processor = new();
        processor.VariableValues["num11"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprF3()
    {
        string expr_str = "!(num11 == ~num11)";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VariableProcessor processor = new();
        processor.VariableValues["num11"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Test_exprF4()
    {
        string expr_str = "!(num11 != ~num11)";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VariableProcessor processor = new();
        processor.VariableValues["num11"] = UnknownValue.Create("int");
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Test_exprG()
    {
        string expr_str = "(((-(num12 + num12) << 1) ^ (10792 * num3 - 8444)) & 2) == 0";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
        processor.VariableValues["x"] = UnknownValue.Create("int");
        processor.VariableValues["QRR11"] = 0;
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
        VariableProcessor processor = new();
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
        VariableProcessor processor = new();
        processor.VariableValues["x"] = x;
        processor.VariableValues["y"] = y;
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(res.GetType(), result.GetType());
        Assert.Equal(res, result);
    }

    void check_err(string res_type, object value1, string op, string value2)
    {
        // TBD
    }

    void check(string exprected_res_type, string expected_res, string type1, object value1, string op, string value2)
    {
        var decl = $"{type1} x = {value1}";
        var expr = $"x {op} {value2}";

        VariableProcessor processor = new();
        processor.EvaluateExpression(SyntaxFactory.ParseStatement(decl));
        var result = processor.EvaluateExpression(SyntaxFactory.ParseExpression(expr));
        var short_res_type = TypeDB.ShortType(result.GetType().Name);
        Assert.True(exprected_res_type == short_res_type, $"{decl}; {expr} == ({exprected_res_type}) {expected_res}; // got ({short_res_type}) {result}");
        Assert.Equal(expected_res, result.ToString());
    }

    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#12473-binary-numeric-promotions
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#10211-implicit-constant-expression-conversions

#pragma warning disable format
    [Fact]  void  check_byte_()          {  check("int",        "127",  "byte",    123,          "+",  "4");          }
    [Fact]  void  check_byte_byte()      {  check("int",        "127",  "byte",    123,          "+",  "(byte)4");    }
    [Fact]  void  check_byte_sbyte()     {  check("int",        "127",  "byte",    123,          "+",  "(sbyte)4");   }
    [Fact]  void  check_byte_short()     {  check("int",        "127",  "byte",    123,          "+",  "(short)4");   }
    [Fact]  void  check_byte_ushort()    {  check("int",        "127",  "byte",    123,          "+",  "(ushort)4");  }
    [Fact]  void  check_byte_int()       {  check("int",        "127",  "byte",    123,          "+",  "(int)4");     }
    [Fact]  void  check_byte_uint()      {  check("uint",       "127",  "byte",    123,          "+",  "(uint)4");    }
    [Fact]  void  check_byte_long()      {  check("long",       "127",  "byte",    123,          "+",  "(long)4");    }
    [Fact]  void  check_byte_ulong()     {  check("ulong",      "127",  "byte",    123,          "+",  "(ulong)4");   }
    [Fact]  void  check_sbyte_()         {  check("int",        "127",  "sbyte",   123,          "+",  "4");          }
    [Fact]  void  check_sbyte_byte()     {  check("int",        "127",  "sbyte",   123,          "+",  "(byte)4");    }
    [Fact]  void  check_sbyte_sbyte()    {  check("int",        "127",  "sbyte",   123,          "+",  "(sbyte)4");   }
    [Fact]  void  check_sbyte_short()    {  check("int",        "127",  "sbyte",   123,          "+",  "(short)4");   }
    [Fact]  void  check_sbyte_ushort()   {  check("int",        "127",  "sbyte",   123,          "+",  "(ushort)4");  }
    [Fact]  void  check_sbyte_int()      {  check("int",        "127",  "sbyte",   123,          "+",  "(int)4");     }
    [Fact]  void  check_sbyte_uint()     {  check("long",       "127",  "sbyte",   123,          "+",  "(uint)4");    }
    [Fact]  void  check_sbyte_long()     {  check("long",       "127",  "sbyte",   123,          "+",  "(long)4");    }
    [Fact]  void  check_sbyte_ulong()    {  check_err("sbyte",  123,    "+",       "(ulong)4");  }
    [Fact]  void  check_short_()         {  check("int",        "127",  "short",   123,          "+",  "4");          }
    [Fact]  void  check_short_byte()     {  check("int",        "127",  "short",   123,          "+",  "(byte)4");    }
    [Fact]  void  check_short_sbyte()    {  check("int",        "127",  "short",   123,          "+",  "(sbyte)4");   }
    [Fact]  void  check_short_short()    {  check("int",        "127",  "short",   123,          "+",  "(short)4");   }
    [Fact]  void  check_short_ushort()   {  check("int",        "127",  "short",   123,          "+",  "(ushort)4");  }
    [Fact]  void  check_short_int()      {  check("int",        "127",  "short",   123,          "+",  "(int)4");     }
    [Fact]  void  check_short_uint()     {  check("long",       "127",  "short",   123,          "+",  "(uint)4");    }
    [Fact]  void  check_short_long()     {  check("long",       "127",  "short",   123,          "+",  "(long)4");    }
    [Fact]  void  check_short_ulong()    {  check_err("short",  123,    "+",       "(ulong)4");  }
    [Fact]  void  check_ushort_()        {  check("int",        "127",  "ushort",  123,          "+",  "4");          }
    [Fact]  void  check_ushort_byte()    {  check("int",        "127",  "ushort",  123,          "+",  "(byte)4");    }
    [Fact]  void  check_ushort_sbyte()   {  check("int",        "127",  "ushort",  123,          "+",  "(sbyte)4");   }
    [Fact]  void  check_ushort_short()   {  check("int",        "127",  "ushort",  123,          "+",  "(short)4");   }
    [Fact]  void  check_ushort_ushort()  {  check("int",        "127",  "ushort",  123,          "+",  "(ushort)4");  }
    [Fact]  void  check_ushort_int()     {  check("int",        "127",  "ushort",  123,          "+",  "(int)4");     }
    [Fact]  void  check_ushort_uint()    {  check("uint",       "127",  "ushort",  123,          "+",  "(uint)4");    }
    [Fact]  void  check_ushort_long()    {  check("long",       "127",  "ushort",  123,          "+",  "(long)4");    }
    [Fact]  void  check_ushort_ulong()   {  check("ulong",      "127",  "ushort",  123,          "+",  "(ulong)4");   }
    [Fact]  void  check_int_()           {  check("int",        "127",  "int",     123,          "+",  "4");          }
    [Fact]  void  check_int_byte()       {  check("int",        "127",  "int",     123,          "+",  "(byte)4");    }
    [Fact]  void  check_int_sbyte()      {  check("int",        "127",  "int",     123,          "+",  "(sbyte)4");   }
    [Fact]  void  check_int_short()      {  check("int",        "127",  "int",     123,          "+",  "(short)4");   }
    [Fact]  void  check_int_ushort()     {  check("int",        "127",  "int",     123,          "+",  "(ushort)4");  }
    [Fact]  void  check_int_int()        {  check("int",        "127",  "int",     123,          "+",  "(int)4");     }
    [Fact]  void  check_int_uint()       {  check("long",       "127",  "int",     123,          "+",  "(uint)4");    }
    [Fact]  void  check_int_long()       {  check("long",       "127",  "int",     123,          "+",  "(long)4");    }
    [Fact]  void  check_int_ulong()      {  check_err("int",    123,    "+",       "(ulong)4");  }
    [Fact]  void  check_uint_()          {  check("uint",       "127",  "uint",    123,          "+",  "4");          }
    [Fact]  void  check_uint_byte()      {  check("uint",       "127",  "uint",    123,          "+",  "(byte)4");    }
    [Fact]  void  check_uint_sbyte()     {  check("long",       "127",  "uint",    123,          "+",  "(sbyte)4");   }
    [Fact]  void  check_uint_short()     {  check("long",       "127",  "uint",    123,          "+",  "(short)4");   }
    [Fact]  void  check_uint_ushort()    {  check("uint",       "127",  "uint",    123,          "+",  "(ushort)4");  }
    [Fact]  void  check_uint_int()       {  check("uint",       "127",  "uint",    123,          "+",  "(int)4");     }
    [Fact]  void  check_uint_uint()      {  check("uint",       "127",  "uint",    123,          "+",  "(uint)4");    }
    [Fact]  void  check_uint_long()      {  check("long",       "127",  "uint",    123,          "+",  "(long)4");    }
    [Fact]  void  check_uint_ulong()     {  check("ulong",      "127",  "uint",    123,          "+",  "(ulong)4");   }
    [Fact]  void  check_long_()          {  check("long",       "127",  "long",    123,          "+",  "4");          }
    [Fact]  void  check_long_byte()      {  check("long",       "127",  "long",    123,          "+",  "(byte)4");    }
    [Fact]  void  check_long_sbyte()     {  check("long",       "127",  "long",    123,          "+",  "(sbyte)4");   }
    [Fact]  void  check_long_short()     {  check("long",       "127",  "long",    123,          "+",  "(short)4");   }
    [Fact]  void  check_long_ushort()    {  check("long",       "127",  "long",    123,          "+",  "(ushort)4");  }
    [Fact]  void  check_long_int()       {  check("long",       "127",  "long",    123,          "+",  "(int)4");     }
    [Fact]  void  check_long_uint()      {  check("long",       "127",  "long",    123,          "+",  "(uint)4");    }
    [Fact]  void  check_long_long()      {  check("long",       "127",  "long",    123,          "+",  "(long)4");    }
    [Fact]  void  check_long_ulong()     {  check_err("long",   123,    "+",       "(ulong)4");  }
    [Fact]  void  check_ulong_()         {  check("ulong",      "127",  "ulong",   123,          "+",  "4");          }
    [Fact]  void  check_ulong_byte()     {  check("ulong",      "127",  "ulong",   123,          "+",  "(byte)4");    }
    [Fact]  void  check_ulong_sbyte()    {  check_err("ulong",  123,    "+",       "(sbyte)4");  }
    [Fact]  void  check_ulong_short()    {  check_err("ulong",  123,    "+",       "(short)4");  }
    [Fact]  void  check_ulong_ushort()   {  check("ulong",      "127",  "ulong",   123,          "+",  "(ushort)4");  }
    [Fact]  void  check_ulong_int()      {  check("ulong",      "127",  "ulong",   123,          "+",  "(int)4");     }
    [Fact]  void  check_ulong_uint()     {  check("ulong",      "127",  "ulong",   123,          "+",  "(uint)4");    }
    [Fact]  void  check_ulong_long()     {  check("ulong",      "127",  "ulong",   123,          "+",  "(long)4");    }
    [Fact]  void  check_ulong_ulong()    {  check("ulong",      "127",  "ulong",   123,          "+",  "(ulong)4");   }
#pragma warning restore format
}
