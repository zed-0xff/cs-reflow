using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

public class VarDB
{
    int _var_id = 0;

    Dictionary<SyntaxAnnotation, Variable> _ann2vars = new();
    Dictionary<int, Variable> _vars = new();
    Dictionary<string, Variable> _name2vars = new();

    public Variable this[int id] => _vars[id];
    public Variable? FindByName(string name) => _name2vars.TryGetValue(name, out var variable) ? variable : null;

    public bool TryGetValue(string varName, out Variable? variable) => _name2vars.TryGetValue(varName, out variable);
    public bool TryGetValue(SyntaxToken token, out Variable? variable)
    {
        var ann = token.VarID();
        if (ann == null)
        {
            Logger.warn_once($"Variable definition not found for {token}", "VarDB.TryGetValue");
            return TryGetValue(token.ToString(), out variable);
        }
        return _ann2vars.TryGetValue(ann, out variable);
    }

    public Variable Add(VariableDeclaratorSyntax node, string typeName) => Add(node.Identifier.ValueText, typeName);
    public Variable Add(string name, string typeName)
    {
        int id = _var_id++;
        var v = new Variable(id, name, typeName);
        _ann2vars[v.Annotation] = v;
        _vars[id] = v;
        _name2vars[v.Name] = v;
        Logger.debug(() => $"{v}: ({typeName}) {name}", "VarDB.Add");
        return v;
    }

    public void SetLoopVar(int id) => _vars[id].Flags |= Variable.FLAG_LOOP;
}
