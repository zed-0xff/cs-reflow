using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public class PureArithmeticsEvaluatorTests
{

    [Fact]
    public void TestSimpleBinary()
    {
        var evaluator = new PureArithmeticsEvaluator();
        var code = "int a = 5*3;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var newRoot = evaluator.Visit(root);

        // Check if the arithmetic expression was evaluated correctly
        Assert.NotNull(newRoot);
        Assert.Equal("int a = 15;", newRoot.ToString());
    }

    [Fact]
    public void TestSimpleUnary()
    {
        var evaluator = new PureArithmeticsEvaluator();
        var code = "int a = -(-15);";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var newRoot = evaluator.Visit(root);

        // Check if the unary expression was evaluated correctly
        Assert.NotNull(newRoot);
        Assert.Equal("int a = 15;", newRoot.ToString());
    }

    [Fact]
    public void TestMinusUnaryMinus()
    {
        var evaluator = new PureArithmeticsEvaluator();
        var code = "int a = x - (-y);";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var newRoot = evaluator.Visit(root);

        // Check if the unary expression was evaluated correctly
        Assert.NotNull(newRoot);
        Assert.Equal("int a = x + y;", newRoot.NormalizeWhitespace().ToString());
    }
}
