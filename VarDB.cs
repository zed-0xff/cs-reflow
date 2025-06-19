using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

public class VarDB
{
    int _var_id = 0;

    Dictionary<SyntaxAnnotation, Variable> _ann2vars = new();
    Dictionary<int, Variable> _vars = new();

    public Variable? TryFind(SyntaxAnnotation annotation)
    {
        Variable? v = null;
        if (!_ann2vars.TryGetValue(annotation, out v))
        {
            int id = int.Parse(annotation.Data, System.Globalization.NumberStyles.HexNumber);
            _vars.TryGetValue(id, out v);
        }
        Logger.debug(() => $"{annotation.Data}: {v}", "VarDB.TryFind");
        return v;
    }

    public Variable Find(SyntaxAnnotation annotation)
    {
        if (!_ann2vars.TryGetValue(annotation, out var v))
        {
            Logger.error($"Variable not found for annotation {annotation.Data}", "VarDB.Find");
            throw new KeyNotFoundException($"Variable not found for annotation {annotation.Data}");
        }
        Logger.debug(() => $"{annotation.Data}: {v}", "VarDB.Find");
        return v;
    }

    public Variable Add(VariableDeclaratorSyntax node, ITypeSymbol type)
    {
        int id = ++_var_id;
        var v = new Variable(id, node, type);
        _ann2vars[v.Annotation] = v;
        _vars[id] = v;
        Logger.debug(() => $"{v}: ({type}) {node}", "VarDB.Add");
        return v;
    }
}
