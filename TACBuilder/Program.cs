﻿using System.Diagnostics;
using CommandLine;
using org.jacodb.api.net.generated.models;
using TACBuilder.ILReflection;
using TACBuilder.Serialization;
using TACBuilder.Utils;

namespace TACBuilder;

class Program
{
    public class StartOptions
    {
        [Option('m', "mode", Required = true, HelpText = "The mode to use (rd, console)", Default = "console")]
        public string Mode { get; set; }

        [Option('p', "port", Required = false, HelpText = "The port to listen on, required for rd mode",
            Default = 8083)]
        public int Port { get; set; }

        [Option('f', "files", Required = false, HelpText = ".dll files to use")]
        public IEnumerable<string> InputFiles { get; set; }
    }

    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<StartOptions>(args).WithParsed(Run).WithNotParsed(HandleParseError);
    }

    private static void Run(StartOptions opts)
    {
        switch (opts.Mode)
        {
            case "rd":
            {
                RunRd(opts);
                break;
            }
            case "console":
            {
                RunConsole(opts);
                break;
            }
        }

        AppTacBuilder builder = new();


        // TODO add dynamic asm tests case
        // TODO move to tests
        // var asm = new CalliDynamicAsmBuilder().Build();
        // var name = asm.GetName();
        // AppTacBuilder.IncludeRootAsm(name);
        // builder.Build(asm);
        // var builtAsms = builder.BuiltAssemblies;
    }

    private static void RunRd(StartOptions opts)
    {
        AppTacBuilder builder = new();
        var connection = new RdConnection(req =>
        {
            foreach (var target in req.RootAsms)
            {
                AppTacBuilder.IncludeRootAsm(target);
                builder.Build(target);
            }

            var instances = AppTacBuilder.GetFreshInstances();
            Console.WriteLine(
                $".net built {instances.Count} instances with total of {instances.Select(it => (it as IlType).Methods.Count).Sum()}");
            var asmDepGraph = AppTacBuilder.GetBuiltAssemblies();
            var serialized = RdSerializer.Serialize(instances);

            return new PublicationResponse(asmDepGraph.Select(asm => new IlAsmDto(asm.Name, asm.Location)).ToList(),
                asmDepGraph.Select(asm =>
                    asm.ReferencedAssemblies.Select(referenced => new IlAsmDto(referenced.Name, referenced.Location))
                        .ToList()).ToList(), serialized);
        });
        connection.Connect(opts.Port);
    }

    private static void RunConsole(StartOptions opts)
    {
        AppTacBuilder builder = new();
        // AppTacBuilder.FilterSingleMethodFromRootAsm(opts.InputFiles.First(), "LdelemA");
        // AppTacBuilder.IncludeMsCoreLib();
        foreach (var file in opts.InputFiles)
        {
            Debug.Assert(File.Exists(file));
            AppTacBuilder.IncludeRootAsm(file);
        }

        foreach (var file in opts.InputFiles)
            builder.Build(file);
        var asmDepGraph = AppTacBuilder.GetBuiltAssemblies();
        var serialized =
            RdSerializer.Serialize(IlInstanceBuilder
                .GetFreshTypes());
    }

    static void HandleParseError(IEnumerable<Error> errs)
    {
        Console.WriteLine("Error parsing start options:");
        foreach (var err in errs)
        {
            Console.WriteLine(err.ToString());
        }
    }
}