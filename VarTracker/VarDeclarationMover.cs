using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    class VarDeclarationMover : CSharpSyntaxRewriter
    {
        VarScopeCollector _collector;
        Dictionary<SyntaxAnnotation, BlockSyntax> _targetBlocks = new();
        HashSet<LocalDeclarationStatementSyntax> _toRemove = new();

        public VarDeclarationMover(VarScopeCollector collector)
        {
            _collector = collector;

            // Calculate target blocks
            foreach (var kvp in _collector.Declarations)
            {
                var key = kvp.Key;
                var declStmt = kvp.Value;

                // Skip declarations with more than one variable
                if (declStmt.Declaration.Variables.Count != 1)
                {
                    _targetBlocks[key] = declStmt.Ancestors().OfType<BlockSyntax>().FirstOrDefault();
                    continue;
                }

                var declBlock = declStmt.Ancestors().OfType<BlockSyntax>().FirstOrDefault();
                if (!_collector.UsageBlocks.TryGetValue(key, out var usageBlocks))
                {
                    _targetBlocks[key] = declBlock;
                    continue;
                }

                var allBlocks = new HashSet<BlockSyntax>(usageBlocks) { declBlock };
                var targetBlock = FindCommonAncestor(allBlocks);

                if (targetBlock == null)
                    throw new InvalidOperationException($"No common ancestor found for variable '{key}'");

                if (targetBlock != declBlock)
                {
                    _targetBlocks[key] = targetBlock;
                    _toRemove.Add(declStmt);
                }
                else
                {
                    _targetBlocks[key] = declBlock;
                }
            }
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
                if (current is BlockSyntax blk && blocks.All(b => b.AncestorsAndSelf().Contains(current)))
                {
                    return blk;
                }
                current = current.Parent;
            }
            return null;
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            var newNode = (BlockSyntax)base.VisitBlock(node);

            // Insert moved declarations (without initializers) at start of block
            var declsToInsert = new List<LocalDeclarationStatementSyntax>();

            foreach (var kvp in _targetBlocks)
            {
                var varName = kvp.Key;
                var targetBlock = kvp.Value;

                if (targetBlock == node && _collector.Declarations.TryGetValue(varName, out var declStmt))
                {
                    if (_toRemove.Contains(declStmt))
                    {
                        var variable = declStmt.Declaration.Variables[0];

                        // Create declaration without initializer
                        var newVariable = variable.WithInitializer(null)
                            .WithTrailingTrivia(SyntaxFactory.ElasticMarker);

                        var newDeclaration = declStmt.Declaration.WithVariables(
                            SyntaxFactory.SingletonSeparatedList(newVariable));

                        var newDeclStmt = declStmt.WithDeclaration(newDeclaration)
                            .WithTrailingTrivia(SyntaxFactory.ElasticMarker);

                        declsToInsert.Add(newDeclStmt);
                    }
                }
            }

            if (declsToInsert.Count == 0)
                return newNode;

            // Remove moved declarations from this block
            var statements = newNode.Statements.Where(s => !_toRemove.Contains(s)).ToList();

            // Insert moved declarations at the start of the block
            statements.InsertRange(0, declsToInsert);

            return newNode.WithStatements(SyntaxFactory.List(statements));
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (!_toRemove.Contains(node))
                return base.VisitLocalDeclarationStatement(node);

            // Replace removed declaration with assignment statement
            var variable = node.Declaration.Variables[0];
            if (variable.Initializer == null)
            {
                // no initializer, safe to remove declaration completely
                return null;
            }

            // Build assignment expression: varName = initializer;
            var assignmentExpr = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory
                        .IdentifierName(variable.Identifier)
                        .WithAdditionalAnnotations(variable.GetAnnotations("VAR")),
                        variable.Initializer.Value)
                    );

            return assignmentExpr;
        }
    }
}
