using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

public class VariableProcessorTests
{
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
}
