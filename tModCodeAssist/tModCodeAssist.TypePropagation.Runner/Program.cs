using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using tModCodeAssist.TypePropagation.Runner.MSBuild;

namespace tModCodeAssist.TypePropagation.Runner;

internal static class Program
{
	public async static Task<int> Main(string[] args)
	{
		if (args.Length is < 1 or > 2) {
			await Console.Error.WriteLineAsync("Usage: ./propagator.exe <path-to-sln-or-csproj> [project-name]");
			return 1;
		}

		// No need to provide the project name if the input is a project.
		// If the input is a solution, then assume FirstOrDefault if no name is
		// given.
		string projPath = args[0];
		string? projName = args.Length > 1 ? args[1] : null;

		await Console.Out.WriteLineAsync($"Reading project: {projPath}...");
		using ProjectAnalyzer analyzer = await ProjectAnalyzer.CreateAsync(projPath, projName);
		await Console.Out.WriteLineAsync("Finished reading project!");

		await Console.Out.WriteLineAsync("Running type propagation...");
		var sw = Stopwatch.StartNew();
		SymbolTracker tracker = analyzer.Run(out PropagationTraceGraph trace);
		sw.Stop();
		await Console.Out.WriteLineAsync($"Finished propagating types! Elapsed: {sw.Elapsed:g}");

		PropagationDumper.TrackedSymbolsToJson(tracker, out string symbolData, out string seedData);
		await File.WriteAllTextAsync("symbols.json", symbolData);
		await File.WriteAllTextAsync("seeds.json", seedData);

		PropagationDumper.TraceGraphToJson(trace, out string traceData);
		await File.WriteAllTextAsync("trace.json", traceData);

		return 0;
	}
}