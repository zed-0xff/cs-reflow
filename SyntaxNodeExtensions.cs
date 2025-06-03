using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public static class SyntaxNodeExtensions
{
    public static int LineNo(this SyntaxNode node)
    {
        var annotation = node.GetAnnotations("OriginalLineNo").FirstOrDefault();
        if (annotation != null && int.TryParse(annotation.Data, out int originalLine))
        {
            return originalLine;
        }

        if (node is LabeledStatementSyntax labeledStatement)
        {
            return labeledStatement.Statement.LineNo();
        }

        return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    }

    public static string Title(this SyntaxNode node)
    {
        return node.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault().Trim();
    }

    public static string TitleWithLineNo(this SyntaxNode node)
    {
        return $"{node.LineNo()}: {node.Title()}";
    }

    public static SyntaxNode StripLabel(this SyntaxNode node) => node is LabeledStatementSyntax l ? l.Statement : node;

    public static bool IsTerminal(this SyntaxNode node)
    {
        return node is BreakStatementSyntax ||
               node is ContinueStatementSyntax ||
               node is ReturnStatementSyntax ||
               node is ThrowStatementSyntax ||
               node is GotoStatementSyntax;
    }
}
