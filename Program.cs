using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Options
{
    [Value(0, MetaName = "filename", Required = true, HelpText = "Input .cs file.")]
    public string Filename { get; set; }

    [Value(1, MetaName = "methods", HelpText = "Method names to process.")]
    public IEnumerable<string> Methods { get; set; }

    [Option("hint", HelpText = "Set lineno:bool control flow hint.")]
    public IEnumerable<string> Hint { get; set; }
}

class Program
{
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(opts =>
            {
                var hints = new Dictionary<int, bool>();

                // Parse --hint key=value entries
                foreach (var entry in opts.Hint ?? Enumerable.Empty<string>())
                {
                    var parts = entry.Split(':');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int lineno) &&
                        bool.TryParse(parts[1], out bool hintValue))
                    {
                        hints[lineno] = hintValue;
                    }
                    else
                    {
                        Console.WriteLine($"Invalid --hint entry: {entry}");
                    }
                }

                var fname = opts.Filename;
                var code = File.ReadAllText(fname);

                SyntaxTreePrinter printer = new SyntaxTreePrinter(code);
                ControlFlowUnflattener controlFlowUnflattener = new ControlFlowUnflattener(code, hints);

                if (opts.Methods == null || !opts.Methods.Any())
                {
                    Dictionary<int, string> methods = controlFlowUnflattener.Methods;
                    if (methods.Count == 0)
                    {
                        Console.WriteLine("[?] No methods found.");
                        return;
                    }
                    Console.WriteLine("methods:\n - " + String.Join("\n - ", controlFlowUnflattener.Methods.Select(kv => $"{kv.Key}: {kv.Value}")));
                }
                else
                {
                    foreach (string methodName in opts.Methods)
                    {
                        //                        printer.PrintMethod(methodName);
                        Console.WriteLine();
                        controlFlowUnflattener.ReflowMethod(methodName);
                    }
                }
            });
    }
}

