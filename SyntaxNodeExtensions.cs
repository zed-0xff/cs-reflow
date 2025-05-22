using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public static class SyntaxNodeExtensions
{
    public static int LineNo(this CSharpSyntaxNode node)
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
}
