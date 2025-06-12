using Microsoft.CodeAnalysis.CSharp;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using HintsDictionary = System.Collections.Generic.Dictionary<int, EHint>;

class Program
{
    record Options(
        string? filename,
        List<string> methods,
        List<string> hintList,
        string? expr,
        int verbosity,
        bool processAll,
        bool listMethods,
        bool printTree,
        bool addComments,
        bool showAnnotations,
        bool removeSwitchVars,
        PostProcessMode preProcess,
        PostProcessMode postProcess,
        List<string> dropVars,
        bool dumpFlowInfos,
        bool showIntermediateLogs
    );

    enum PostProcessMode
    {
        Disabled,
        Enabled,
        Only
    }

    static ControlFlowUnflattener createUnflattener(string code, Options opts, HintsDictionary hints)
    {
        return new ControlFlowUnflattener(code, hints)
        {
            Verbosity = opts.verbosity,
            RemoveSwitchVars = opts.removeSwitchVars,
            AddComments = opts.addComments,
            ShowAnnotations = opts.showAnnotations,
            PreProcess = (opts.preProcess != PostProcessMode.Disabled),
            PostProcess = (opts.postProcess != PostProcessMode.Disabled),
            showIntermediateLogs = opts.showIntermediateLogs,
        };
    }

    static PostProcessMode ParsePostProcessMode(string? input)
    {
        switch (input?.ToLowerInvariant())
        {
            case "1":
            case "on":
            case "true":
                return PostProcessMode.Enabled;
            case "0":
            case "off":
            case "false":
                return PostProcessMode.Disabled;
            case "only":
                return PostProcessMode.Only;
            default:
                Console.Error.WriteLine($"Invalid value: {input}. Must be one of: 1, 0, on, off, true, false, only.");
                Environment.Exit(1);
                return PostProcessMode.Disabled; // never reached
        }
    }

    public static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture; // do not print unicode '−' for negative numbers

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

        var showAnnotationsOpt = new Option<bool>(
            aliases: new[] { "--annotations", "-A" },
            getDefaultValue: () => false,
            description: "Show annotations."
        );

        var removeSwitchVarsOpt = new Option<bool>(
            name: "--remove-switch-vars",
            getDefaultValue: () => true,
            description: "Remove switch variables."
        );

        var preProcessOpt = new Option<string>(
                aliases: new[] { "--pre-process", "-p" },
                getDefaultValue: () => "true",
                description: "Pre-process the code. Accepts: 1/on/true, 0/off/false, only (pre-process only)."
                );

        var postProcessOpt = new Option<string>(
                aliases: new[] { "--post-process", "-P" },
                getDefaultValue: () => "true",
                description: "Post-process the code. Accepts: 1/on/true, 0/off/false, only (post-process only)."
                );

        var dropVarsOpt = new Option<List<string>>(
            name: "--drop-var",
            getDefaultValue: () => new List<string>(),
            description: "Drop specified variable(s)."
        );

        var dumpFlowInfosOpt = new Option<bool>(
            aliases: new[] { "--dump-flow-infos", "-F" },
            getDefaultValue: () => false,
            description: "Dump FlowInfos."
        );

        var exprArg = new Option<string>(
            aliases: new[] { "--expr", "-e" },
            getDefaultValue: () => string.Empty,
            description: "Evaluate an expression."
        );

        var showIntermediateLogsOpt = new Option<bool>(
            aliases: new[] { "--show-intermediate-logs", "-I" },
            getDefaultValue: () => false,
            description: "Show intermediate logs during processing."
        );

        // --- Define the root command ---
        var rootCommand = new RootCommand("Control flow reflow tool for .cs files")
        {
            filenameArg,
            methodsArg,
            exprArg,
            hintOpt,
            verbosityOpt,
            processAllOpt,
            printTreeOpt,
            addCommentsOpt,
            showAnnotationsOpt,
            removeSwitchVarsOpt,
            preProcessOpt,
            postProcessOpt,
            quietOpt,
            listMethodsOpt,
            dropVarsOpt,
            dumpFlowInfosOpt,
            showIntermediateLogsOpt
        };

