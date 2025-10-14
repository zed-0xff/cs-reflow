using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SymbolAnnotationMap = System.Collections.Generic.Dictionary<Microsoft.CodeAnalysis.ISymbol, Microsoft.CodeAnalysis.SyntaxAnnotation>;

public partial class VarTracker
{
    class SwitchVarCollector : CSharpSyntaxWalker
    {
        private readonly VarTracker _tracker;
        private readonly VarDB _varDB;

        public SwitchVarCollector(VarTracker tracker, VarDB db)
        {
            _tracker = tracker;
            _varDB = db;
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            foreach (int var_id in _varDB.CollectVars(node.Expression).read)
                _varDB.SetSwitchVar(var_id);

            base.VisitSwitchStatement(node);
        }
    }
}
