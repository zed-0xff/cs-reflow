using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class SemanticContext
{
    public SemanticModel Model;
    SyntaxTree _tree;
    CSharpCompilation _compilation;

    static int _aidx = 0;

    public SemanticContext(SyntaxNode rootNode)
    {
        _compilation = CSharpCompilation.Create($"MyAnalysis{_aidx++}")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(rootNode.SyntaxTree.GetCompilationUnitRoot().SyntaxTree);
        _tree = rootNode.SyntaxTree.GetCompilationUnitRoot().SyntaxTree;
        Model = _compilation.GetSemanticModel(rootNode.SyntaxTree);
    }

    public void Update(SyntaxNode newRoot)
    {
        var newTree = newRoot.SyntaxTree.GetCompilationUnitRoot().SyntaxTree;
        _compilation = _compilation.ReplaceSyntaxTree(_tree, newTree);
        _tree = newTree;
        Model = _compilation.GetSemanticModel(newTree);
    }
}
