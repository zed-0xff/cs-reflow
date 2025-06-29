using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

public class CommentAligner : CSharpSyntaxRewriter
{
    private readonly SourceText _text;
    private readonly int _targetColumn;

    public CommentAligner(SourceText text, int targetColumn = 80)
    {
        _text = text;
        _targetColumn = targetColumn;
    }

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (!token.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia)))
            return token;

        var newTrivia = new List<SyntaxTrivia>();
        bool aligned = false;

        foreach (var trivia in token.TrailingTrivia)
        {
            if (!aligned && trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                var tokenEnd = token.Span.End;
                var line = _text.Lines.GetLineFromPosition(tokenEnd);
                int offsetInLine = tokenEnd - line.Start;

                if (token.Parent is EmptyStatementSyntax)
                {
                    var trimmedText = trivia.ToString().Trim();

                    if (trimmedText.Contains(" // "))
                    {
                        // Split at the first occurrence of " // "
                        var splitIndex = trimmedText.IndexOf(" // ");

                        var leftCommentText = trimmedText.Substring(0, splitIndex);
                        var rightCommentText = trimmedText.Substring(splitIndex);

                        // Add left comment (non-aligned)
                        newTrivia.Add(SyntaxFactory.Comment(leftCommentText));

                        // Add padding spaces for alignment
                        int paddingSpaces = Math.Max(1, _targetColumn - offsetInLine - leftCommentText.Length - 1);
                        newTrivia.Add(SyntaxFactory.Whitespace(new string(' ', paddingSpaces)));

                        // Add right comment (aligned)
                        newTrivia.Add(SyntaxFactory.Comment(rightCommentText));

                        aligned = true;
                        continue; // skip rest of loop to avoid duplicate addition
                    }
                    else
                    {
                        // Case A: just add the comment as-is (non-aligned)
                        newTrivia.Add(trivia);
                        aligned = true;
                        continue;
                    }
                }

                // For tokens NOT under EmptyStatementSyntax, do default alignment
                int padSpaces = Math.Max(1, _targetColumn - offsetInLine);
                newTrivia.Add(SyntaxFactory.Whitespace(new string(' ', padSpaces)));
                newTrivia.Add(trivia);
                aligned = true;
            }
            else
            {
                newTrivia.Add(trivia);
            }
        }

        return token.WithTrailingTrivia(newTrivia);
    }
}
