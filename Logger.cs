using System;
using System.Runtime.CompilerServices;

public static class Logger
{
    public static HashSet<string> EnabledTags { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    static HashSet<string> _onceMessages = new();

    public static void error(string message, [CallerMemberName] string caller = "")
    {
        log($"[!] [{caller}] {message}".Red());
    }

    public static void warn(string message, [CallerMemberName] string caller = "")
    {
        if (message != null && message.StartsWith("[?] "))
            message = message.Substring(4);

        log($"[?] [{caller}] {message}".Yellow());
    }

    public static void warn_once(string message, [CallerMemberName] string caller = "")
    {
        if (_onceMessages.Contains(message))
            return;

        _onceMessages.Add(message);
        warn(message, caller);
    }

    public static void info(string message, [CallerMemberName] string caller = "")
    {
        if (!HasTag(caller))
            return;

        log($"[.] [{caller}] {message}");
    }

    public static void debug(string message, [CallerMemberName] string caller = "")
    {
        if (!HasTag(caller))
            return;

        if (message != null && message.StartsWith("[d] "))
            message = message.Substring(4); // remove prefix if already present

        log($"[d] [{caller}] {message}");
    }

    public static void debug(Func<string> msgFunc, [CallerMemberName] string caller = "")
    {
        if (!HasTag(caller))
            return;

        // TODO: check level
        debug(msgFunc(), caller);
    }

    public static void log(string message)
    {
        Console.Error.WriteLine(message);
    }

    public static void once(string message)
    {
        if (_onceMessages.Contains(message))
            return;

        _onceMessages.Add(message);
        log(message);
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

public class TaggedLogger
{
    private readonly string _tag;

    public TaggedLogger(string tag)
    {
        _tag = tag;
    }

    public void debug(string message)
    {
        Logger.debug(message, _tag);
    }

    // lazy
    public void debug(Func<string> msgFunc)
    {
        if (!Logger.HasTag(_tag))
            return;

        // TODO: check level

        Logger.debug(msgFunc(), _tag);
    }
}
