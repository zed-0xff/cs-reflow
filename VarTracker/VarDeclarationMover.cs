using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    class VarDeclarationMover : CSharpSyntaxRewriter
    {
        static readonly string LOG_TAG = "VarDeclarationMover";

        readonly VarDB _varDB;
        readonly VarTracker _tracker;
        readonly VarScopeCollector _collector;
        Dictionary<int, BlockSyntax> _targetBlocks = new();
        HashSet<LocalDeclarationStatementSyntax> _toRemove = new();

        static bool IsSafeToRemove(LocalDeclarationStatementSyntax decl)
        {
            return decl.Declaration.Variables.Count == 1; // && decl.Declaration.Variables[0].Initializer is null;
        }

        void enqueue_removal(LocalDeclarationStatementSyntax decl)
        {
            if (IsSafeToRemove(decl))
            {
                _toRemove.Add(decl);
            }
            else
            {
                Logger.debug($"Cannot remove declaration {decl.TitleWithLineNo()}: it has initializer or multiple variables", LOG_TAG);
            }
        }

        Dictionary<BlockSyntax, LocalDeclarationStatementSyntax> collect_block_decls(int key)
        {
            Dictionary<BlockSyntax, LocalDeclarationStatementSyntax> blockDecls = new();
            foreach (var declInfo in _collector.DeclInfos[key])
            {
                if (blockDecls.TryGetValue(declInfo.block, out var existingDecl))
                {
                    // If we already have a declaration in this block, keep the topmost one
                    int oldIdx = declInfo.block.IndexWithLabels(existingDecl);
                    int newIdx = declInfo.block.IndexWithLabels(declInfo.decl);
                    if (oldIdx == -1 || newIdx == -1)
                    {
                        Console.Error.WriteLine(declInfo.block);
                        throw new InvalidOperationException($"Declaration for {_varDB[key]} not found ({oldIdx}, {newIdx}) in block statements: {declInfo.block.TitleWithLineNo()}");
                    }
                    if (newIdx < oldIdx)
                    {
                        blockDecls[declInfo.block] = declInfo.decl;
                        enqueue_removal(existingDecl);
                    }
                    else
                    {
                        enqueue_removal(declInfo.decl);
                    }
                }
                else
                    blockDecls[declInfo.block] = declInfo.decl;
            }
            return blockDecls;
        }

        public VarDeclarationMover(VarTracker tracker, VarScopeCollector collector)
        {
            _varDB = tracker.VarDB;
            _tracker = tracker;
            _collector = collector;

            foreach (var (key, usageBlocks) in _collector.UsageBlocks)
            {
                if (!_collector.DeclInfos.ContainsKey(key))
                    continue;

                var blockDecls = collect_block_decls(key); // also adds duplicate decls to _toRemove
                var declBlocks = _collector.DeclInfos[key].Select(d => d.block).ToHashSet();
                var usageToDecl = new Dictionary<BlockSyntax, BlockSyntax>();
                var declToUsage = new Dictionary<BlockSyntax, List<BlockSyntax>>();
                var orphanedUsageBlocks = new List<BlockSyntax>();

                foreach (var usageBlk in usageBlocks)
                {
                    BlockSyntax? closestDecl = null;

                    foreach (var ancestor in usageBlk.AncestorsAndSelf())
                    {
                        if (declBlocks.Contains(ancestor))
                        {
                            closestDecl = (BlockSyntax)ancestor;
                            break;
                        }
                    }

                    if (closestDecl is not null)
                    {
                        usageToDecl[usageBlk] = closestDecl;
                        if (!declToUsage.TryGetValue(closestDecl, out var usages))
                        {
                            usages = new List<BlockSyntax>();
                            declToUsage[closestDecl] = usages;
                        }
                        usages.Add(usageBlk);
                    }
                    else
                        orphanedUsageBlocks.Add(usageBlk);
                }

                foreach (var usageBlk in orphanedUsageBlocks)
                    fix_orphaned_block(_varDB[key], usageBlk);

                var unusedDecls = _collector.DeclInfos[key]
                    .Where(d => !declToUsage.ContainsKey(d.block))
                    .Select(d => d.decl);

                foreach (var decl in unusedDecls)
                {
                    Logger.debug($"{_varDB[key]}: unused decl: {decl.TitleWithLineNo()}", LOG_TAG);
                    _toRemove.Add(decl);
                }
            }
        }

        void fix_orphaned_block(Variable V, BlockSyntax usageBlk)
        {
            Logger.debug($"{V}: usage block has no decl: {usageBlk.TitleWithLineNo()}", LOG_TAG);
            BlockSyntax? targetBlk = usageBlk
                .AncestorsAndSelf()
                .OfType<BlockSyntax>()
                .FirstOrDefault(b => b.DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Any(ld => ld.IsSameVar(V)));

            if (targetBlk is null)
                throw new TaggedException(LOG_TAG, $"{V}: No parent block found for orphaned usage block {usageBlk.TitleWithLineNo()}");

            // targetBlk itself won't have the declaration of interest, but some of its children do
            targetBlk.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>()
                .Where(ld => ld.IsSameVar(V))
                .ToList()
                .ForEach(ld => enqueue_removal(ld));

            if (_targetBlocks.TryGetValue(V.id, out var existingTarget))
            {
                if (existingTarget != targetBlk)
                {
                    var commonAncestor = FindCommonAncestor(new[] { existingTarget, targetBlk });
                    if (commonAncestor is null)
                        throw new TaggedException(LOG_TAG, $"{V}: No common ancestor found for {existingTarget.TitleWithLineNo()} and {targetBlk.TitleWithLineNo()}");
                    _targetBlocks[V.id] = commonAncestor;
                }
            }
            else
                _targetBlocks[V.id] = targetBlk;
        }

        // Helper to find common ancestor block of many blocks
        static BlockSyntax? FindCommonAncestor(IEnumerable<BlockSyntax> blocks)
        {
            // naive approach: pick first block and climb its parents until all blocks contain that parent
            var first = blocks.FirstOrDefault();
            if (first is null) return null;

            SyntaxNode? current = first;
            while (current is not null)
            {
                if (current is BlockSyntax blk && blocks.All(b => b.AncestorsAndSelf().Contains(current)))
                {
                    return blk;
                }
                current = current.Parent;
            }
            return null;
        }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var newNode = base.VisitBlock(node) as BlockSyntax;

            // Insert moved declarations (without initializers) at start of block
            var declsToInsert = new List<LocalDeclarationStatementSyntax>();

            foreach (var kvp in _targetBlocks)
            {
                var varName = kvp.Key;
                var targetBlock = kvp.Value;

                if (targetBlock == node && _collector.DeclInfos.TryGetValue(varName, out var decls))
                {
                    foreach (DeclInfo di in decls)
                    {
                        var declStmt = di.decl;
                        if (!_toRemove.Contains(declStmt))
                            continue;

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
            var statements = newNode!.Statements.Where(s => !_toRemove.Contains(s)).ToList();

            // Insert moved declarations at the start of the block
            statements.InsertRange(0, declsToInsert);

            return newNode.WithStatements(SyntaxFactory.List(statements));
        }

        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (!_toRemove.Contains(node))
                return base.VisitLocalDeclarationStatement(node);

            // Replace removed declaration with assignment statement
            var variable = node.Declaration.Variables[0];
            if (variable.Initializer is null)
            {
                // no initializer, safe to remove declaration completely
                return node.ToEmptyStmt();
            }

            // Build assignment expression: varName = initializer;
            var assignmentExpr = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory
                            .IdentifierName(variable.Identifier)
                            .WithAdditionalAnnotations(variable.GetAnnotations(new string[] { "VarID", "LineNo" })),
                        variable.Initializer.Value)
                    .WithAdditionalAnnotations(new SyntaxAnnotation("StmtID", _tracker.NextStmtID()))
                    );

            return assignmentExpr;
        }
    }
}
