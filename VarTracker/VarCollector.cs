using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SymbolAnnotationMap = System.Collections.Generic.Dictionary<Microsoft.CodeAnalysis.ISymbol, Microsoft.CodeAnalysis.SyntaxAnnotation>;

public partial class VarTracker
{
    class VarCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel _model;
        private readonly VarTracker _tracker;
        private readonly SymbolAnnotationMap _sym2ann;
        private readonly VarDB _varDB;

        public VarCollector(VarTracker tracker, SemanticModel model, SymbolAnnotationMap sym2ann, VarDB db)
        {
            _tracker = tracker;
            _model = model;
            _sym2ann = sym2ann;
            _varDB = db;
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            if (symbol == null || _sym2ann.ContainsKey(symbol))
                return;

            ITypeSymbol typeSymbol = _model.GetTypeInfo(node).Type;

            switch (symbol)
            {
                case ILocalSymbol localSymbol:
                    typeSymbol = localSymbol.Type;
                    break;

                case IFieldSymbol fieldSymbol:
                    typeSymbol = fieldSymbol.Type;
                    break;

                default:
                    Logger.warn($"Unexpected symbol type {symbol?.GetType()}", "VarCollector.VisitVariableDeclarator");
                    return;
            }

            _sym2ann[symbol] = _varDB.Add(node, typeSymbol).Annotation;
        }
    }
}
