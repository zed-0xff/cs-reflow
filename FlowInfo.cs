using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

public class FlowInfo
{
    public int id = -1;
    public SyntaxKind Kind = SyntaxKind.None;
    public bool hasTrue = false;
    public bool hasFalse = false;
    public bool hasUnknown = false;
    public bool hasBreak = false;
    public bool hasContinue = false;
    public bool hasReturn = false;
    public bool hasOutGoto = false;
    public bool hasInterCaseGoto = false; // goto from one case into a middle of another
    public HashSet<int> loopVars = new();
    public HashSet<object?> values = new();

    public FlowInfo() { }

    public FlowInfo(SyntaxKind kind, int id = -1)
    {
        this.id = id;
        this.Kind = kind;
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<FlowInfo");
        if (id != -1)
            sb.Append($" @{id}");
        if (Kind != SyntaxKind.None)
            sb.Append($" {Kind}");
        if (hasTrue)
            sb.Append(" hasTrue");
        if (hasFalse)
            sb.Append(" hasFalse");
        if (hasUnknown)
            sb.Append(" hasUnknown");
        if (hasBreak)
            sb.Append(" hasBreak");
        if (hasContinue)
            sb.Append(" hasContinue");
        if (hasOutGoto)
            sb.Append(" hasOutGoto");
        if (hasInterCaseGoto)
            sb.Append(" hasInterCaseGoto");
        if (hasReturn)
            sb.Append(" hasReturn");
        if (loopVars.Count > 0)
            sb.Append($" loopVars=[{String.Join(", ", loopVars)}]");
        if (values.Count > 0)
        {
            if (values.Count > 5)
                sb.Append($" values=[{String.Join(", ", values.Take(5))}, …][{values.Count}]");
            else if (values.Count == 1 && values.First() is bool)
            {
                // skip
            }
            else
            {
                sb.Append($" values=[{String.Join(", ", values)}]");
            }
        }
        sb.Append(">");

        return sb.ToString();
    }
}
