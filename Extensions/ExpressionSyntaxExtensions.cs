using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

public static class ExpressionSyntaxExtensions
{
    public static ExpressionSyntax StripParentheses(this ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }
        return expression;
    }

    public static List<SyntaxToken> CollectTokens(this ExpressionSyntax expr)
    {
        var tokens = new List<SyntaxToken>();
        expr = expr.StripParentheses();
        switch (expr)
        {
            case IdentifierNameSyntax id:
                tokens.Add(id.Identifier);
                break;
            case TupleExpressionSyntax tuple:
                foreach (var item in tuple.Arguments)
                    tokens.AddRange(CollectTokens(item.Expression));
                break;
        }
        return tokens;
    }
}
