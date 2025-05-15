using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Options
{
    [Value(0, MetaName = "filename", Required = false, HelpText = "Input .cs file.")]
    public string Filename { get; set; }

    [Value(1, MetaName = "methods", HelpText = "Method names to process.")]
    public IEnumerable<string> Methods { get; set; }

    [Option("hint", HelpText = "Set lineno:bool control flow hint.")]
    public IEnumerable<string> Hint { get; set; }

    [Option('v', "verbosity", Default = 0, HelpText = "Set verbosity level.")]
    public int Verbosity { get; set; }

    [Option('a', "all", Default = false, HelpText = "Process all methods (default if processing STDIN)")]
    public bool ProcessAllMethods { get; set; }

    [Option('T', "tree", Default = false, HelpText = "Print syntax tree.")]
    public bool PrintTree { get; set; }

    [Option("remove-switch-vars", Default = true, HelpText = "Remove switch variables.")]
    public bool RemoveSwitchVars { get; set; }
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
                string code;

                if (string.IsNullOrEmpty(fname))
                {
                    if (!Console.IsInputRedirected)
                    {
                        Console.WriteLine("No input file nor pipe is specified. Run with --help for more information.");
                        return;
                    }
                    code = Console.In.ReadToEnd();
                    opts.ProcessAllMethods = true;
                }
                else
                {
                    code = File.ReadAllText(fname);
                }

                if (opts.PrintTree)
                {
                    SyntaxTreePrinter printer = new SyntaxTreePrinter(code);
                    printer.Print();
                    return;
                }

                ControlFlowUnflattener controlFlowUnflattener = new ControlFlowUnflattener(code, hints);
                controlFlowUnflattener.Verbosity = opts.Verbosity;
                controlFlowUnflattener.RemoveSwitchVars = opts.RemoveSwitchVars;

                if (opts.Methods == null || !opts.Methods.Any())
                {
                    Dictionary<int, string> methods = controlFlowUnflattener.Methods;
                    if (methods.Count == 0)
                    {
                        Console.WriteLine("[?] No methods found.");
                        return;
                    }

                    if (opts.ProcessAllMethods)
                    {
                        foreach (var method in methods)
                        {
                            string methodStr = controlFlowUnflattener.ReflowMethod(method.Key);
                            Console.WriteLine(methodStr);
                        }
                    }
                    else
                    {
                        Console.WriteLine("methods:\n - " + String.Join("\n - ", controlFlowUnflattener.Methods.Select(kv => $"{kv.Key}: {kv.Value}")));
                    }
                }
                else
                {
                    foreach (string methodName in opts.Methods)
                    {
                        // printer.PrintMethod(methodName);
                        string methodStr = controlFlowUnflattener.ReflowMethod(methodName);
                        Console.WriteLine(methodStr);
                    }
                }
            });
    }
}

