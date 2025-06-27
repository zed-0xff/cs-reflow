using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Xunit;

public class SyntaxNodeExtensionsTests
{
    [Fact]
    public void IsIdempotent_ReturnsTrue_ForLiteralExpression()
    {
        var literal = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(42));
        Assert.True(literal.IsIdempotent());
    }

    [Fact]
    public void IsIdempotent_ReturnsTrue_ForIdentifierName()
    {
        var identifier = SyntaxFactory.IdentifierName("x");
        Assert.True(identifier.IsIdempotent());
    }

    [Fact]
    public void IsIdempotent_ReturnsTrue_ForBinaryExpression()
    {
        var binary = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(2)));
        Assert.True(binary.IsIdempotent());
    }

    [Fact]
    public void IsIdempotent_ReturnsTrue_ForLogicalNot()
    {
        var unary = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
            SyntaxFactory.IdentifierName("x"));
        Assert.True(unary.IsIdempotent());
    }

    [Fact]
    public void IsIdempotent_ReturnsFalse_ForMethodCall()
    {
        var methodCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName("Method"),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(42))))));
        Assert.False(methodCall.IsIdempotent());
    }

    [Fact]
    public void IsIdempotent_ReturnsFalse_ForPostIncrementExpression()
    {
        var postIncrement = SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression,
            SyntaxFactory.IdentifierName("x"));
        Assert.False(postIncrement.IsIdempotent());
    }

    [Fact]
    public void IsIdempotent_ReturnsFalse_ForPreIncrementExpression()
    {
        var preIncrement = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression,
            SyntaxFactory.IdentifierName("x"));
        Assert.False(preIncrement.IsIdempotent());
    }

    [Fact]
    public void IsIdempotent_ReturnsTrue_ForStr()
    {
        string expr_str = "(0x359 ^ ((0x82E & num4) * (int)(num9 << 14))";
        var expr = SyntaxFactory.ParseExpression(expr_str);
        Assert.True(expr.IsIdempotent());
    }
}
