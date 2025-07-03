using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

// removes:
//  - empty finally block
//  - all EmptyStatementSyntax
public class EmptiesRemover : CSharpSyntaxRewriter
{
    // remove empty finally block
    public override SyntaxNode VisitTryStatement(TryStatementSyntax node)
    {
        node = (TryStatementSyntax)base.VisitTryStatement(node)!;

        if (node.Finally != null && node.Finally.Block.Statements.Count == 0)
            node = node.WithFinally(null);

        return node;
    }

    // remove all EmptyStatementSyntax
    public override SyntaxNode? VisitBlock(BlockSyntax node)
    {
        node = (BlockSyntax)base.VisitBlock(node)!;

        var newStatements = node.Statements
            .Where(stmt => !(stmt is EmptyStatementSyntax)) // remove empty statements
            .ToList();

        // Label + EmptyStatement => Label + next statement
        for (int i = 0; i < newStatements.Count - 1; i++)
        {
            if (newStatements[i] is LabeledStatementSyntax labelStmt && labelStmt.Statement is EmptyStatementSyntax)
            {
                newStatements[i] = labelStmt.WithStatement(newStatements[i + 1]);
                newStatements.RemoveAt(i + 1);
            }
        }

        if (newStatements.Count != node.Statements.Count)
            node = node.WithStatements(SyntaxFactory.List(newStatements));
        return node;
    }
}

