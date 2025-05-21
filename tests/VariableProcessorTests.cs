using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

public class VariableProcessorTests
{
    [Fact]
    public void Test_expr()
    {
        string expr_str = "(0x359 ^ ((0x82E & num4) * (int)(num9 << 14))) == 0";
        ExpressionSyntax expr = SyntaxFactory.ParseExpression(expr_str);
        VariableProcessor processor = new();
        var result = processor.EvaluateExpression(expr);
        Assert.Equal(UnknownValue.Create("bool"), result);
    }
}
