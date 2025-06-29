using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

// convert declaration followed by assignment into a single declaration with initializer
// i.e.:
//   byte[] array;
//   array = fun(obj2);
// =>
//   byte[] array = fun(obj2);
//
// XXX FIXME may hide default initializer call for objects
public class DeclarationAssignmentMerger : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitBlock(BlockSyntax node)
    {
        var newStatements = new List<StatementSyntax>();
        var stmts = node.Statements;

        bool was = false;
        int i = 0;
        while (i < stmts.Count)
        {
            // Try to match pattern: declaration + assignment
            if (i + 1 < stmts.Count &&
                stmts[i] is LocalDeclarationStatementSyntax declStmt && declStmt.Declaration.Variables.Count == 1 &&
                declStmt.Declaration.Variables[0].Initializer == null &&
                stmts[i + 1] is ExpressionStatementSyntax assignStmt &&
                assignStmt.Expression is AssignmentExpressionSyntax assignExpr &&
                assignExpr.Left is IdentifierNameSyntax leftId &&
                leftId.Identifier.IsSameVar(declStmt.Declaration.Variables[0].Identifier))
            {
                // Build combined declaration
                var variable = declStmt.Declaration.Variables[0]
                    .WithInitializer(SyntaxFactory.EqualsValueClause(assignExpr.Right))
                    .WithTrailingTrivia(assignStmt.GetTrailingTrivia());

                var newDecl = declStmt.WithDeclaration(
                    declStmt.Declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(variable)));

                newStatements.Add(newDecl);
                i += 2; // Skip both
                was = true;
            }
            else
            {
                newStatements.Add(stmts[i]);
                i++;
            }
        }

        return base.VisitBlock(was ? node.WithStatements(SyntaxFactory.List(newStatements)) : node);
    }
}
