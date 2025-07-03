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

    public SyntaxNode PostProcessAll(SyntaxNode block)
    {
        block = new EmptiesRemover().Visit(block);
        block = new DuplicateDeclarationRemover().Visit(block);
        block = new DeclarationAssignmentMerger().Visit(block);
        block = new IfRewriter(_varDB).Visit(block); // should be after EmptiesRemover
        block = new TernaryRewriter(_varDB).Visit(block);
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
