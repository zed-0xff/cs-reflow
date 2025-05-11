using System.Collections.Generic;

public class VarDict : Dictionary<string, object>, ICloneable
{
    public object Clone()
    {
        // Create a new instance of VarDict
        var clonedDict = new VarDict();

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

    public override bool Equals(object obj)
    {
        if (obj is not VarDict other || Count != other.Count)
            return false;

        foreach (var kvp in this)
        {
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
            hash = hash * 31 + kvp.Key.GetHashCode();
            hash = hash * 31 + (kvp.Value?.GetHashCode() ?? 0);
        }
        return hash;
    }
}
