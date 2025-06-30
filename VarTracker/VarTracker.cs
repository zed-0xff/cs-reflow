using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SymbolAnnotationMap = System.Collections.Generic.Dictionary<Microsoft.CodeAnalysis.ISymbol, Microsoft.CodeAnalysis.SyntaxAnnotation>;

public partial class VarTracker
{
    int _stmt_id = 0;
    readonly VarDB _varDB;
    public VarDB VarDB => _varDB;

    public VarTracker(VarDB varDB)
    {
        _varDB = varDB;
    }

    public string NextStmtID() => (++_stmt_id).ToString("X4");

    public SyntaxNode? Track(SyntaxNode rootNode)
    {
        var ctx = new SemanticContext(rootNode);
        return IndexSymbols(rootNode, ctx.Model);
    }

    public SyntaxNode? IndexSymbols(SyntaxNode rootNode, SemanticModel semanticModel)
    {
        // Maps variable symbol -> unique annotation
        // Symbols are invalidated when the syntax tree is modified (i.e. just after this method is finished), Annotations are not.
        var sym2ann = new SymbolAnnotationMap(SymbolEqualityComparer.Default);

        // First pass: collect and annotate declarators
        var collector = new VarCollector(this, semanticModel, sym2ann, _varDB);
        collector.Process(rootNode);

        // Second pass: rewrite and apply annotations
        var rewriter = new AnnotationRewriter(this, semanticModel, sym2ann);
        return rewriter.Visit(rootNode);
    }

    public SyntaxNode MoveDeclarations(SyntaxNode rootNode)
    {
        // Collect variable declarations and usage blocks
        var collector = new VarScopeCollector(_varDB);
        collector.Visit(rootNode);

        // Move declarations to appropriate blocks
        var mover = new VarDeclarationMover(this, collector);
        return mover.Visit(rootNode);
    }
}

