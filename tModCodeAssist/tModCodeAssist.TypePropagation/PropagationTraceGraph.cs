using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace tModCodeAssist.TypePropagation;

/// <summary>
///		Records a cause-effect relationship graph to debug incorrect
///		propagations and ambiguity.
/// </summary>
public sealed class PropagationTraceGraph
{
	public readonly record struct TraceNode(
		ISymbol Source,
		IdKind Kind,
		string Reason,
		List<TraceNode> SourceTrace
	);

	private readonly Dictionary<ISymbol, List<TraceNode>> traces = new(SymbolEqualityComparer.IncludeNullability);

	public void AddTrace(ISymbol source, ISymbol target, IdKind kind, string reason)
	{
		if (!traces.TryGetValue(target, out List<TraceNode>? list))
			traces[target] = list = [];

		list.Add(new TraceNode(source, kind, reason, []));
	}

	public List<TraceNode> BuildTraceTree(ISymbol target, HashSet<ISymbol>? visited = null)
	{
		visited ??= new HashSet<ISymbol>(SymbolEqualityComparer.IncludeNullability);
		if (!visited.Add(target))
			return [];

		var result = new List<TraceNode>();

		foreach (TraceNode node in GetDirectTraces(target))
			result.Add(node with { SourceTrace = BuildTraceTree(node.Source, visited) });

		visited.Remove(target);
		return result;
	}

	private List<TraceNode> GetDirectTraces(ISymbol target) =>
		traces.TryGetValue(target, out List<TraceNode>? list) ? list : [];

	public string BuildReadableTrace(ISymbol target, SymbolTracker tracker)
	{
		List<TraceNode> rootTraces = BuildTraceTree(target);
		var sb = new StringBuilder();

		if (target is ILocalSymbol local)
			sb.Append(local.ContainingSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) + " local: ");

		sb.AppendLine($"{target.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)} ({tracker.GetKind(target)})");

		for (int i = 0; i < rootTraces.Count; i++) {
			string prefix = (i == rootTraces.Count - 1) ? "└── " : "├── ";
			RenderTraceNode(rootTraces[i], sb, prefix, "");
		}

		return sb.ToString();
	}

	private static void RenderTraceNode(TraceNode node, StringBuilder sb, string prefix, string indent)
	{
		sb.AppendLine($"{indent}{prefix}{node.Source.ToDisplayString()} ({node.Kind}) [{node.Reason}]");

		for (int i = 0; i < node.SourceTrace.Count; i++) {
			string nextPrefix = (i == node.SourceTrace.Count - 1) ? "└── " : "├── ";
			string nextIndent = indent + (prefix == "└── " ? "    " : "│   ");
			RenderTraceNode(node.SourceTrace[i], sb, nextPrefix, nextIndent);
		}
	}
}