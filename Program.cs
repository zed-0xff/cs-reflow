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
        bool moveDeclarations,
        bool removeSwitchVars,
        bool preProcess,
        bool reflow,
        bool postProcess,
        List<string> dropVars,
        List<string> keepVars,
        List<string> traceVars,
        List<string> traceUniqVars,
        bool dumpFlowInfos,
        string dumpIntermediateLogs,
        List<string> debugTags
    );

    static ControlFlowUnflattener createUnflattener(string code, Options opts, HintsDictionary hints)
    {
        return new ControlFlowUnflattener(code, hints)
        {
            Verbosity = opts.verbosity,
            RemoveSwitchVars = opts.removeSwitchVars,
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

        var moveDeclarationsOpt = new Option<bool>(
            aliases: new[] { "--move-declarations", "-M" },
            getDefaultValue: () => true,
            description: "Move variable declarations to the lowest common ancestor."
        );

        var removeSwitchVarsOpt = new Option<bool>(
            name: "--remove-switch-vars",
            getDefaultValue: () => true,
            description: "Remove switch variables."
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
            filenameArg,
            methodsArg,
            exprArg,
            hintOpt,
            verbosityOpt,
            processAllOpt,
            printTreeOpt,
            addCommentsOpt,
            showAnnotationsOpt,
            moveDeclarationsOpt,
            removeSwitchVarsOpt,
            preProcessOpt,
            reflowOpt,
            postProcessOpt,
            quietOpt,
            listMethodsOpt,
            dropVarsOpt,
            keepVarsOpt,
            traceVarsOpt,
            traceUniqVarsOpt,
            dumpFlowInfosOpt,
            dumpIntermediateLogsOpt,
            debugTagsOpt
        };

        // --- Set the handler ---
        rootCommand.SetHandler((context) =>
        {
            var opts = new Options(
                filename: context.ParseResult.GetValueForArgument(filenameArg),
                methods: context.ParseResult.GetValueForArgument(methodsArg),
                expr: context.ParseResult.GetValueForOption(exprArg),
                hintList: context.ParseResult.GetValueForOption(hintOpt),
                verbosity: calc_verbosity(context, quietOpt),
                processAll: context.ParseResult.GetValueForOption(processAllOpt),
                printTree: context.ParseResult.GetValueForOption(printTreeOpt),
                addComments: context.ParseResult.GetValueForOption(addCommentsOpt),
                showAnnotations: context.ParseResult.GetValueForOption(showAnnotationsOpt),
                moveDeclarations: context.ParseResult.GetValueForOption(moveDeclarationsOpt),
                removeSwitchVars: context.ParseResult.GetValueForOption(removeSwitchVarsOpt),
                preProcess: context.ParseResult.GetValueForOption(preProcessOpt),
                reflow: context.ParseResult.GetValueForOption(reflowOpt),
                postProcess: context.ParseResult.GetValueForOption(postProcessOpt),
                listMethods: context.ParseResult.GetValueForOption(listMethodsOpt),
                dropVars: context.ParseResult.GetValueForOption(dropVarsOpt),
                keepVars: context.ParseResult.GetValueForOption(keepVarsOpt),
                traceVars: context.ParseResult.GetValueForOption(traceVarsOpt),
                traceUniqVars: context.ParseResult.GetValueForOption(traceUniqVarsOpt),
                dumpFlowInfos: context.ParseResult.GetValueForOption(dumpFlowInfosOpt),
                dumpIntermediateLogs: context.ParseResult.GetValueForOption(dumpIntermediateLogsOpt),
                debugTags: context.ParseResult.GetValueForOption(debugTagsOpt)
            );

            Logger.EnableTags(opts.debugTags);
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
                methods = unflattener.Methods.Keys.Select(k => unflattener.GetMethod(k)).ToList();
            }
            else
            {
                foreach (var methodName in opts.methods)
                {
                    // if methodName is integer
                    if (int.TryParse(methodName, out int lineno))
                        methods.Add(unflattener.GetMethod(lineno));
                    else
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
                    unflattener.DropVars(opts.dropVars);
                    unflattener.KeepVars(opts.keepVars);
                    unflattener.TraceVars(opts.traceVars);
                    unflattener.TraceUniqVars(opts.traceUniqVars);
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
}
