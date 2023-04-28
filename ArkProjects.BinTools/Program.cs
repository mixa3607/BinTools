using System.CommandLine;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using Serilog.Extensions.Logging;

namespace ArkProjects.BinTools;

public static class Program
{
    private static ILoggerFactory? _loggerFactory;

    public static int Main(string[] args)
    {
        InitLogging();
        var rootCommand = new RootCommand("BinTools - fw helper");

        {
            var partsFileOpt = new Option<string>(
                aliases: new[] { "--parts", "-p" },
                description: "Json file with defined partitions",
                getDefaultValue: () => "./parts.json");
            var inFileOpt = new Option<string>(
                aliases: new[] { "--in", "-i" },
                description: "Source file");
            var outDirOpt = new Option<string>(
                aliases: new[] { "--out", "-o" },
                description: "Output directory");
            var partNamesOpt = new Option<string[]>(
                aliases: new[] { "--names", "-n" },
                description: "Specific part names. Leave empty for all",
                getDefaultValue: Array.Empty<string>);
            var overwriteOpt = new Option<bool>(
                aliases: new[] { "--overwrite" },
                description: "Overwrite existed files",
                getDefaultValue: () => true);
            var command = new Command("split", "Split file to partitions")
            {
                partsFileOpt,
                inFileOpt,
                outDirOpt,
                partNamesOpt,
                overwriteOpt
            };
            command.SetHandler(async (parts, inFile, outDir, names, overwrite) =>
            {
                var partitions = JsonConvert.DeserializeObject<PartitionsSettings>(await File.ReadAllTextAsync(parts))!;
                var toolkit = new Toolkit(partitions.Partitions, _loggerFactory!.CreateLogger<Toolkit>());
                await toolkit.SplitBinToPartsAsync(inFile, outDir, names, overwrite);
            }, partsFileOpt, inFileOpt, outDirOpt, partNamesOpt, overwriteOpt);
            rootCommand.AddCommand(command);
        }
        {
            var partsFileOpt = new Option<string>(
                aliases: new[] { "--parts", "-p" },
                description: "Json file with defined partitions",
                getDefaultValue: () => "./parts.json");
            var inDirOpt = new Option<string>(
                aliases: new[] { "--in", "-i" },
                description: "Source directory");
            var outFileOpt = new Option<string>(
                aliases: new[] { "--out", "-o" },
                description: "Output file");
            var partNamesOpt = new Option<string[]>(
                aliases: new[] { "--names", "-n" },
                description: "Specific part names. Leave empty for all",
                getDefaultValue: Array.Empty<string>);
            var overwriteOpt = new Option<bool>(
                aliases: new[] { "--overwrite" },
                description: "Overwrite existed file",
                getDefaultValue: () => true);
            var command = new Command("concat", "Concat partitions to file")
            {
                partsFileOpt,
                inDirOpt,
                outFileOpt,
                partNamesOpt,
                overwriteOpt
            };
            command.SetHandler(async (parts, inDir, outFile, names, overwrite) =>
            {
                var partitions = JsonConvert.DeserializeObject<PartitionsSettings>(await File.ReadAllTextAsync(parts))!;
                var toolkit = new Toolkit(partitions.Partitions, _loggerFactory!.CreateLogger<Toolkit>());
                await toolkit.ConcatPartsToBin(inDir, outFile, names, overwrite);
            }, partsFileOpt, inDirOpt, outFileOpt, partNamesOpt, overwriteOpt);
            rootCommand.AddCommand(command);
        }
        {
            var inFileOpt = new Option<string>(
                aliases: new[] { "--in", "-i" },
                description: "Source file");
            var outFileOpt = new Option<string>(
                aliases: new[] { "--out", "-o" },
                description: "Output file");
            var targetSizeOpt = new Option<long>(
                aliases: new[] { "--size", "-s" },
                description: "Target size",
                parseArgument: x => NumHelper.ParseI64(x.Tokens[0].Value));
            var padWithOpt = new Option<byte>(
                aliases: new[] { "--pad" },
                description: "Byte for padding",
                isDefault: true,
                parseArgument: x => NumHelper.ParseB(x.Tokens.Count > 0 ? x.Tokens[0].Value : "0xFF"));
            var overwriteOpt = new Option<bool>(
                aliases: new[] { "--overwrite" },
                description: "Overwrite existed file",
                getDefaultValue: () => true);
            var command = new Command("adjust", "Strip or pad file to target size")
            {
                inFileOpt,
                outFileOpt,
                padWithOpt,
                targetSizeOpt,
                overwriteOpt
            };
            command.SetHandler(async (inDir, outFile, targetSize, padWith, overwrite) =>
            {
                var toolkit = new Toolkit(Array.Empty<Partition>(), _loggerFactory!.CreateLogger<Toolkit>());
                await toolkit.AdjustSizeAsync(inDir, outFile, targetSize, padWith, overwrite);
            }, inFileOpt, outFileOpt, targetSizeOpt, padWithOpt, overwriteOpt);
            rootCommand.AddCommand(command);
        }

        return rootCommand.InvokeAsync(args).Result;
    }

    private static void InitLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new SerilogLoggerProvider(Log.Logger));
            builder.SetMinimumLevel(LogLevel.Trace);
        });
    }
}