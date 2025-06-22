using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class Variable
{
    public readonly int id;
    public int Flags = 0;
    public readonly string Name;
    public readonly string TypeName;

    public readonly SyntaxAnnotation Annotation;
    public readonly TypeDB.IntInfo? IntType;

    public const int FLAG_LOOP = 1;

    public Variable(int id, string name, string typeName)
    {
        this.id = id;
        this.Name = name;
        this.Annotation = new SyntaxAnnotation("VarID", id.ToString("X4"));
        this.IntType = TypeDB.TryFind(typeName);
        this.TypeName = typeName;
    }

    public bool IsLoopVar => (Flags & FLAG_LOOP) != 0;

    public override string ToString() => $"<Var{id:X4}.{Name ?? "?"}>";
    public string ToFullString() => $"<Var{id:X4} type={TypeName} name={Name ?? "?"}>";
}
