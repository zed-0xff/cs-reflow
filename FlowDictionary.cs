using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class FlowDictionary : System.Collections.Generic.Dictionary<Microsoft.CodeAnalysis.SyntaxNode, ControlFlowNode>
{
    public FlowDictionary Clone()
    {
        FlowDictionary clone = new();
        foreach (var kvp in this)
        {
            clone[kvp.Key] = kvp.Value; // XXX cannot deep-clone ControlFlowNode bc it has references to other nodes
        }
        return clone;
    }
}
