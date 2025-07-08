using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class PureArithmeticsEvaluator : CSharpSyntaxRewriter
{
    readonly VarDB _varDB;
    readonly VarProcessor _processor;

    static readonly ExpressionSyntax TRUE_LITERAL = LiteralExpression(SyntaxKind.TrueLiteralExpression);
    static readonly ExpressionSyntax FALSE_LITERAL = LiteralExpression(SyntaxKind.FalseLiteralExpression);

    public PureArithmeticsEvaluator()
    {
        _varDB = new();
        _processor = new(_varDB);
    }

    static SyntaxNode? obj2node(object? obj) =>
        obj switch
        {
            true => TRUE_LITERAL,
            false => FALSE_LITERAL,
            int i => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i)),
            long l => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(l)),
            ulong u => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal((long)u)),
            _ => throw new InvalidOperationException($"Unexpected result type: {obj?.GetType()}"),
        };

    public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        if (node.IsPureArithmetic())
        {
            var result = _processor.EvaluateExpression(node);
            Logger.debug(() => $"{node} => {result}", "PureArithmeticsEvaluator.VisitBinaryExpression");
            return obj2node(result);
        }

        // x - (-y) => x + y
        if (node.IsKind(SyntaxKind.SubtractExpression)
                && node.Right.StripParentheses() is PrefixUnaryExpressionSyntax unaryR
                && unaryR.IsKind(SyntaxKind.UnaryMinusExpression))
        {
            var newNode = BinaryExpression(SyntaxKind.AddExpression, node.Left, unaryR.Operand);
            Logger.debug(() => $"{node} => {newNode}", "PureArithmeticsEvaluator.VisitBinaryExpression");
            return VisitBinaryExpression(newNode);
        }

        return base.VisitBinaryExpression(node);
    }

    public override SyntaxNode? VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
    {
        if (node.IsPureArithmetic())
        {
            var result = _processor.EvaluateExpression(node);
            Logger.debug(() => $"{node} => {result}", "PureArithmeticsEvaluator.VisitPrefixUnaryExpression");
            return obj2node(result);
        }

        // -(-x) => x
        if (node.IsKind(SyntaxKind.UnaryMinusExpression)
                && node.Operand.StripParentheses() is PrefixUnaryExpressionSyntax unaryOperand
                && unaryOperand.IsKind(SyntaxKind.UnaryMinusExpression))
        {
            var newNode = unaryOperand.Operand;
            Logger.debug(() => $"{node} => {newNode}", "PureArithmeticsEvaluator.VisitPrefixUnaryExpression");
            return Visit(newNode);
        }

        return base.VisitPrefixUnaryExpression(node);
    }
}
