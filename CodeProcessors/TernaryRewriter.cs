using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class TernaryRewriter : RewriterBase
{
    public TernaryRewriter(VarDB varDB) : base(varDB)
    {
    }

    public override SyntaxNode VisitConditionalExpression(ConditionalExpressionSyntax node)
    {
        var expr = node.Condition;
        if (expr.IsIdempotent())
        {
            var result = eval_constexpr(expr);
            if (result is not null)
                return result.Value ? node.WhenTrue : node.WhenFalse;
        }

        return node;
    }
}
