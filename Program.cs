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

    [Option('c', "comments", Default = true, HelpText = "Add comments")]
    public bool AddComments { get; set; }

    [Option("remove-switch-vars", Default = true, HelpText = "Remove switch variables.")]
    public bool RemoveSwitchVars { get; set; }
}

class Program
{
    static int RunWithOptions(Options opts)
    {
        // --- Validate input ---
        if (opts.Methods != null)
        {
            foreach (var method in opts.Methods)
            {
                if (method == "true" || method == "false")
                {
                    Console.Error.WriteLine("[error] 'true' or 'false' was parsed as a method name. Did you misplace an option like `--comments false`?");
                    return 1;
                }
            }
        }

        var hints = new Dictionary<int, bool>();

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

        string code;
        if (string.IsNullOrEmpty(opts.Filename))
        {
            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("No input file nor pipe is specified. Run with --help for more information.");
                return 1;
            }
            code = Console.In.ReadToEnd();
            opts.ProcessAllMethods = true;
        }
        else
        {
            if (!File.Exists(opts.Filename))
            {
                Console.Error.WriteLine($"[error] File not found: {opts.Filename}");
                return 1;
            }
            code = File.ReadAllText(opts.Filename);
        }

        if (opts.PrintTree)
        {
            var printer = new SyntaxTreePrinter(code);
            printer.Print();
            return 0;
        }

        var controlFlowUnflattener = new ControlFlowUnflattener(code, hints)
        {
            Verbosity = opts.Verbosity,
            RemoveSwitchVars = opts.RemoveSwitchVars,
            AddComments = opts.AddComments
        };

        if (opts.Methods == null || !opts.Methods.Any())
        {
            var methods = controlFlowUnflattener.Methods;
            if (methods.Count == 0)
            {
                Console.WriteLine("[?] No methods found.");
                return 1;
            }

            if (opts.ProcessAllMethods)
            {
                foreach (var method in methods)
                {
                    Console.WriteLine(controlFlowUnflattener.ReflowMethod(method.Key));
                }
            }
            else
            {
                Console.WriteLine("methods:\n - " + string.Join("\n - ", methods.Select(kv => $"{kv.Key}: {kv.Value}")));
            }
        }
        else
        {
            foreach (var methodName in opts.Methods)
            {
                try
                {
                    Console.WriteLine(controlFlowUnflattener.ReflowMethod(methodName));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[error] Failed to process method '{methodName}': {ex.Message}");
                    return 1;
                }
            }
        }

        return 0;
    }

    static int HandleParseError(IEnumerable<Error> errs)
    {
        if (errs.Any(e => e is HelpRequestedError || e is VersionRequestedError))
            return 0;

        Console.Error.WriteLine("[error] Failed to parse command-line arguments.");
        foreach (var err in errs)
            Console.Error.WriteLine("  - " + err.ToString());

        return 1;
    }

    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                    (Options opts) => RunWithOptions(opts),
                    errs => HandleParseError(errs)
                    );
    }
}

