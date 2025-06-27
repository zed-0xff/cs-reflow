using System;
using System.Runtime.CompilerServices;

public static class Logger
{
    public static bool EnableAll = false;
    public static HashSet<string> EnabledTags { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public static HashSet<string> EnabledCategories { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    static HashSet<string> _onceMessages = new();

    const int TAG_PAD = 37;

    static Logger()
    {
        var v = Environment.GetEnvironmentVariable("REFLOW_LOG_TAGS");
        if (v == null)
            return;

        var tags = v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        EnableTags(tags);
    }

    public static string fmt_tag(string tag)
    {
        return $"[{tag}]".PadRight(TAG_PAD);
    }

    public static void error(string message, [CallerMemberName] string caller = "")
    {
        log($"[!] {fmt_tag(caller)} {message}".Red());
    }

    public static void warn(string message, [CallerMemberName] string caller = "")
    {
        if (message != null && message.StartsWith("[?] "))
            message = message.Substring(4);

        log($"[?] {fmt_tag(caller)} {message}".Yellow());
    }

    // caller is not counted in message uniqueness
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

        log($"[.] {fmt_tag(caller)} {message}");
    }

    public static void debug(string message, [CallerMemberName] string caller = "")
    {
        if (!HasTag(caller))
            return;

        if (message != null && message.StartsWith("[d] "))
            message = message.Substring(4); // remove prefix if already present

        log($"[d] {fmt_tag(caller)} {message}");
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
            if (string.IsNullOrEmpty(trimmed))
                continue;

            if (trimmed == "*" || trimmed == "all")
                EnableAll = true;
            else if (trimmed.EndsWith(".*"))
            {
                EnabledCategories.Add(trimmed.Substring(0, trimmed.Length - 1));
                EnabledCategories.Add(trimmed.Substring(0, trimmed.Length - 2));
            }
            else
                EnabledTags.Add(trimmed);
        }
    }

    public static bool HasTag(string tag) => EnableAll || EnabledTags.Contains(tag) || (EnabledCategories.Any(c => tag.StartsWith(c, StringComparison.OrdinalIgnoreCase)));
}

public class TaggedLogger
{
    private readonly string _tag;

    public TaggedLogger(string tag)
    {
        _tag = tag;
    }

    public void debug(string message, [CallerMemberName] string caller = "") => Logger.debug(message, $"{_tag}.{caller}");
    public void debug(Func<string> msgFunc, [CallerMemberName] string caller = "") => Logger.debug(() => msgFunc(), $"{_tag}.{caller}");
    public void warn(string message, [CallerMemberName] string caller = "") => Logger.warn(message, $"{_tag}.{caller}");
    public void warn_once(string message, [CallerMemberName] string caller = "") => Logger.warn_once(message, $"{_tag}.{caller}");
    public void error(string message, [CallerMemberName] string caller = "") => Logger.error(message, $"{_tag}.{caller}");
}
