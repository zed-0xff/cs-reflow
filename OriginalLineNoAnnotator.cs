using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class OriginalLineNoAnnotator : CSharpSyntaxRewriter
{
    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node == null)
            return null;

        int lineno = node.LineNo();
        node = base.Visit(node);
        if (node != null)
            node = node
                .WithAdditionalAnnotations(
                        new SyntaxAnnotation("LineNo", lineno.ToString())
                        );
        return node;
    }
}
