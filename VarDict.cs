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
}
