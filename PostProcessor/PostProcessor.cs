using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Text;

public class PostProcessor
{
    readonly VarDB _varDB;

    public PostProcessor(VarDB varDB)
    {
        _varDB = varDB;
    }

    public static SyntaxNode RemoveAllComments(SyntaxNode root)
    {
        return root.ReplaceTrivia(
            root.DescendantTrivia(),
            (trivia, _) =>
                trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                    ? default
                    : trivia
        );
    }

    public SyntaxNode ProcessFunction(SyntaxNode root)
    {
        return root switch
        {
            BaseMethodDeclarationSyntax method => method.WithBody(PostProcessAll(method.Body)),
            LocalFunctionStatementSyntax func => func.WithBody(PostProcessAll(func.Body)),
            _ => throw new InvalidOperationException($"Unsupported function type: {root.Kind()}")
        };
    }

    public BlockSyntax PostProcessAll(BlockSyntax block)
    {
        block = new EmptiesRemover().Visit(block) as BlockSyntax;
        block = new DuplicateDeclarationRemover().Visit(block) as BlockSyntax;
        block = new DeclarationAssignmentMerger().Visit(block) as BlockSyntax;
        block = new IfRewriter(_varDB).Visit(block) as BlockSyntax; // should be after EmptiesRemover
        block = new TernaryRewriter(_varDB).Visit(block) as BlockSyntax;
        return block;
    }

    public static string ExpandTabs(string input, int tabSize = 4)
    {
        var result = new StringBuilder();
        int column = 0;

        foreach (char c in input)
        {
            if (c == '\t')
            {
                int spaces = tabSize - (column % tabSize);
                result.Append(' ', spaces);
                column += spaces;
            }
            else
            {
                result.Append(c);
                column = (c == '\n' || c == '\r') ? 0 : column + 1;
            }
        }

        return result.ToString();
    }
}
