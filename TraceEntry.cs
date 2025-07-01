using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public class TraceEntry
{
    public StatementSyntax stmt;
    public readonly object? value;
    public readonly VarDict vars;
    public string? comment;

    public TraceEntry(StatementSyntax stmt, object? value, VarDict vars, string? comment = null)
    {
        this.stmt = stmt;
        this.value = value;
        this.vars = (VarDict)vars.Clone();
        this.comment = comment;
    }

    public string Title()
    {
        string result = stmt.Title();
        if (comment != null && comment.Length > 0)
        {
            result += " // " + comment;
        }
        return result;
    }

    public string TitleWithLineNo()
    {
        return $"{stmt.LineNo()}: {Title()}";
    }

    public string StmtWithLineNo()
    {
        return $"{stmt.LineNo()}: {stmt}";
    }

    public string FormatStmtWithLineNo()
    {
        return $"{stmt.LineNo()}: {PostProcessor.RemoveAllComments(stmt).NormalizeWhitespace()}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not TraceEntry other) return false;

        return stmt.Equals(other.stmt) && vars.Equals(other.vars);
    }

    public override string ToString() => TitleWithLineNo();
    public override int GetHashCode() => HashCode.Combine(stmt, vars, value);
}

