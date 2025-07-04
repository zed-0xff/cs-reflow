using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class OriginalLineNoAnnotator : CSharpSyntaxRewriter
{
    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node is null)
            return null;

        int lineno = node.LineNo();
        node = base.Visit(node);
        if (node is not null)
            node = node
                .WithAdditionalAnnotations(
                        new SyntaxAnnotation("LineNo", lineno.ToString())
                        );
        return node;
    }
}
