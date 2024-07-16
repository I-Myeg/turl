using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Net;
using AddCode.Practice.Turl.Models;
using AddCode.Practice.Turl.Utils;
using Tomlyn;
using Tomlyn.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AddCode.Practice.Turl;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand
        {
            new Argument<string[]>("paths")
            {
                Description = "The paths to the test files.",
                Arity = ArgumentArity.OneOrMore
            }
        };

        rootCommand.Description = "CLI for running HTTP tests defined in YAML or TOML files";

        rootCommand.Handler = CommandHandler.Create<string[]>(async (paths) =>
        {
            var globalSuccessCount = 0;
            var globalTestCount = 0;
            var totalStopwatch = Stopwatch.StartNew();

            foreach (var path in paths)
            {
                var testFile = FileUtils.LoadTestFile(path);
                var globalHeaders = testFile.Headers;
                var globalTimeout = testFile.Timeout;
                var testRunner = new TestRunner(new HttpClient(), globalHeaders, globalTimeout);
                var (testCount, successCount) = await testRunner.RunTestsAsync(testFile);

                globalTestCount += testCount;
                globalSuccessCount += successCount;
            }

            totalStopwatch.Stop();
            Console.WriteLine($"------------------------------------------------------------------------------------");
            Console.WriteLine($"Execute files: {paths.Length}");
            Console.WriteLine($"Execute tests: {globalTestCount}");
            Console.WriteLine($"Success tests: {globalSuccessCount}");
            Console.WriteLine($"Failed tests: {globalTestCount - globalSuccessCount}");
            Console.WriteLine($"Duration: {totalStopwatch.ElapsedMilliseconds}ms");
        });

        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .Build();

        return await rootCommand.InvokeAsync(args);
    }
}
    
    

