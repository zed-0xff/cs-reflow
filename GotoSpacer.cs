using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class GotoSpacer : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitBlock(BlockSyntax node)
    {
        var newStatements = new List<StatementSyntax>();

        for (int i = 0; i < node.Statements.Count; i++)
        {
            var stmt = node.Statements[i];
            switch (stmt)
            {
                case GotoStatementSyntax:
                case ReturnStatementSyntax:
                    if (i < node.Statements.Count - 1)
                    {
                        stmt = stmt.WithTrailingTrivia(
                                TriviaList(
                                    EndOfLine("\n"),
                                    EndOfLine("\n") // Add a blank line after the goto/return
                                    )
                                );
                    }
                    break;
            }
            newStatements.Add(stmt);
        }

        return node.WithStatements(List(newStatements));
    }

    public static string Process(string src)
    {
        var tree = CSharpSyntaxTree.ParseText(src);
        var root = tree.GetRoot();
        var rewriter = new GotoSpacer();
        var newRoot = rewriter.Visit(root);
        return newRoot.ToFullString();
    }
}
