using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class ConsoleColorizer
{
    public static void ColorizeToConsole(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        foreach (var token in root.DescendantTokens(descendIntoTrivia: true))
        {
            // Leading trivia (e.g. comments, whitespace)
            foreach (var trivia in token.LeadingTrivia)
                WriteTrivia(trivia);

            // Token itself
            WriteToken(token);

            // Trailing trivia
            foreach (var trivia in token.TrailingTrivia)
                WriteTrivia(trivia);
        }

        Console.ResetColor();
        Console.WriteLine();
    }

    private static void WriteToken(SyntaxToken token)
    {
        Console.ForegroundColor = GetColor(token);
        Console.Write(token.Text);
        Console.ResetColor();
    }

    private static void WriteTrivia(SyntaxTrivia trivia)
    {
        Console.ForegroundColor = GetTriviaColor(trivia);
        Console.Write(trivia.ToFullString());
        Console.ResetColor();
    }

    private static ConsoleColor GetColor(SyntaxToken token)
    {
        if (token.IsKind(SyntaxKind.SemicolonToken) && token.Parent is EmptyStatementSyntax)
            return ConsoleColor.DarkGray;

        if (IsTypeNameToken(token))
            return ConsoleColor.Green;

        switch (token.Kind())
        {
            case SyntaxKind.EqualsToken:             // =
            case SyntaxKind.EqualsEqualsToken:       // ==
            case SyntaxKind.ExclamationEqualsToken:  // !=
            case SyntaxKind.LessThanToken:            // <
            case SyntaxKind.GreaterThanToken:         // >
            case SyntaxKind.AmpersandAmpersandToken:  // &&
            case SyntaxKind.BarBarToken:              // ||
            case SyntaxKind.PlusToken:                // +
            case SyntaxKind.MinusToken:               // -
            case SyntaxKind.AsteriskToken:            // *
            case SyntaxKind.SlashToken:               // /
            case SyntaxKind.PercentToken:             // %
            case SyntaxKind.PlusPlusToken:            // ++
            case SyntaxKind.MinusMinusToken:          // --
            case SyntaxKind.LessThanEqualsToken:      // <=
            case SyntaxKind.GreaterThanEqualsToken:   // >=
            case SyntaxKind.AmpersandToken:            // &
            case SyntaxKind.BarToken:                  // |
            case SyntaxKind.CaretToken:                // ^
            case SyntaxKind.TildeToken:                // ~
            case SyntaxKind.LessThanLessThanToken:    // <<
            case SyntaxKind.GreaterThanGreaterThanToken:// >>
                return ConsoleColor.Yellow;

            case SyntaxKind.TrueKeyword:
            case SyntaxKind.FalseKeyword:
            case SyntaxKind.StringLiteralToken:
            case SyntaxKind.NumericLiteralToken:
                return ConsoleColor.Red;

            case SyntaxKind.IdentifierToken:
                return GetIdentifierColor(token);

            default:
                return token.IsKeyword() ? ConsoleColor.Yellow : ConsoleColor.White;
        }
        ;
    }

    private static bool IsTypeNameToken(SyntaxToken token)
    {
        var parent = token.Parent;

        return parent is PredefinedTypeSyntax
            || parent is NullableTypeSyntax
            || parent is ArrayTypeSyntax;
    }

    private static ConsoleColor GetIdentifierColor(SyntaxToken token)
    {
        return token.Parent switch
        {
            Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax => ConsoleColor.Cyan,
            Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax => ConsoleColor.Magenta,
            _ => ConsoleColor.Gray
        };
    }

    private static ConsoleColor GetTriviaColor(SyntaxTrivia trivia)
    {
        return trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
               trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
               ? ConsoleColor.DarkGray
               : ConsoleColor.White;
    }
}

