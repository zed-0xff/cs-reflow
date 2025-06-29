using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

public class VarDict
{
    static readonly string TAG = "VarDict";
    private static readonly TaggedLogger _logger = new(TAG);
    public const int FLAG_LOOP = 1;

    DefaultDict<int, int> _flags = new();
    Dictionary<int, object?> _values = new();
    readonly VarDB _varDB;

    public VarDict(VarDB db)
    {
        _varDB = db;
    }

    public IReadOnlyDictionary<int, object?> ReadOnlyDict => new ReadOnlyDictionary<int, object?>(_values);

    public object? DefaultValue(SyntaxToken token) => _varDB.TryGetValue(token, out var V) ? DefaultValue(V!.id) : UnknownValue.Create();
    public object? DefaultValue(int id)
    {
        var result = UnknownValue.Create(_varDB[id].IntType);
        _logger.debug(() => $"{_varDB[id]} => {result}");
        return result;
    }

    public object? this[int id] => _values.TryGetValue(id, out var value) ? value : DefaultValue(id);
    public object? this[SyntaxToken token] => _varDB.TryGetValue(token, out var v) ? this[v!.id] : UnknownValue.Create();

    public IEnumerable<int> Keys => _values.Keys;
    public int Count => _values.Count;

    public bool ContainsKey(int key) => _values.ContainsKey(key);
    public bool ContainsKey(SyntaxToken token) => _varDB.TryGetValue(token, out var V) && _values.ContainsKey(V!.id);

    public bool TryGetValue(int key, out object? value) => _values.TryGetValue(key, out value);
    public bool TryGetValue(IdentifierNameSyntax id, out object? value) => TryGetValue(id.Identifier, out value); // id.Identifier is SyntaxToken
    public bool TryGetValue(SyntaxToken token, out object? value)
    {
        bool result;
        if (_varDB.TryGetValue(token, out var V))
        {
            result = _values.TryGetValue(V!.id, out value);
        }
        else
        {
            value = null;
            result = false;
            _logger.warn_once($"Variable definition not found for {token}");
        }
        _logger.debug($"{token} => {value}");
        return result;
    }

    public bool IsVariableRegistered(SyntaxToken token) => _varDB.TryGetValue(token, out var _);
    public void RegisterVariable(VariableDeclaratorSyntax decl)
    {
        _logger.debug($"Registering variable: {decl}");
        var parent = decl.Parent as VariableDeclarationSyntax;
        if (parent == null)
            throw new ArgumentException("VariableDeclaratorSyntax must have a parent VariableDeclarationSyntax", nameof(decl));

        _varDB.Add(decl, parent.Type.ToString());
    }

    // XXX all Sets should call this one
    void setVar(Variable V, object? value)
    {
        _logger.debug(() => $"setVar: {V} = ({value?.GetType()}) {value}");
        if (value is UnknownTypedValue ut && V.IntType != null && ut.type != V.IntType)
            value = ut.Cast(V.IntType);

        _values[V.id] = value switch
        {
            UnknownValue u => V.IntType != null ? UnknownTypedValue.Create(V.IntType).WithVarID(V.id) : u,
            UnknownTypedValue ut2 => ut2.WithVarID(V.id),
            IntConstExpr ice => ice.Materialize(V.IntType),
            _ => value
        };
    }

    public void ResetVar(SyntaxToken token)
    {
        if (_varDB.TryGetValue(token, out var V))
        {
            _logger.debug($"Resetting variable {V}");
            setVar(V!, DefaultValue(V!.id));
        }
    }

    public void Remove(int key) => _values.Remove(key); // XXX _flags?

    public void Set(int id, object? value, [CallerMemberName] string caller = "")
    {
        _logger.debug(() => $"{_varDB[id]} = {value} [caller: {caller}]");
        setVar(_varDB[id], value);
    }

