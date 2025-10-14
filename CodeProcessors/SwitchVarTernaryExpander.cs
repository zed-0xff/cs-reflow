using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class SwitchVarTernaryExpander : CSharpSyntaxRewriter
{
    readonly VarDB _varDB;

    public SwitchVarTernaryExpander(VarDB varDB)
    {
        _varDB = varDB;
    }

    // before:
    //   num = ((fromIndex < 0) ? 1 : 0) * 2 + 6;
    //
    // after:
    //   if (fromIndex < 0)
    //     num = 1 * 2 + 6;
    //   else
    //     num = 0 * 2 + 6;
    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        if (node.Expression is AssignmentExpressionSyntax assignExpr &&
                assignExpr.Left is IdentifierNameSyntax idName)
        {
            var var_id = idName.VarID();
            if (var_id is not null && _varDB[var_id].IsSwitchVar)
            {
                // Find ConditionalExpressionSyntax inside Right
                var conditional = assignExpr.Right.DescendantNodesAndSelf()
                    .OfType<ConditionalExpressionSyntax>()
                    .FirstOrDefault();

                if (conditional != null)
                {
                    // Build two new right-hand sides: one with trueExpr, one with falseExpr
                    var trueReplaced = assignExpr.Right.ReplaceNode(conditional, conditional.WhenTrue);
                    var falseReplaced = assignExpr.Right.ReplaceNode(conditional, conditional.WhenFalse);

                    // Construct assignments
                    var trueAssign = assignExpr.WithRight(trueReplaced);
                    var falseAssign = assignExpr.WithRight(falseReplaced);

                    // Wrap in if-else
                    var ifStatement =
                        SyntaxFactory.IfStatement(
                                conditional.Condition,
                                SyntaxFactory.ExpressionStatement(trueAssign),
                                SyntaxFactory.ElseClause(
                                    SyntaxFactory.ExpressionStatement(falseAssign)));

                    return ifStatement.WithTriviaFrom(node);
                }
            }
        }
        return base.VisitExpressionStatement(node);
    }
}
