using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

// remove duplicate declarations left after flow tree unflattening
public class DuplicateDeclarationRemover : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitBlock(BlockSyntax node)
    {
        var groups = node.Statements
            .OfType<LocalDeclarationStatementSyntax>()
            .Where(decl => decl.HasAnnotations("StmtID"))
            .GroupBy(decl => decl.GetAnnotations("StmtID").First().Data)
            .Where(g => g.Count() > 1)
            .ToList();

        var newNode = node;

        foreach (var group in groups)
        {
            // Keep the first declaration, remove the rest
            var toRemove = group.Skip(1).ToList();
            newNode = newNode!.RemoveNodes(toRemove, SyntaxRemoveOptions.KeepNoTrivia);
        }

        // "int a; int a=2;"   => "int a=2;"
        // "int a=2; int a;"   => "int a=2;"
        // "int a; int a;"     => "int a;"
        // "int a=1; int a=2;" => "int a=1; int a=2;"
        bool was = false;
        var newStatements = new List<StatementSyntax>();
        var stmts = newNode!.Statements;
        for (int i = 0; i < stmts.Count; i++)
        {
            if (
                    i + 1 < stmts.Count &&
                    stmts[i] is LocalDeclarationStatementSyntax decl1 &&
                    stmts[i + 1] is LocalDeclarationStatementSyntax decl2 &&
                    decl1.IsSameVar(decl2)
                )
            {
                if (decl1.IsSameStmt(decl2) || (decl1.Declaration.Variables[0].Initializer == null && decl2.Declaration.Variables[0].Initializer == null))
                {
                    newStatements.Add(decl1);
                }
                else
                {
                    if (decl1.Declaration.Variables[0].Initializer != null)
                        newStatements.Add(decl1);
                    if (decl2.Declaration.Variables[0].Initializer != null)
                        newStatements.Add(decl2);
                }
                was = true;
                i++;
            }
            else
            {
                newStatements.Add(stmts[i]);
            }
        }

        if (was)
            newNode = newNode.WithStatements(SyntaxFactory.List(newStatements));

        // Visit the modified node further down the tree if needed
        return base.VisitBlock(newNode);
    }
}

