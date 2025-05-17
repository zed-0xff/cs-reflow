using System.Collections.Generic;

public class VarDict : Dictionary<string, object>, ICloneable
{
    public class VarFlags
    {
        public bool isSwitch = false;
        public bool isLoop = false;
    }

    Dictionary<string, VarFlags> flags = new();

    public bool IsSwitchVar(string varName)
    {
        return flags.TryGetValue(varName, out VarFlags varFlags) && varFlags.isSwitch;
    }

    public void SetSwitchVar(string varName, bool isSwitch = true)
    {
        flags.TryAdd(varName, new());
        flags[varName].isSwitch = isSwitch;
    }

    public void SetLoopVar(string varName, bool isLoop = true)
    {
        flags.TryAdd(varName, new());
        flags[varName].isLoop = isLoop;
    }

    public List<string> SwitchVars()
    {
        return new List<string>(flags.Where(kvp => kvp.Value.isSwitch).Select(kvp => kvp.Key));
    }

    public object Clone()
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
                // If the object is not ICloneable, you might need to handle how to copy it
                clonedDict[entry.Key] = entry.Value; // Just copy reference as is
            }
        }

        return clonedDict;
    }

    public VarDict CloneWithoutLoopVars()
    {
        VarDict clone = (VarDict)Clone();
        foreach (var kvp in flags)
        {
            if (kvp.Value.isLoop)
            {
                clone.Remove(kvp.Key);
            }
        }
        return clone;
    }

    public override bool Equals(object obj)
    {
        if (obj is not VarDict other || Count != other.Count)
            return false;

        foreach (var kvp in this)
        {
            if (flags.TryGetValue(kvp.Key, out var varFlags) && varFlags.isLoop)
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
            if (flags.TryGetValue(kvp.Key, out var varFlags) && varFlags.isLoop)
                continue; // Skip loop variables

            hash = hash * 31 + kvp.Key.GetHashCode();
            hash = hash * 31 + (kvp.Value?.GetHashCode() ?? 0);
        }
        return hash;
    }

    public override string ToString()
    {
        return "<VarDict " + string.Join(", ", this.Select(kvp => $"{kvp.Key}={kvp.Value}")) + ">";
    }
}
