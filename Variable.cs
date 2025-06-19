using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class Variable
{
    public readonly int id;
    public int Flags = 0;
    public readonly string Name;
    public readonly string TypeName;

    public readonly SyntaxAnnotation Annotation;
    public readonly VariableDeclaratorSyntax Declarator;
    public readonly TypeDB.IntInfo? IntType;

    public const int FLAG_SWITCH = 1;
    public const int FLAG_LOOP = 2;

    public Variable(int id, VariableDeclaratorSyntax node, ITypeSymbol type)
    {
        this.Declarator = node;
        this.id = id;
        this.Name = node.Identifier.ValueText;
        this.Annotation = new SyntaxAnnotation("VarID", id.ToString("X4"));
        this.IntType = TypeDB.TryFind(type.ToString());
        this.TypeName = type.ToString();
    }

    public override string ToString() => $"Var{id:X4} ({TypeName} {Name ?? "?"})";
}
