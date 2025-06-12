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

    public SyntaxNode MoveDeclarations(SyntaxNode rootNode)
    {
        // Collect variable declarations and usage blocks
        var collector = new VarScopeCollector();
        collector.Visit(rootNode);

        // Move declarations to appropriate blocks
        var mover = new VarDeclarationMover(collector);
        return mover.Visit(rootNode);
    }
}

