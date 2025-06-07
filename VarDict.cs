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

            this[other_kvp.Key] = other_kvp.Value; // Update with the new value
        }
    }

    public void MergeExisting(VarDict other)
    {
        foreach (var other_kvp in other)
        {
            if (!this.TryGetValue(other_kvp.Key, out var thisValue))
                continue;

            if (object.Equals(thisValue, other_kvp.Value))
                continue; // Values are equal, nothing to do

            this[other_kvp.Key] = MergeVars(thisValue, other_kvp.Value);
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

    // input: value1 != value2 and both of them are not null
    object MergeVars(object value1, object value2)
    {
        if (value2 is UnknownValueBase && value1 is not UnknownValueBase)
        {
            return MergeVars(value2, value1); // Ensure UnknownValueBase is always first
        }

        return value1 switch
        {
            byte b1 when value2 is byte b2 => new UnknownValueList(TypeDB.Byte, new() { b1, b2 }),
            sbyte sb1 when value2 is sbyte sb2 => new UnknownValueList(TypeDB.SByte, new() { sb1, sb2 }),
            int i1 when value2 is int i2 => new UnknownValueList(TypeDB.Int, new() { i1, i2 }),
            uint ui1 when value2 is uint ui2 => new UnknownValueList(TypeDB.UInt, new() { ui1, ui2 }),
            short s1 when value2 is short s2 => new UnknownValueList(TypeDB.Short, new() { s1, s2 }),
            ushort us1 when value2 is ushort us2 => new UnknownValueList(TypeDB.UShort, new() { us1, us2 }),
            long l1 when value2 is long l2 => new UnknownValueList(TypeDB.Long, new() { l1, l2 }),
            ulong ul1 when value2 is ulong ul2 => throw new NotImplementedException("Merging ulong values is not implemented."),
            UnknownValueBase unk1 => unk1.Merge(value2),
            _ => new UnknownValue()
        };
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
