using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

public class VarDict : Dictionary<string, object>
{
    public const int FLAG_SWITCH = 1;
    public const int FLAG_LOOP = 2;

    public static int Verbosity = 0;

    DefaultDict<string, int> flags = new();

    public bool IsSwitchVar(string varName)
    {
        return (flags[varName] & FLAG_SWITCH) != 0;
    }

    public void SetSwitchVar(string varName, bool isSwitch = true)
    {
        flags[varName] |= FLAG_SWITCH;
    }

    public void SetLoopVar(string varName, bool isLoop = true)
    {
        flags[varName] |= FLAG_LOOP;
    }

    public List<string> SwitchVars()
    {
        return new List<string>(flags.Where(kvp => (kvp.Value & FLAG_SWITCH) != 0).Select(kvp => kvp.Key));
    }

    public VarDict Clone()
    {
        // Create a new instance of VarDict
        var clonedDict = new VarDict();
        clonedDict.flags = flags; // XXX byRef!

        // Deep copy the dictionary
        foreach (var entry in this)
        {
            if (entry.Value is ICloneable cloneableValue)
            {
                clonedDict[entry.Key] = cloneableValue.Clone();
            }
            else
            {
                clonedDict[entry.Key] = entry.Value;
            }
        }

        return clonedDict;
    }

    public VarDict CloneWithoutLoopVars()
    {
        VarDict clone = (VarDict)Clone();
        foreach (var kvp in flags)
        {
            if ((kvp.Value & FLAG_LOOP) != 0)
                clone.Remove(kvp.Key);
        }
        return clone;
    }

    public void UpdateExisting(VarDict other)
    {
        foreach (var other_kvp in other)
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

        this[key] = newValue;
    }

    public void MergeExisting(VarDict other)
    {
        foreach (var other_kvp in other)
        {
            if (!this.TryGetValue(other_kvp.Key, out var thisValue))
                continue;

            if (object.Equals(thisValue, other_kvp.Value))
                continue; // Values are equal, nothing to do

            this[other_kvp.Key] = VarProcessor.MergeVar(other_kvp.Key, thisValue, other_kvp.Value);
        }
    }

    // keep only vars present in both
    public void MergeCommon(VarDict other)
    {
        var keysToRemove = this.Keys.Where(k => !other.ContainsKey(k)).ToList();
        foreach (var key in keysToRemove)
        {
            this.Remove(key);
            flags.Remove(key);
        }

        MergeExisting(other);
    }

    public VarDict VarsFromNode(SyntaxNode node)
    {
        VarDict vars = new VarDict();
        foreach (var id in node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            if (ContainsKey(id.Identifier.ValueText))
                vars[id.Identifier.ValueText] = this[id.Identifier.ValueText];
        return vars;
    }

    public override bool Equals(object obj)
    {
        if (obj is not VarDict other || Count != other.Count)
            return false;

        foreach (var kvp in this)
        {
            if ((flags[kvp.Key] & FLAG_LOOP) != 0)
                continue; // Skip loop variables

            if (!other.TryGetValue(kvp.Key, out var value) || !Equals(kvp.Value, value))
                return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var kvp in this.OrderBy(kvp => kvp.Key)) // Ensure order doesn't affect hash
        {
            if ((flags[kvp.Key] & FLAG_LOOP) != 0)
                continue; // Skip loop variables

            hash = hash * 31 + kvp.Key.GetHashCode();
            hash = hash * 31 + (kvp.Value?.GetHashCode() ?? 0);
        }
        return hash;
    }

    public override string ToString()
    {
        if (Verbosity > 2)
            return "<VarDict " + string.Join(", ", this.Select(kvp => $"{kvp.Key}=({kvp.Value?.GetType()}){kvp.Value}")) + ">";
        else
            return "<VarDict " + string.Join(", ", this.Select(kvp => $"{kvp.Key}={kvp.Value}")) + ">";
    }
}
