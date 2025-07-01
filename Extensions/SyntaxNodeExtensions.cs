using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public static class SyntaxNodeExtensions
{
    static readonly string[] ANNOTATION_KINDS = new[] { "StmtID", "VarID", "LineNo" };

    public static int LineNo(this SyntaxNode node)
    {
        var annotation = node.GetAnnotations("LineNo").FirstOrDefault();
        if (annotation != null && int.TryParse(annotation.Data, out int originalLine))
            return originalLine;

        if (node is LabeledStatementSyntax labeledStatement)
            return labeledStatement.Statement.LineNo();

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

    public static bool IsIdempotent(this SyntaxNode node)
    {
        if (node == null)
            return false;

        switch (node)
        {
            case BinaryExpressionSyntax binary:
                return binary.Left.IsIdempotent() && binary.Right.IsIdempotent();

            case CastExpressionSyntax cast:
                return cast.Expression.IsIdempotent(); // casting is idempotent if the expression is

            case IdentifierNameSyntax:
                return true; // assume local vars are idempotent (needs semantic model for precision)

            case LiteralExpressionSyntax:
                return true; // constants are idempotent

            case MemberAccessExpressionSyntax memberAccess:
                return memberAccess.IsKnownConstant();

            case ParenthesizedExpressionSyntax paren:
                return paren.Expression.IsIdempotent();

            case PrefixUnaryExpressionSyntax prefix:
                // ++x, --x are not idempotent
                return prefix.Kind() switch
                {
                    SyntaxKind.LogicalNotExpression or
                        SyntaxKind.UnaryMinusExpression or
                        SyntaxKind.UnaryPlusExpression => prefix.Operand.IsIdempotent(),
                    _ => false
                };

            default:
                Logger.debug(() => $"Node {node.GetType()} is not idempotent: {node}", "SyntaxNode.IsIdempotent");
                return false;
        }
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

    public static T ReplaceWith<T>(this T oldNode, SyntaxNode newNode) where T : SyntaxNode
    {
        if (oldNode == null)
            throw new ArgumentNullException(nameof(oldNode));
        if (newNode == null)
            throw new ArgumentNullException(nameof(newNode));

        var annotation = new SyntaxAnnotation();
        var annotatedNewNode = newNode.WithAdditionalAnnotations(annotation);

        var oldRoot = oldNode.SyntaxTree.GetCompilationUnitRoot();
        var newRoot = oldRoot.ReplaceNode(oldNode, annotatedNewNode);

        var result = newRoot.GetAnnotatedNodes(annotation).OfType<T>().First() as T;
        if (result == null)
            throw new InvalidOperationException($"Failed to replace node {oldNode} with {newNode}");
        return result;
    }

    public static bool IsSameStmt(this SyntaxNode node1, SyntaxNode node2)
    {
        var ann1 = node1.GetAnnotations("StmtID").FirstOrDefault();
        var ann2 = node2.GetAnnotations("StmtID").FirstOrDefault();

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

    public static string? NestedAnnotationsAsString(this SyntaxNode node, SyntaxNode? addNode = null)
    {
        var stuff = node.GetAnnotatedNodesAndTokens(ANNOTATION_KINDS);
        if (stuff.Count() == 0)
            return null;

        HashSet<string> annotations = new();
        if (addNode != null)
        {
            foreach (var ann in addNode.GetAnnotations(ANNOTATION_KINDS))
            {
                annotations.Add($"{ann.Kind}:{ann.Data}");
            }
        }

        foreach (var item in stuff)
        {
            if (item.IsNode)
            {
                foreach (var ann in item.AsNode()!.GetAnnotations(ANNOTATION_KINDS))
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
        return node.WithTrailingTrivia(SyntaxFactory.Comment("// " + EscapeNonPrintable(comment)));
    }

    public static SyntaxNode WithUniqueAnnotation(this SyntaxNode node, SyntaxAnnotation newAnnotation)
    {
        if (newAnnotation.Kind == null)
            throw new ArgumentException("Annotation kind must not be null", nameof(newAnnotation));

        // Remove all annotations of the same kind
        var existing = node.GetAnnotations(newAnnotation.Kind);
        if (existing.Any())
            node = node.WithoutAnnotations(existing.ToArray());

        // Add the new annotation
        return node.WithAdditionalAnnotations(newAnnotation);
    }

    public static EmptyStatementSyntax ToEmptyStmt(this SyntaxNode node)
    {
        if (node is EmptyStatementSyntax thisEmpty)
            return thisEmpty;

        var annotations = new List<SyntaxAnnotation>();

        var stmtId = node.GetAnnotations("StmtID").FirstOrDefault();
        if (stmtId != null)
            annotations.Add(stmtId);

        // after StmtID to match other nodes visual output on -A
        annotations.Add(new SyntaxAnnotation("LineNo", node.LineNo().ToString()));

        return SyntaxFactory.EmptyStatement()
            .WithComment(node.Title())
            .WithAdditionalAnnotations(annotations);
    }

    public static SyntaxAnnotation? VarID(this VariableDeclaratorSyntax node) => node.Identifier.VarID();
}
