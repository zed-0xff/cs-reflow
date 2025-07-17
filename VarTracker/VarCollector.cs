using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SymbolAnnotationMap = System.Collections.Generic.Dictionary<Microsoft.CodeAnalysis.ISymbol, Microsoft.CodeAnalysis.SyntaxAnnotation>;

public partial class VarTracker
{
    class VarCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly VarTracker _tracker;
        private readonly SymbolAnnotationMap _sym2ann;
        private readonly VarDB _varDB;
        private readonly HashSet<IFieldSymbol> _fields = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);

        static readonly TaggedLogger _logger = new("VarCollector");

        public VarCollector(VarTracker tracker, SemanticModel model, SymbolAnnotationMap sym2ann, VarDB db)
        {
            _tracker = tracker;
            _semanticModel = model;
            _sym2ann = sym2ann;
            _varDB = db;
        }

        public void Process(SyntaxNode rootNode)
        {
            Visit(rootNode);

            // _fields may contain symbols declared in upper scopes
            foreach (var field in _fields)
            {
                if (!_sym2ann.ContainsKey(field))
                {
                    if (VarProcessor.Constants.ContainsKey(field.ToString()!))
                    {
                        _logger.debug($"Field {field} is a known constant");
                        continue;
                    }
                    _sym2ann[field] = _varDB.Add(field).Annotation;
                }
            }
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.IsKnownConstant())
                return;

            var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol is IFieldSymbol fieldSymbol)
                _fields.Add(fieldSymbol);
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node);
            if (symbol is null || _sym2ann.ContainsKey(symbol))
                return;

            if (node.Type is null)
            {
                _logger.warn($"Parameter \"{node}\" has no type information");
                return;
            }

            var typeSymbol = _semanticModel.GetTypeInfo(node.Type).Type;
            if (typeSymbol is null)
            {
                _logger.warn($"Parameter \"{node}\" has no type information");
                return;
            }

            _sym2ann[symbol] = _varDB.Add(node, typeSymbol.ToString()!).Annotation;
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node);
            _logger.debug(() => $"node={node}, symbol={symbol?.ToDisplayString() ?? "null"}");
            if (symbol is null || _sym2ann.ContainsKey(symbol))
                return;

            var typeSymbol = _semanticModel.GetTypeInfo(node).Type;

            switch (symbol)
            {
                case ILocalSymbol localSymbol:
                    typeSymbol = localSymbol.Type;
                    break;

                case IFieldSymbol fieldSymbol:
                    typeSymbol = fieldSymbol.Type;
                    break;

                default:
                    _logger.warn($"Unexpected symbol type {symbol?.GetType()}");
                    return;
            }

            _sym2ann[symbol] = _varDB.Add(node, typeSymbol.ToString()!).Annotation;
            base.VisitVariableDeclarator(node); // if declarator is/has a lambda, then collect lambda's vars as well
        }
    }
}
