using System.CommandLine;
using System.CommandLine.Invocation;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    public static int Main(string[] args)
    {
        // --- Define arguments and options ---
        var filenameArg = new Argument<string?>("filename", description: "Input .cs file (optional).") { Arity = ArgumentArity.ZeroOrOne };

        var methodsArg = new Argument<List<string>>("methods", description: "Method names to process.") { Arity = ArgumentArity.ZeroOrMore };

        var hintOpt = new Option<List<string>>(
            name: "--hint",
            description: "Set lineno:bool control flow hint.",
            parseArgument: result => result.Tokens.Select(t => t.Value).ToList()
        );

        var verbosityOpt = new Option<int>(
            aliases: new[] { "--verbosity", "-v" },
            getDefaultValue: () => 0,
            description: "Set verbosity level."
        );

        var processAllOpt = new Option<bool>(
            aliases: new[] { "--all", "-a" },
            getDefaultValue: () => false,
            description: "Process all methods (default if processing STDIN)."
        );

        var printTreeOpt = new Option<bool>(
            aliases: new[] { "--tree", "-T" },
            getDefaultValue: () => false,
            description: "Print syntax tree."
        );

        var addCommentsOpt = new Option<bool>(
            aliases: new[] { "--comments", "-c" },
            getDefaultValue: () => true,
            description: "Add comments."
        );

        var removeSwitchVarsOpt = new Option<bool>(
            name: "--remove-switch-vars",
            getDefaultValue: () => true,
            description: "Remove switch variables."
        );

        // --- Define the root command ---
        var rootCommand = new RootCommand("Control flow reflow tool for .cs files")
        {
            filenameArg,
            methodsArg,
            hintOpt,
            verbosityOpt,
            processAllOpt,
            printTreeOpt,
            addCommentsOpt,
            removeSwitchVarsOpt
        };

        // --- Set the handler ---
        rootCommand.SetHandler((
            string? filename,
            List<string> methods,
            List<string> hintList,
            int verbosity,
            bool processAll,
            bool printTree,
            bool addComments,
            bool removeSwitchVars) =>
        {
            var hints = new Dictionary<int, bool>();

            foreach (var entry in hintList ?? Enumerable.Empty<string>())
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

            if (string.IsNullOrEmpty(filename))
            {
                if (!Console.IsInputRedirected)
                {
                    Console.WriteLine("No input file nor pipe is specified. Run with --help for more information.");
                    return;
                }
                code = Console.In.ReadToEnd();
                processAll = true;
            }
            else
            {
                if (!File.Exists(filename))
                {
                    Console.Error.WriteLine($"[error] File not found: {filename}");
                    return;
                }
                code = File.ReadAllText(filename);
            }

            if (printTree)
            {
                var printer = new SyntaxTreePrinter(code);
                printer.Print();
                return;
            }

            var controlFlowUnflattener = new ControlFlowUnflattener(code, hints)
            {
                Verbosity = verbosity,
                RemoveSwitchVars = removeSwitchVars,
                AddComments = addComments
            };

            if (methods == null || methods.Count == 0)
            {
                var methodDict = controlFlowUnflattener.Methods;
                if (methodDict.Count == 0)
                {
                    Console.WriteLine("[?] No methods found.");
                    return;
                }

                if (processAll)
                {
                    foreach (var method in methodDict)
                    {
                        Console.WriteLine(controlFlowUnflattener.ReflowMethod(method.Key));
                    }
                }
                else
                {
                    Console.WriteLine("methods:\n - " + string.Join("\n - ", methodDict.Select(kv => $"{kv.Key}: {kv.Value}")));
                }
            }
            else
            {
                foreach (var methodName in methods)
                {
                    Console.WriteLine(controlFlowUnflattener.ReflowMethod(methodName));
                }
            }
        },
        filenameArg,
        methodsArg,
        hintOpt,
        verbosityOpt,
        processAllOpt,
        printTreeOpt,
        addCommentsOpt,
        removeSwitchVarsOpt
        );

        return rootCommand.Invoke(args);
    }
}
