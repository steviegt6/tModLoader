using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace tModCodeAssist.TypePropagation.Runner.MSBuild;

/// <summary>
///		Analyzes an MSBuild project.
/// </summary>
public sealed class ProjectAnalyzer(
	MSBuildWorkspace workspace,
	Compilation compilation
) : IDisposable
{
	public SymbolTracker Run()
	{
		IEnumerable<(ISymbol symbol, IdKind kind)> seeds = WellKnownSeedProvider.GetSeedsForCompilation(compilation);
		Console.WriteLine(seeds);
		// TODO

		return new SymbolTracker();
	}

	public void Dispose()
	{
		workspace.Dispose();
	}

	public static async Task<ProjectAnalyzer> CreateAsync(string projectPath, string? projectName)
	{
		var workspace = MSBuildWorkspace.Create();

		Project project;
		if (projectPath.EndsWith(".sln")) {
			Solution solution = await workspace.OpenSolutionAsync(projectPath);
			project = projectName is not null ? solution.Projects.First(x => x.Name == projectName) : solution.Projects.First();
		}
		else if (projectPath.EndsWith(".csproj")) {
			project = await workspace.OpenProjectAsync(projectPath);
		}
		else {
			throw new InvalidOperationException($"Unknown project file kind: {projectPath}");
		}

		Compilation compilation = project.GetCompilationAsync().GetAwaiter().GetResult()
		                       ?? throw new InvalidOperationException("Failed to get compilation");

		return new ProjectAnalyzer(workspace, compilation);
	}
}