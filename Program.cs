using System.CommandLine;
using System.CommandLine.Invocation;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    public record Options(
        string? filename,
        List<string> methods,
        List<string> hintList,
        int verbosity,
        bool processAll,
        bool listMethods,
        bool printTree,
        bool addComments,
        bool removeSwitchVars,
        bool postProcess,
        List<string> dropVars
    );

    static ControlFlowUnflattener createUnflattener(string code, Options opts, Dictionary<int, bool> hints)
    {
        return new ControlFlowUnflattener(code, hints)
        {
            Verbosity = opts.verbosity,
            RemoveSwitchVars = opts.removeSwitchVars,
            AddComments = opts.addComments,
            PostProcess = opts.postProcess
        };
    }

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
            aliases: new[] { "--verbose", "-v" },
            getDefaultValue: () => 0,
            description: "Increase verbosity level."
        )
        {
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true
        };

        var quietOpt = new Option<bool>(
            aliases: new[] { "--quiet", "-q" },
            getDefaultValue: () => false,
            description: "Suppress all debug/status output."
        );

        var processAllOpt = new Option<bool>(
            aliases: new[] { "--all", "-a" },
            getDefaultValue: () => true,
            description: "Process all methods (default)."
        );

        var listMethodsOpt = new Option<bool>(
            aliases: new[] { "--list", "-l" },
            getDefaultValue: () => false,
            description: "List methods."
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

        var postProcessOpt = new Option<bool>(
            aliases: new[] { "--post-process", "-P" },
            getDefaultValue: () => true,
            description: "Post-process the code."
        );

        var dropVarsOpt = new Option<List<string>>(
            name: "--drop-var",
            getDefaultValue: () => new List<string>(),
            description: "Drop specified variable(s)."
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
            removeSwitchVarsOpt,
            postProcessOpt,
            quietOpt,
            listMethodsOpt,
            dropVarsOpt
        };

        // --- Set the handler ---
        rootCommand.SetHandler((context) =>
        {
            var opts = new Options(
                filename: context.ParseResult.GetValueForArgument(filenameArg),
                methods: context.ParseResult.GetValueForArgument(methodsArg),
                hintList: context.ParseResult.GetValueForOption(hintOpt),
                verbosity: context.ParseResult.GetValueForOption(quietOpt) ? -1 : context.ParseResult.Tokens.Count(t => t.Value == "-v"),
                processAll: context.ParseResult.GetValueForOption(processAllOpt),
                printTree: context.ParseResult.GetValueForOption(printTreeOpt),
                addComments: context.ParseResult.GetValueForOption(addCommentsOpt),
                removeSwitchVars: context.ParseResult.GetValueForOption(removeSwitchVarsOpt),
                postProcess: context.ParseResult.GetValueForOption(postProcessOpt),
                listMethods: context.ParseResult.GetValueForOption(listMethodsOpt),
                dropVars: context.ParseResult.GetValueForOption(dropVarsOpt)
            );

            var hints = new Dictionary<int, bool>();

            foreach (var entry in opts.hintList ?? Enumerable.Empty<string>())
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

            if (string.IsNullOrEmpty(opts.filename))
            {
                if (!Console.IsInputRedirected)
                {
                    Console.WriteLine("No input file nor pipe is specified. Run with --help for more information.");
                    return;
                }
                code = Console.In.ReadToEnd();
            }
            else
            {
                if (!File.Exists(opts.filename))
                {
                    Console.Error.WriteLine($"[error] File not found: {opts.filename}");
                    return;
                }
                code = File.ReadAllText(opts.filename);
            }

            var printer = new SyntaxTreePrinter(code);
            printer.Verbosity = opts.verbosity;

            var unflattener = createUnflattener(code, opts, hints);

            if (opts.methods == null || opts.methods.Count == 0)
            {
                var methodDict = unflattener.Methods;
                if (methodDict.Count == 0)
                {
                    Console.WriteLine("[?] No methods found.");
                    return;
                }

                if (opts.listMethods)
                {
                    Console.WriteLine("methods:\n - " + string.Join("\n - ", methodDict.Select(kv => $"{kv.Key}: {kv.Value}")));
                }
                else
                {
                    if (opts.printTree)
                    {
                        printer.Print();
                    }
                    else
                    {
                        foreach (var method in methodDict)
                        {
                            unflattener.Reset();
                            unflattener.SetHints(hints);
                            unflattener.DropVars(opts.dropVars);
                            Console.WriteLine(unflattener.ReflowMethod(method.Key));
                            Console.WriteLine();
                        }
                    }
                }
            }
            else
            {
                foreach (var methodName in opts.methods)
                {
                    if (opts.printTree)
                    {
                        printer.PrintMethod(methodName);
                    }
                    else
                    {
                        unflattener.Reset();
                        unflattener.SetHints(hints);
                        unflattener.DropVars(opts.dropVars);
                        Console.WriteLine(unflattener.ReflowMethod(methodName));
                        Console.WriteLine();
                    }
                }
            }
        });

        return rootCommand.Invoke(args);
    }
}
