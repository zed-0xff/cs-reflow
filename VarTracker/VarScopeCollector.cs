using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    public record DeclInfo(LocalDeclarationStatementSyntax decl, BlockSyntax block);

    class VarScopeCollector : CSharpSyntaxWalker
    {
        public Dictionary<SyntaxAnnotation, List<DeclInfo>> DeclInfos = new();
        public Dictionary<SyntaxAnnotation, HashSet<BlockSyntax>> UsageBlocks = new();

        private Stack<BlockSyntax> _blockStack = new();

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
                    if (annotation == null)
                        continue;

                    if (!DeclInfos.TryGetValue(annotation, out var decls))
                    {
                        decls = new List<DeclInfo>();
                        DeclInfos[annotation] = decls;
                    }
                    decls.Add(new DeclInfo(node, _blockStack.Peek()));
                }
            }
            base.VisitLocalDeclarationStatement(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var annotation = node.VarID();
            if (annotation != null)
            {
                if (!_blockStack.Any()) return; // safety

                if (!UsageBlocks.TryGetValue(annotation, out var blocks))
                {
                    blocks = new HashSet<BlockSyntax>();
                    UsageBlocks[annotation] = blocks;
                }
                blocks.Add(_blockStack.Peek());
            }
            base.VisitIdentifierName(node);
        }
    }
}
