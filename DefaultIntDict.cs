using System.Collections.Generic;

public class DefaultIntDict : Dictionary<int, int>
{
    public new int this[int key]
    {
        get
        {
            if (!TryGetValue(key, out var value))
            {
                value = 0;
                base[key] = value;
            }
            return value;
        }
        set => base[key] = value;
    }
}
