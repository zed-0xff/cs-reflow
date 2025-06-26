using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public static class BlockSyntaxExtensions
{
    public static int IndexWithLabels(this BlockSyntax block, StatementSyntax stmt)
    {
        int idx = block.Statements.IndexOf(stmt);
        if (idx != -1)
            return idx;

        // If the statement is not directly in the block, check if it is labeled
        for (int i = 0; i < block.Statements.Count; i++)
        {
            var labeledStmt = block.Statements[i] as LabeledStatementSyntax;
            if (labeledStmt != null && labeledStmt.Statement == stmt)
            {
                return i;
            }
        }

        // not found
        return -1;
    }
}
