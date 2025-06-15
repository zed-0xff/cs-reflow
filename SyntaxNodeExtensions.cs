using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public static class SyntaxNodeExtensions
{
    static readonly string[] ANNOTATION_KINDS = new[] { "ID", "VAR", "OriginalLineNo" };

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

    public static string TitleWithLineSpan(this SyntaxNode node)
    {
        int start = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        int end = node.GetLocation().GetLineSpan().EndLinePosition.Line + 1;
        return (end == start) ?
            $"{start}: {node.Title()}" :
            $"{start}-{end}: {node.Title()}";
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

    public static TTarget? FirstAncestorOrSelfUntil<TTarget, TBoundary>(this SyntaxNode node)
        where TTarget : SyntaxNode
        where TBoundary : SyntaxNode
    {
        var current = node;
        while (current != null && current is not TBoundary)
        {
            if (current is TTarget match)
                return match;
            current = current.Parent;
        }
        return null;
    }

    public static T ReplaceAndGetNewNode<T>(this SyntaxNode oldNode, T newNode)
        where T : SyntaxNode
    {
        var annotation = new SyntaxAnnotation();
        var annotatedNewNode = newNode.WithAdditionalAnnotations(annotation);

        var oldRoot = oldNode.SyntaxTree.GetCompilationUnitRoot();
        var newRoot = oldRoot.ReplaceNode(oldNode, annotatedNewNode);

        return newRoot.GetAnnotatedNodes(annotation).OfType<T>().First();
    }

    // dosent work as expected
    //
    // public static T ReplaceAndGetNewNode<T>(T oldNode, T newNode)
    //     where T : SyntaxNode
    //     {
    //         var root = oldNode.SyntaxTree.GetCompilationUnitRoot();
    //         var newRoot = root.TrackNodes(oldNode).ReplaceNode(oldNode, newNode);
    //
    //         var updatedNode = newRoot.GetCurrentNode(oldNode);
    //         if (updatedNode == null)
    //             throw new InvalidOperationException("Updated node not found in new tree.");
    //
    //         return (T)updatedNode;
    //     }

    public static bool IsSameVar(this SyntaxNode node1, SyntaxNode node2)
    {
        if (node1.IsSameStmt(node2))
            return true;

        var ann2 = node2.GetAnnotations("VAR").FirstOrDefault();

        if (ann2 != null && node1.IsSameVar(ann2))
            return true;

        if (node2 is LocalDeclarationStatementSyntax decl2 && decl2.Declaration.Variables.Count == 1)
        {
            node2 = decl2.Declaration.Variables[0];
            ann2 = node2.GetAnnotations("VAR").FirstOrDefault();
            if (ann2 != null && node1.IsSameVar(ann2))
                return true;
        }

        return false;
    }

    public static bool IsSameVar(this SyntaxNode node1, SyntaxAnnotation ann2)
    {
        var ann1 = node1.GetAnnotations("VAR").FirstOrDefault();

        if (ann1 != null && ann2 != null && ann1.Data == ann2.Data)
            return true;

        if (node1 is LocalDeclarationStatementSyntax decl1 && decl1.Declaration.Variables.Count == 1)
        {
            node1 = decl1.Declaration.Variables[0];
            ann1 = node1.GetAnnotations("VAR").FirstOrDefault();
            if (ann1 != null && ann2 != null && ann1.Data == ann2.Data)
                return true;
        }

        return false;
    }

    public static bool IsSameStmt(this SyntaxNode node1, SyntaxNode node2)
    {
        var ann1 = node1.GetAnnotations("ID").FirstOrDefault();
        var ann2 = node2.GetAnnotations("ID").FirstOrDefault();

        return ann1 != null && ann2 != null && ann1.Data == ann2.Data;
    }

    public static string? AnnotationsAsString(this SyntaxNode node)
    {
        List<string> annotations = new List<string>();
        foreach (var ann in node.GetAnnotations(ANNOTATION_KINDS))
        {
            annotations.Add($"{ann.Kind}:{ann.Data}");
        }
        return annotations.Count > 0 ? string.Join(", ", annotations) : null;
    }

    public static string? NestedAnnotationsAsString(this SyntaxNode node)
    {
        var stuff = node.GetAnnotatedNodesAndTokens(ANNOTATION_KINDS);
        if (stuff.Count() == 0)
            return null;

        HashSet<string> annotations = new();
        foreach (var item in stuff)
        {
            if (item.IsNode)
            {
                foreach (var ann in item.AsNode().GetAnnotations(ANNOTATION_KINDS))
                    annotations.Add($"{ann.Kind}:{ann.Data}");
            }
            else
            {
                foreach (var ann in item.AsToken().GetAnnotations(ANNOTATION_KINDS))
                    annotations.Add($"{ann.Kind}:{ann.Data}");
            }
        }
        return annotations.Count > 0 ? string.Join(", ", annotations) : null;
    }

    public static string EscapeNonPrintable(string input)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var ch in input)
        {
            if (char.IsControl(ch) && ch != '\t')
            {
                switch (ch)
                {
                    case '\n': sb.Append(ch); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append($"\\x{(int)ch:X2}"); break;
                }
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    public static T WithComment<T>(this T node, string comment) where T : SyntaxNode
    {
        return node.WithTrailingTrivia(SyntaxFactory.Comment(" // " + EscapeNonPrintable(comment)));
    }
}
