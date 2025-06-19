using System;

public static class ConsoleExtensions
{
    public static void WriteColor(this TextWriter writer, string text, ConsoleColor color)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        writer.Write(text);
        Console.ForegroundColor = previousColor;
    }

    public static void WriteLineColor(this TextWriter writer, string text, ConsoleColor color)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        writer.WriteLine(text);
        Console.ForegroundColor = previousColor;
    }
}
