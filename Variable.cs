using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class Variable
{
    public readonly int id;
    public int Flags = 0;
    public readonly string Name;
    public readonly string TypeName;
    public readonly string VarID;
    public readonly bool IsConst = false;
    public readonly object? ConstValue = null;

    public readonly SyntaxAnnotation Annotation;
    public readonly TypeDB.IntType? IntType;

    public const int FLAG_LOOP = 1;

    public Variable(int id, string name, string typeName)
    {
        this.id = id;
        this.Name = name;
        this.VarID = id.ToString("X4");
        this.Annotation = new SyntaxAnnotation("VarID", VarID);
        this.IntType = TypeDB.TryFind(typeName);
        this.TypeName = typeName;
    }

    // add const
    public Variable(int id, string name, string typeName, object? value) : this(id, name, typeName)
    {
        this.IsConst = true;
        this.ConstValue = value;
    }

    public bool IsLoopVar => (Flags & FLAG_LOOP) != 0;

    public override string ToString() => $"<Var{VarID}.{Name ?? "?"}>";
    public string ToFullString() => $"<Var{VarID} type={TypeName} name={Name ?? "?"}>";
}