    public void Set(SyntaxToken token, object? value, [CallerMemberName] string caller = "")
    {
        if (_varDB.TryGetValue(token, out var V))
        {
            _logger.debug(() => $"{V} = ({value?.GetType()}) {value} [caller: {caller}]");
            setVar(V!, value);
        }
        else
        {
            _logger.warn_once($"Variable not found in VarDB for {token}");
        }
    }

    public void Set(IdentifierNameSyntax id, object? value) => Set(id.Identifier, value);

    public VarDict ShallowClone()
    {
        var clonedDict = new VarDict(_varDB);
        clonedDict._flags = _flags; // XXX byRef!

        foreach (var entry in this._values)
            clonedDict._values[entry.Key] = entry.Value;

        return clonedDict;
    }

    public VarDict Clone()
    {
        var clonedDict = new VarDict(_varDB);
        clonedDict._flags = _flags; // XXX byRef!

        // Deep copy the dictionary
        foreach (var entry in this._values)
        {
            if (entry.Value is ICloneable cloneableValue)
            {
                clonedDict._values[entry.Key] = cloneableValue.Clone();
            }
            else
            {
                clonedDict._values[entry.Key] = entry.Value;
            }
        }

        return clonedDict;
    }

    public void UpdateExisting(VarDict other)
    {
        foreach (var other_kvp in other.ReadOnlyDict)
        {
            if (!this.TryGetValue(other_kvp.Key, out var thisValue))
                continue;

            if (object.Equals(thisValue, other_kvp.Value))
                continue; // Values are equal, nothing to do

            UpdateVar(other_kvp.Key, other_kvp.Value);
        }
    }

    void UpdateVar(int key, object? newValue)
    {
        // if (_logger.HasTag("UpdateVar"))
        //     _logger.info($"{key,-10} {this[key],-20} => {newValue}");

        Set(key, newValue);
    }

    public void MergeExisting(VarDict other)
    {
        foreach (var other_kvp in other.ReadOnlyDict)
        {
            if (!this.TryGetValue(other_kvp.Key, out var thisValue))
                continue;

            if (Equals(thisValue, other_kvp.Value))
                continue; // Values are equal, nothing to do

            if (thisValue != null && other_kvp.Value != null)
                Set(other_kvp.Key, VarProcessor.MergeVar("_", thisValue, other_kvp.Value)); // TODO: return back var name (display only)
            else if (thisValue == null && other_kvp.Value != null)
                Set(other_kvp.Key, other_kvp.Value);
        }
    }

    // keep only vars present in both
    public void MergeCommon(VarDict other)
    {
        var keysToRemove = this.Keys.Where(k => !other.ContainsKey(k)).ToList();
        foreach (var key in keysToRemove)
        {
            this.Remove(key);
            _flags.Remove(key);
        }

        MergeExisting(other);
    }

    public VarDict VarsFromNode(SyntaxNode node)
    {
        VarDict vars = new VarDict(_varDB);
        foreach (var id in node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            if (ContainsKey(id.Identifier))
                vars.Set(id.Identifier, this[id.Identifier]);
        return vars;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not VarDict other || Count != other.Count)
            return false;

        foreach (var kvp in _values)
        {
            if (_varDB[kvp.Key].IsLoopVar)
                continue; // Skip loop variables

            if (!other.TryGetValue(kvp.Key, out var value) || !Equals(kvp.Value, value))
                return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var kvp in _values.OrderBy(kvp => kvp.Key)) // Ensure order doesn't affect hash
        {
            if (_varDB[kvp.Key].IsLoopVar)
                continue; // Skip loop variables

            hash = hash * 31 + kvp.Key.GetHashCode();
            hash = hash * 31 + (kvp.Value?.GetHashCode() ?? 0);
        }
        return hash;
    }

    public override string ToString() => ToString(false);
    public string ToString(bool showType) => "<VarDict " + string.Join(", ", _values.Select(kvp => $"{_varDB[kvp.Key].Name}={(showType ? $"({kvp.Value?.GetType()}) " : "")}{kvp.Value}")) + ">";
}
