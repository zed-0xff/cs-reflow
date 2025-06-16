public static class ANSI
{
    public const string COLOR_BLUE = "\x1b[34m";
    public const string COLOR_CYAN = "\x1b[36m";
    public const string COLOR_LIGHT_BLUE = "\x1b[94m";
    public const string COLOR_LIGHT_CYAN = "\x1b[96m";
    public const string COLOR_LIGHT_MAGENTA = "\x1b[95m";
    public const string COLOR_LIGHT_RED = "\x1b[91m";
    public const string COLOR_LIGHT_YELLOW = "\x1b[93m";
    public const string COLOR_MAGENTA = "\x1b[35m";
    public const string COLOR_RED = "\x1b[31m";
    public const string COLOR_RESET = "\x1b[0m";
    public const string COLOR_YELLOW = "\x1b[33m";
    public const string COLOR_GRAY = "\x1b[90m";

    public const string ERASE_TILL_EOL = "\x1b[0K";

    public static string Yellow(this string text)
    {
        return COLOR_YELLOW + text + COLOR_RESET;
    }

    public static string Red(this string text)
    {
        return COLOR_RED + text + COLOR_RESET;
    }

    public static string Gray(this string text)
    {
        return COLOR_GRAY + text + COLOR_RESET;
    }
}
