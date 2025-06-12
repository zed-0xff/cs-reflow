using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class VariableTracker
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
        var collector = new VariableCollector(semanticModel, _varAnnotations);
        collector.Visit(rootNode);

        // Second pass: rewrite and apply annotations
        var rewriter = new AnnotationRewriter(_varAnnotations, semanticModel);
        return rewriter.Visit(rootNode);
    }

    class VariableCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel _model;
        private readonly Dictionary<ISymbol, SyntaxAnnotation> _dict;
        int _vid = 0;

        public VariableCollector(SemanticModel model, Dictionary<ISymbol, SyntaxAnnotation> dict)
        {
            _model = model;
            _dict = dict;
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            if (symbol != null && !_dict.ContainsKey(symbol))
                _dict[symbol] = new SyntaxAnnotation("VAR", $"{_vid++}");
        }
    }

    class AnnotationRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<ISymbol, SyntaxAnnotation> _varAnnotations;
        private readonly SemanticModel _semanticModel;
        int _id = 0;

        public AnnotationRewriter(Dictionary<ISymbol, SyntaxAnnotation> varAnnotations, SemanticModel semanticModel)
        {
            _varAnnotations = varAnnotations;
            _semanticModel = semanticModel;
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            node = base.VisitVariableDeclarator(node) as VariableDeclaratorSyntax;
            if (node.SyntaxTree == _semanticModel.SyntaxTree)
            {
                var symbol = _semanticModel.GetDeclaredSymbol(node);
                if (symbol != null && _varAnnotations.TryGetValue(symbol, out var annotation))
                    node = node.WithAdditionalAnnotations(annotation);
            }
            return node;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            node = base.VisitIdentifierName(node) as IdentifierNameSyntax;
            if (node.SyntaxTree == _semanticModel.SyntaxTree)
            {
                var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
                if (symbol != null && _varAnnotations.TryGetValue(symbol, out var annotation))
                    node = node.WithAdditionalAnnotations(annotation);
            }
            return node;
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            return base.VisitLocalDeclarationStatement(node)
                .WithAdditionalAnnotations(
                        new SyntaxAnnotation("ID", $"{_id++}")
                        );
        }
    }

    public class ShowAnnotationsRewriter : CSharpSyntaxRewriter
    {
        T add_comment<T>(T node) where T : SyntaxNode
        {
            if (node != null)
            {
                string? ann_str = node.NestedAnnotationsAsString();
                if (ann_str != null && ann_str.Length < 100)
                    node = node.WithTrailingTrivia(SyntaxFactory.Comment(" // " + ann_str));
            }
            return node;
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return base.VisitExpressionStatement(add_comment(node));
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            return base.VisitLocalDeclarationStatement(add_comment(node));
        }
    }
}

