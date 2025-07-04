using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    public record DeclInfo(LocalDeclarationStatementSyntax decl, BlockSyntax block);

    class VarScopeCollector : CSharpSyntaxWalker
    {
        public Dictionary<int, List<DeclInfo>> DeclInfos = new();
        public Dictionary<int, HashSet<BlockSyntax>> UsageBlocks = new();

        private readonly VarDB _varDB;
        private Stack<BlockSyntax> _blockStack = new();

        public VarScopeCollector(VarDB varDB)
        {
            _varDB = varDB;
        }

        public override void VisitBlock(BlockSyntax node)
        {
            _blockStack.Push(node);
            base.VisitBlock(node);
            _blockStack.Pop();
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (node.Declaration.Variables.Count == 1) // skip multi-variable declarations
            {
                foreach (var v in node.Declaration.Variables)
                {
                    var annotation = v.VarID();
                    if (annotation is null)
                        continue;

                    var V = _varDB[annotation];
                    if (!DeclInfos.TryGetValue(V.id, out var decls))
                    {
                        decls = new List<DeclInfo>();
                        DeclInfos[V.id] = decls;
                    }
                    decls.Add(new DeclInfo(node, _blockStack.Peek()));
                }
            }
            base.VisitLocalDeclarationStatement(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var annotation = node.VarID();
            if (annotation is not null)
            {
                if (!_blockStack.Any()) return; // safety

                var V = _varDB[annotation];
                if (!UsageBlocks.TryGetValue(V.id, out var blocks))
                {
                    blocks = new HashSet<BlockSyntax>();
                    UsageBlocks[V.id] = blocks;
                }
                blocks.Add(_blockStack.Peek());
            }
            base.VisitIdentifierName(node);
        }
    }
}
