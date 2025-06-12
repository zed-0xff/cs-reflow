using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    class VarScopeCollector : CSharpSyntaxWalker
    {
        public Dictionary<SyntaxAnnotation, LocalDeclarationStatementSyntax> Declarations = new();
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
            foreach (var v in node.Declaration.Variables)
            {
                var annotation = v.GetAnnotations("VAR").FirstOrDefault();
                if (annotation != null)
                {
                    Declarations[annotation] = node;
                }
            }
            base.VisitLocalDeclarationStatement(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var annotation = node.GetAnnotations("VAR").FirstOrDefault();
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
