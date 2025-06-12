using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    // Maps variable symbol -> unique annotation
    private readonly Dictionary<ISymbol, SyntaxAnnotation> _varAnnotations = new Dictionary<ISymbol, SyntaxAnnotation>(SymbolEqualityComparer.Default);

    public SyntaxNode Track(SyntaxNode rootNode)
    {
        var ctx = new SemanticContext(rootNode);
        return IndexSymbols(rootNode, ctx.Model);
    }

    // Call this to get annotation for a declared variable symbol
    public SyntaxAnnotation GetAnnotation(ISymbol symbol) =>
        _varAnnotations.TryGetValue(symbol, out var annotation) ? annotation : null;

    public SyntaxNode IndexSymbols(SyntaxNode rootNode, SemanticModel semanticModel)
    {
        // First pass: collect and annotate declarators
        var collector = new VarCollector(semanticModel, _varAnnotations);
        collector.Visit(rootNode);

        // Second pass: rewrite and apply annotations
        var rewriter = new AnnotationRewriter(_varAnnotations, semanticModel);
        return rewriter.Visit(rootNode);
    }

    // Helper to find common ancestor block of many blocks
    static BlockSyntax FindCommonAncestor(IEnumerable<BlockSyntax> blocks)
    {
        // naive approach: pick first block and climb its parents until all blocks contain that parent
        var first = blocks.FirstOrDefault();
        if (first == null) return null;

        SyntaxNode current = first;
        while (current != null)
        {
            if (blocks.All(b => b.AncestorsAndSelf().Contains(current)))
                return current as BlockSyntax;
            current = current.Parent;
        }
        return null;
    }
}

