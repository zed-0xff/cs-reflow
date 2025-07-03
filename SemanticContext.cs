using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class SemanticContext
{
    public SemanticModel Model;
    SyntaxTree _tree;
    CSharpCompilation _compilation;

    static int _aidx = 0;

    public SemanticContext(SyntaxNode rootNode, IEnumerable<SyntaxTree>? trees = null)
    {
        _tree = rootNode.SyntaxTree;
        _compilation = CSharpCompilation.Create($"MyAnalysis{_aidx++}")
            .AddReferences(
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                    )
            .AddSyntaxTrees(_tree);
        if (trees != null)
            _compilation = _compilation.AddSyntaxTrees(trees);
        Model = _compilation.GetSemanticModel(_tree);
    }

    public void Update(SyntaxNode newRoot)
    {
        var newTree = newRoot.SyntaxTree;
        _compilation = _compilation.ReplaceSyntaxTree(_tree, newTree);
        _tree = newTree;
        Model = _compilation.GetSemanticModel(newTree);
    }
}
