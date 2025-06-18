using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    // Maps variable symbol -> unique annotation
    public readonly Dictionary<ISymbol, SyntaxAnnotation> _varAnnotations = new Dictionary<ISymbol, SyntaxAnnotation>(SymbolEqualityComparer.Default);

    int _id = 0;

    public SyntaxNode Track(SyntaxNode rootNode)
    {
        var ctx = new SemanticContext(rootNode);
        return IndexSymbols(rootNode, ctx.Model);
    }

    public string GetNextId()
    {
        return (++_id).ToString("X4");
    }

    public SyntaxNode IndexSymbols(SyntaxNode rootNode, SemanticModel semanticModel)
    {
        // First pass: collect and annotate declarators
        var collector = new VarCollector(semanticModel, _varAnnotations);
        collector.Visit(rootNode);

        // Second pass: rewrite and apply annotations
        var rewriter = new AnnotationRewriter(this, semanticModel);
        return rewriter.Visit(rootNode);
    }

    public SyntaxNode MoveDeclarations(SyntaxNode rootNode)
    {
        // Collect variable declarations and usage blocks
        var collector = new VarScopeCollector();
        collector.Visit(rootNode);

        // Move declarations to appropriate blocks
        var mover = new VarDeclarationMover(this, collector);
        return mover.Visit(rootNode);
    }
}

