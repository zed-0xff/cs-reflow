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
}
