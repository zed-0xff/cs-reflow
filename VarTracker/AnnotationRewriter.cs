using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SymbolAnnotationMap = System.Collections.Generic.Dictionary<Microsoft.CodeAnalysis.ISymbol, Microsoft.CodeAnalysis.SyntaxAnnotation>;

public partial class VarTracker
{
    class AnnotationRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;
        private readonly VarTracker _tracker;
        private readonly SymbolAnnotationMap _sym2ann;

        public AnnotationRewriter(VarTracker tracker, SemanticModel semanticModel, SymbolAnnotationMap sym2ann)
        {
            _tracker = tracker;
            _semanticModel = semanticModel;
            _sym2ann = sym2ann;
        }

        public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            // capture annotation first bc base.VisitVariableDeclarator may return rewritten node, so semantic model would not match
            SyntaxAnnotation? annotation = null;
            if (node.SyntaxTree == _semanticModel.SyntaxTree)
            {
                var key = _semanticModel.GetDeclaredSymbol(node);
                if (key != null)
                    _sym2ann.TryGetValue(key, out annotation);
            }

            var newNode = base.VisitVariableDeclarator(node) as VariableDeclaratorSyntax;
            if (newNode == null)
                return null;

            if (annotation != null)
                newNode = newNode.WithIdentifier(
                        newNode.Identifier.WithAdditionalAnnotations(annotation)
                        );
            return newNode;
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var newNode = base.VisitIdentifierName(node) as IdentifierNameSyntax;
            if (newNode == null || newNode.SyntaxTree != _semanticModel.SyntaxTree)
                return newNode;

            var symbol = _semanticModel.GetSymbolInfo(newNode).Symbol;
            if (symbol != null && _sym2ann.TryGetValue(symbol, out var annotation))
                newNode = newNode.WithIdentifier(
                        newNode.Identifier.WithAdditionalAnnotations(annotation)
                        );
            return newNode;
        }

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            node = base.Visit(node);
            if (node is StatementSyntax)
            {
                node = node.WithAdditionalAnnotations(
                        new SyntaxAnnotation("StmtID", _tracker.NextStmtID())
                        );
            }
            return node;
        }
    }
}
