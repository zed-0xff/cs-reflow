using System;
using System.Runtime.CompilerServices;

public static class Logger
{
    public static HashSet<string> EnabledTags { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static void info(string message, [CallerMemberName] string caller = "")
    {
        if (!HasTag(caller))
            return;

        Console.Error.WriteLine($"[.] [{caller}] {message}");
    }

    public static void debug(string message, [CallerMemberName] string caller = "")
    {
        if (!HasTag(caller))
            return;

        Console.Error.WriteLine($"[d] [{caller}] {message}");
    }

    public static void EnableTags(IEnumerable<string> tags)
    {
        foreach (var tag in tags)
        {
            var trimmed = tag.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                EnabledTags.Add(trimmed);
        }
    }

    public static bool HasTag(string tag) => EnabledTags.Contains(tag) || EnabledTags.Contains("all");
}
