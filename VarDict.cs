using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class VarDict
{
    public const int FLAG_LOOP = 1;

    public static int Verbosity = 0;

    DefaultDict<string, int> _flags = new();
    Dictionary<string, object?> _values = new();

    public IReadOnlyDictionary<string, object?> ReadOnlyDict => new ReadOnlyDictionary<string, object?>(_values);

    public object? this[string varName]
    {
        get => _values.TryGetValue(varName, out var value) ? value : UnknownValue.Create().WithTag(varName);
    }

    public IEnumerable<string> Keys => _values.Keys;
    public int Count => _values.Count;
    public bool ContainsKey(string key) => _values.ContainsKey(key);
    public bool TryGetValue(string key, out object? value) => _values.TryGetValue(key, out value);
    public void Remove(string key) => _values.Remove(key); // XXX _flags?

    public void Set(string varName, object? value)
    {
        if (value is UnknownValueBase unk)
            value = unk.WithTag(varName);
        _values[varName] = value;
    }

    public void SetLoopVar(string varName) => _flags[varName] |= FLAG_LOOP;

    public VarDict ShallowClone()
    {
        var clonedDict = new VarDict();
        clonedDict._flags = _flags; // XXX byRef!

        foreach (var entry in this._values)
            clonedDict.Set(entry.Key, entry.Value);

        return clonedDict;
    }

    public VarDict Clone()
    {
        var clonedDict = new VarDict();
        clonedDict._flags = _flags; // XXX byRef!

        // Deep copy the dictionary
        foreach (var entry in this._values)
        {
            if (entry.Value is ICloneable cloneableValue)
            {
                clonedDict.Set(entry.Key, cloneableValue.Clone());
            }
            else
            {
                clonedDict.Set(entry.Key, entry.Value);
            }
        }

        return clonedDict;
    }

    public VarDict CloneWithoutLoopVars()
    {
        VarDict clone = (VarDict)Clone();
        foreach (var kvp in _flags)
        {
            if ((kvp.Value & FLAG_LOOP) != 0)
                clone.Remove(kvp.Key);
        }
        return clone;
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

    void UpdateVar(string key, object newValue)
    {
        if (Logger.HasTag("UpdateVar"))
            Logger.info($"{key,-10} {this[key],-20} => {newValue}");

        Set(key, newValue);
    }

    public void MergeExisting(VarDict other)
    {
        foreach (var other_kvp in other.ReadOnlyDict)
        {
            if (!this.TryGetValue(other_kvp.Key, out var thisValue))
                continue;

            if (object.Equals(thisValue, other_kvp.Value))
                continue; // Values are equal, nothing to do

            Set(other_kvp.Key, VarProcessor.MergeVar(other_kvp.Key, thisValue, other_kvp.Value));
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
        VarDict vars = new VarDict();
        foreach (var id in node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            if (ContainsKey(id.Identifier.ValueText))
                vars.Set(id.Identifier.ValueText, this[id.Identifier.ValueText]);
        return vars;
    }

    public override bool Equals(object obj)
    {
        if (obj is not VarDict other || Count != other.Count)
            return false;

        foreach (var kvp in _values)
        {
            if ((_flags[kvp.Key] & FLAG_LOOP) != 0)
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
            if ((_flags[kvp.Key] & FLAG_LOOP) != 0)
                continue; // Skip loop variables

            hash = hash * 31 + kvp.Key.GetHashCode();
            hash = hash * 31 + (kvp.Value?.GetHashCode() ?? 0);
        }
        return hash;
    }

    public override string ToString()
    {
        if (Verbosity > 2)
            return "<VarDict " + string.Join(", ", _values.Select(kvp => $"{kvp.Key}=({kvp.Value?.GetType()}){kvp.Value}")) + ">";
        else
            return "<VarDict " + string.Join(", ", _values.Select(kvp => $"{kvp.Key}={kvp.Value}")) + ">";
    }
}
