using System;
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

		using ProjectAnalyzer analyzer = await ProjectAnalyzer.CreateAsync(projPath, projName);
		SymbolTracker tracker = analyzer.Run();

		return 0;
	}
}