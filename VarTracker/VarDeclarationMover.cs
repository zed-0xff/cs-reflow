using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    class VarDeclarationMover : CSharpSyntaxRewriter
    {
        private VarScopeCollector _collector;
        private Dictionary<string, BlockSyntax> _targetBlocks;
        private HashSet<LocalDeclarationStatementSyntax> _toRemove;

        public VarDeclarationMover(VarScopeCollector collector)
        {
            _collector = collector;
            _toRemove = new HashSet<LocalDeclarationStatementSyntax>();
            _targetBlocks = new Dictionary<string, BlockSyntax>();

            // Calculate target blocks
            foreach (var kvp in _collector.Declarations)
            {
                var key = kvp.Key;
                var declBlock = kvp.Value.Ancestors().OfType<BlockSyntax>().FirstOrDefault();
                if (!_collector.UsageBlocks.TryGetValue(key, out var usageBlocks))
                {
                    // no usage? skip
                    _targetBlocks[key] = declBlock;
                    continue;
                }

                var allBlocks = new HashSet<BlockSyntax>(usageBlocks) { declBlock };
                var targetBlock = FindCommonAncestor(allBlocks);

                if (targetBlock != declBlock)
                {
                    _targetBlocks[key] = targetBlock;
                    _toRemove.Add(kvp.Value);
                }
                else
                {
                    _targetBlocks[key] = declBlock;
                }
            }
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            // First visit children
            var newNode = (BlockSyntax)base.VisitBlock(node);

            // Insert declarations for variables that should move here
            var declsToInsert = new List<LocalDeclarationStatementSyntax>();

            foreach (var kvp in _targetBlocks)
            {
                var varName = kvp.Key;
                var targetBlock = kvp.Value;

                if (targetBlock == node && _collector.Declarations.TryGetValue(varName, out var declStmt))
                {
                    if (_toRemove.Contains(declStmt))
                    {
                        declsToInsert.Add(declStmt);
                    }
                }
            }

            if (declsToInsert.Count == 0)
                return newNode;

            // Remove moved declarations from this block if any
            var statements = newNode.Statements.Where(s => !_toRemove.Contains(s)).ToList();

            // Insert moved declarations at the start
            statements.InsertRange(0, declsToInsert);

            return newNode.WithStatements(SyntaxFactory.List(statements));
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (_toRemove.Contains(node))
                return null; // remove from original position
            return base.VisitLocalDeclarationStatement(node);
        }
    }
}
