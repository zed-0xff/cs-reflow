using System.Collections.Generic;

public class DefaultDict<TKey, TValue> : Dictionary<TKey, TValue>
    where TValue : new()
{
    public new TValue this[TKey key]
    {
        get
        {
            if (!TryGetValue(key, out var value))
            {
                value = new TValue(); // create new instance
                base[key] = value;
            }
            return value;
        }
        set => base[key] = value;
    }
}
