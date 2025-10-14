using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public class ExtraLoopRemover : CSharpSyntaxRewriter
{
    // lbl_28:
    //   throw new ArgumentOutOfRangeException("fromIndex", "FromIndex is out of range.");
    // goto lbl_28;
    public override SyntaxNode? VisitBlock(BlockSyntax node)
    {
        node = (BlockSyntax)base.VisitBlock(node)!;

        var newStatements = node.Statements
            .ToList();

        // Label + EmptyStatement => Label + next statement
        for (int i = 0; i < newStatements.Count - 1; i++)
        {
            if (newStatements[i] is LabeledStatementSyntax labelStmt && labelStmt.Statement is ThrowStatementSyntax &&
                newStatements[i + 1] is GotoStatementSyntax gotoStmt &&
                gotoStmt.Expression is IdentifierNameSyntax idName &&
                labelStmt.Identifier.Text == idName.Identifier.Text)
            {
                newStatements[i] = labelStmt.Statement;
                newStatements.RemoveAt(i + 1);
            }
        }

        if (newStatements.Count != node.Statements.Count)
            node = node.WithStatements(SyntaxFactory.List(newStatements));
        return node;
    }
}

