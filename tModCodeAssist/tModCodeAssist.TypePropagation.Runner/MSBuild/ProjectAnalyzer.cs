using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		var tracker = new SymbolTracker();

		foreach ((ISymbol symbol, IdKind kind) in WellKnownSeedProvider.GetSeedsForCompilation(compilation))
			tracker.TryUpdate(symbol, kind);

		bool changed;
		int i = 0;
		do {
			Console.Write($"Propagation iteration {i++}... ");
			var sw = Stopwatch.StartNew();
			changed = PropagationEngine.PropagateOnce(compilation, tracker, out int changes);
			sw.Stop();
			Console.WriteLine($"{changes} change(s) (elapsed: {sw.Elapsed:g})");
		}
		while (changed);

		return tracker;
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