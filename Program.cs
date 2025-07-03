using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
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
        List<string> debugTags,
        List<string> dropVars,
        List<string> hintList,
        List<string> keepVars,
        List<string> methods,
        List<string> traceUniqVars,
        List<string> traceVars,
        bool addComments,
        bool colorize,
        bool dumpFlowInfos,
        bool listMethods,
        bool moveDeclarations,
        bool postProcess,
        bool preProcess,
        bool printTree,
        bool processAll,
        bool reflow,
        bool showAnnotations,
        bool showProgress,
        int bitness,
        int verbosity,
        string? dumpIntermediateLogs,
        string? expr,
        string? filename
    );

    static ControlFlowUnflattener createUnflattener(string code, Options opts, HintsDictionary hints, bool dummyClassWrap)
    {
        return new ControlFlowUnflattener(code, verbosity: opts.verbosity, flowHints: hints, dummyClassWrap: dummyClassWrap)
        {
            AddComments = opts.addComments,
            ShowAnnotations = opts.showAnnotations,
            MoveDeclarations = opts.moveDeclarations,
            PreProcess = opts.preProcess,
            Reflow = opts.reflow,
            PostProcess = opts.postProcess,
            dumpIntermediateLogs = opts.dumpIntermediateLogs,
        };
    }

    static int calc_verbosity(InvocationContext context, Option<bool> quietOpt)
    {
        bool quiet = context.ParseResult.GetValueForOption(quietOpt);
        if (quiet)
            return -1; // quiet mode, no verbosity

        int nv = context.ParseResult.Tokens.Count(t => t.Value == "-v");
        if (nv > 0)
            return nv; // verbosity level is the number of -v options

        // check if env vars VIMRUNTIME and VIM are set
        if (Environment.GetEnvironmentVariable("VIMRUNTIME") != null || Environment.GetEnvironmentVariable("VIM") != null)
            return -1; // default verbosity when running as filter in vim

        return 0; // default verbosity
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

        var bitnessOpt = new Option<int>(
            aliases: new[] { "--bitness", "-b" },
            getDefaultValue: () => 0,
            description: "Bitness of the code (32 or 64)."
        )
        {
            ArgumentHelpName = "bitness",
            Arity = ArgumentArity.ExactlyOne
        };

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

        var showProgressOpt = new Option<bool>(
            aliases: new[] { "--progress" },
            getDefaultValue: () => !Console.IsOutputRedirected,
            description: "Show progress."
        );

        var colorizeOpt = new Option<bool>(
            aliases: new[] { "--color", "-C" },
            getDefaultValue: () => !Console.IsOutputRedirected,
            description: "Colorize output."
        );

        var moveDeclarationsOpt = new Option<bool>(
            aliases: new[] { "--move-declarations", "-M" },
            getDefaultValue: () => true,
            description: "Move variable declarations to the lowest common ancestor."
        );

        var preProcessOpt = new Option<bool>(
                aliases: new[] { "--pre-process", "-p" },
                getDefaultValue: () => true,
                description: "Pre-process the code."
                );

        var reflowOpt = new Option<bool>(
                aliases: new[] { "--reflow", "-R" },
                getDefaultValue: () => true,
                description: "Reflow the code."
                );

        var postProcessOpt = new Option<bool>(
                aliases: new[] { "--post-process", "-P" },
                getDefaultValue: () => true,
                description: "Post-process the code."
                );

        var keepVarsOpt = new Option<List<string>>(
            aliases: new[] { "--keep-var", "-k" },
            getDefaultValue: () => new List<string>(),
            description: "Keep specified variable(s)."
        );

        var traceVarsOpt = new Option<List<string>>(
            aliases: new[] { "--trace-var", "-V" },
            getDefaultValue: () => new List<string>(),
            description: "Trace specified variable(s)."
        );

        var traceUniqVarsOpt = new Option<List<string>>(
            aliases: new[] { "--trace-var-uniq", "-U" },
            getDefaultValue: () => new List<string>(),
            description: "Trace specified variable(s) - unique values only."
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

        var dumpIntermediateLogsOpt = new Option<string>(
            aliases: new[] { "--dump-intermediate-logs", "-D" },
            getDefaultValue: () => string.Empty,
            description: "Dump intermediate logs into specified dir."
        );

        var debugTagsOpt = new Option<List<string>>(
                aliases: new[] { "--log-tags", "-L" },
                description: "Comma-separated list of logging tags (method names) to enable",
                parseArgument: result =>
                {
                    var combined = string.Join(",", result.Tokens.Select(t => t.Value));
                    return combined.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                })
        {
            Arity = ArgumentArity.OneOrMore,
        };

        // --- Define the root command ---
        var rootCommand = new RootCommand("Control flow reflow tool for .cs files")
        {
            addCommentsOpt,
            bitnessOpt,
            colorizeOpt,
            debugTagsOpt,
            dropVarsOpt,
            dumpFlowInfosOpt,
            dumpIntermediateLogsOpt,
            exprArg,
            filenameArg,
            hintOpt,
            keepVarsOpt,
            listMethodsOpt,
            methodsArg,
            moveDeclarationsOpt,
            postProcessOpt,
            preProcessOpt,
            printTreeOpt,
            processAllOpt,
            quietOpt,
            reflowOpt,
            showAnnotationsOpt,
            showProgressOpt,
            traceUniqVarsOpt,
            traceVarsOpt,
            verbosityOpt,
        };

        // --- Set the handler ---
        rootCommand.SetHandler((context) =>
        {
            var opts = new Options(
                addComments: context.ParseResult.GetValueForOption(addCommentsOpt),
                bitness: context.ParseResult.GetValueForOption(bitnessOpt),
                colorize: context.ParseResult.GetValueForOption(colorizeOpt),
                debugTags: context.ParseResult.GetValueForOption(debugTagsOpt) ?? new(),
                dropVars: context.ParseResult.GetValueForOption(dropVarsOpt) ?? new(),
                dumpFlowInfos: context.ParseResult.GetValueForOption(dumpFlowInfosOpt),
                dumpIntermediateLogs: context.ParseResult.GetValueForOption(dumpIntermediateLogsOpt),
                expr: context.ParseResult.GetValueForOption(exprArg),
                filename: context.ParseResult.GetValueForArgument(filenameArg),
                hintList: context.ParseResult.GetValueForOption(hintOpt) ?? new(),
                keepVars: context.ParseResult.GetValueForOption(keepVarsOpt) ?? new(),
                listMethods: context.ParseResult.GetValueForOption(listMethodsOpt),
                methods: context.ParseResult.GetValueForArgument(methodsArg),
                moveDeclarations: context.ParseResult.GetValueForOption(moveDeclarationsOpt),
                postProcess: context.ParseResult.GetValueForOption(postProcessOpt),
                preProcess: context.ParseResult.GetValueForOption(preProcessOpt),
                printTree: context.ParseResult.GetValueForOption(printTreeOpt),
                processAll: context.ParseResult.GetValueForOption(processAllOpt),
                reflow: context.ParseResult.GetValueForOption(reflowOpt),
                showAnnotations: context.ParseResult.GetValueForOption(showAnnotationsOpt),
                showProgress: context.ParseResult.GetValueForOption(showProgressOpt),
                traceUniqVars: context.ParseResult.GetValueForOption(traceUniqVarsOpt) ?? new(),
                traceVars: context.ParseResult.GetValueForOption(traceVarsOpt) ?? new(),
                verbosity: calc_verbosity(context, quietOpt)
            );

            Logger.EnableTags(opts.debugTags);
            TypeDB.Bitness = opts.bitness;
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
                ProcessCmdLineExpr(opts);
                return;
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

            var unflattener = createUnflattener(code, opts, hints, false);
            var methodDict = unflattener.Methods;

            if (methodDict.Count == 0)
            {
                unflattener = createUnflattener(code, opts, hints, true);
                methodDict = unflattener.Methods;
            }

            bool printAll = false;
            var methods = new List<SyntaxNode>();
            if (opts.methods == null || opts.methods.Count == 0)
            {
                printAll = true;
                methods = unflattener.Methods.Keys.Select(k => unflattener.GetMethod(k)).ToList();
            }
            else
            {
                foreach (var methodNameOrLineNo in opts.methods)
                    methods.Add(unflattener.GetMethod(methodNameOrLineNo));
            }

            // define lambda
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
                else if (opts.methods != null)
                    foreach (var methodName in opts.methods)
                        printer.PrintMethod(methodName);
            }
            else
            {
                bool first = true;
                foreach (var method in methods)
                {
                    if (first)
                        first = false;
                    else
                        Console.WriteLine();

                    unflattener.Reset();
                    unflattener.SetHints(hints);
                    //unflattener.DropVars(opts.dropVars);
                    unflattener.KeepVars(opts.keepVars);
                    unflattener.TraceVars(opts.traceVars);
                    unflattener.TraceUniqVars(opts.traceUniqVars);
                    unflattener.ShowProgress = opts.showProgress;

                    var collector = new ControlFlowTreeCollector();
                    if (opts.dumpFlowInfos)
                    {
                        collector.Process(method);
                        Console.WriteLine("Original Control Flow Tree:");
                        collector.PrintTree();
                    }

                    string result = "";
                    try
                    {
                        result = unflattener.ReflowMethod(method);
                    }
                    catch
                    {
                        Console.Error.WriteLine($"[!] Failed to reflow method: {method?.TitleWithLineNo()}".Red());
                        throw;
                    }

                    if (opts.colorize)
                    {
                        ConsoleColorizer.ColorizeToConsole(PostProcessor.ExpandTabs(result));
                    }
                    else
                    {
                        Console.WriteLine(result);
                    }

                    if (opts.dumpFlowInfos)
                    {
                        Console.WriteLine();
                        unflattener.DumpFlowInfos();

                        // Console.WriteLine();
                        // var collector = new ControlFlowTreeCollector();
                        // collector.Process(method);
                        // Console.WriteLine("Processed Control Flow Tree:");
                        // unflattener._flowRoot.Print();
                    }
                }
            }

        }); // rootCommand.SetHandler

        return rootCommand.Invoke(args);
    }

    static void ProcessCmdLineExpr(Options opts)
    {
        if (opts.printTree)
        {
            var tree = CSharpSyntaxTree.ParseText(opts.expr!);
            var printer = new SyntaxTreePrinter(tree);
            printer.Verbosity = opts.verbosity;
            printer.Print();
        }
        else
        {
            VarDB varDB = new VarDB();
            VarProcessor processor = new(varDB);
            processor.Verbosity = opts.verbosity;
            object? result = processor.EvaluateString(opts.expr!);
            Console.WriteLine($"{result}");
        }
    }
}