        // --- Set the handler ---
        rootCommand.SetHandler((context) =>
        {
            var opts = new Options(
                filename: context.ParseResult.GetValueForArgument(filenameArg),
                methods: context.ParseResult.GetValueForArgument(methodsArg),
                expr: context.ParseResult.GetValueForOption(exprArg),
                hintList: context.ParseResult.GetValueForOption(hintOpt),
                verbosity: context.ParseResult.GetValueForOption(quietOpt) ? -1 : context.ParseResult.Tokens.Count(t => t.Value == "-v"),
                processAll: context.ParseResult.GetValueForOption(processAllOpt),
                printTree: context.ParseResult.GetValueForOption(printTreeOpt),
                addComments: context.ParseResult.GetValueForOption(addCommentsOpt),
                showAnnotations: context.ParseResult.GetValueForOption(showAnnotationsOpt),
                removeSwitchVars: context.ParseResult.GetValueForOption(removeSwitchVarsOpt),
                preProcess: ParsePostProcessMode(context.ParseResult.GetValueForOption(preProcessOpt)),
                postProcess: ParsePostProcessMode(context.ParseResult.GetValueForOption(postProcessOpt)),
                listMethods: context.ParseResult.GetValueForOption(listMethodsOpt),
                dropVars: context.ParseResult.GetValueForOption(dropVarsOpt),
                dumpFlowInfos: context.ParseResult.GetValueForOption(dumpFlowInfosOpt),
                showIntermediateLogs: context.ParseResult.GetValueForOption(showIntermediateLogsOpt)
            );

            var hints = new HintsDictionary();

            foreach (var entry in opts.hintList ?? Enumerable.Empty<string>())
            {
                var parts = entry.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int lineno))
                {
                    hints[lineno] = Enum.Parse<EHint>(parts[1]);
                }
                else
                {
                    Console.WriteLine($"Invalid --hint entry: {entry}");
                }
            }

            string code;

            if (!string.IsNullOrEmpty(opts.expr))
            {
                code = opts.expr;
            }
            else if (string.IsNullOrEmpty(opts.filename))
            {
                if (!Console.IsInputRedirected)
                {
                    Console.WriteLine("No input file nor pipe nor expr is specified. Run with --help for more information.");
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

            VarDict.Verbosity = opts.verbosity;

            var unflattener = createUnflattener(code, opts, hints);
            var methodDict = unflattener.Methods;

            bool printAll = false;
            List<CSharpSyntaxNode> methods = new List<CSharpSyntaxNode>();
            if (opts.methods == null || opts.methods.Count == 0)
            {
                printAll = true;
                methods = unflattener.Methods.Values.Select(m => unflattener.GetMethod(m)).ToList();
            }
            else
            {
                foreach (var methodName in opts.methods)
                {
                    methods.Add(unflattener.GetMethod(methodName));
                }
            }

            // define lambda
            Action? lambda;
            if (opts.listMethods)
            {
                if (methodDict.Count != 0)
                    Console.WriteLine("methods:\n - " + string.Join("\n - ", methodDict.Select(kv => $"{kv.Key}: {kv.Value}")));
                else
                    Console.WriteLine("[?] No methods found.");
            }
            else if (opts.printTree)
            {
                var printer = new SyntaxTreePrinter(code);
                printer.Verbosity = opts.verbosity;

                if (printAll)
                    printer.Print();
                else
                    foreach (var methodName in opts.methods)
                        printer.PrintMethod(methodName);
            }
            else if (opts.postProcess == PostProcessMode.Only)
            {
                foreach (var method in methods)
                {
                    PostProcessor postProcessor = new(new());
                    postProcessor.Verbosity = opts.verbosity;
                    var processed = postProcessor.ProcessFunction(method);
                    Console.Write(processed.ToFullString());
                }
            }
            else
            {
                foreach (var method in methods)
                {
                    unflattener.Reset();
                    unflattener.SetHints(hints);
                    unflattener.DropVars(opts.dropVars);
                    var collector = new ControlFlowTreeCollector();
                    if (opts.dumpFlowInfos)
                    {
                        collector.Process(method);
                        Console.WriteLine("Original Control Flow Tree:");
                        collector.PrintTree();
                    }
                    Console.WriteLine(unflattener.ReflowMethod(method));
                    if (opts.dumpFlowInfos)
                    {
                        Console.WriteLine();
                        unflattener.DumpFlowInfos();

                        //                        Console.WriteLine();
                        //                        var collector = new ControlFlowTreeCollector();
                        //                        collector.Process(method);
                        //                        Console.WriteLine("Processed Control Flow Tree:");
                        //                        unflattener._flowRoot.Print();
                    }
                    Console.WriteLine();
                }
            }

        }); // rootCommand.SetHandler

        return rootCommand.Invoke(args);
    }
}
