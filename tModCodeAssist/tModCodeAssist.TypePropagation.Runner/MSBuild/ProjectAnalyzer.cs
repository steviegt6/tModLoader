using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace tModCodeAssist.TypePropagation.Runner.MSBuild;

/// <summary>
///		Analyzes an MSBuild project.
/// </summary>
public sealed class ProjectAnalyzer : IDisposable
{
	private readonly MSBuildWorkspace workspace;
	private readonly Compilation compilation;

	public ProjectAnalyzer(string projectPath, string? projectName)
	{
		workspace = MSBuildWorkspace.Create();

		Project project;
		if (projectPath.EndsWith(".sln")) {
			Solution solution = workspace.OpenSolutionAsync(projectPath).GetAwaiter().GetResult();
			project = projectName is not null ? solution.Projects.First(x => x.Name == projectName) : solution.Projects.First();
		}
		else if (projectPath.EndsWith(".csproj")) {
			project = workspace.OpenProjectAsync(projectPath).GetAwaiter().GetResult();
		}
		else {
			throw new InvalidOperationException($"Unknown project file kind: {projectPath}");
		}

		compilation = project.GetCompilationAsync().GetAwaiter().GetResult()
			?? throw new InvalidOperationException("Failed to get compilation");
	}

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
}