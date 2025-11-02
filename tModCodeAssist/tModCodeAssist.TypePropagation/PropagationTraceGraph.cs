using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace tModCodeAssist.TypePropagation;

/// <summary>
///		Records a cause-effect relationship graph to debug incorrect
///		propagations and ambiguity.
/// </summary>
public sealed class PropagationTraceGraph
{
	public sealed record TraceEdge(ISymbol Source, ISymbol Target, IdKind PropagatedKind, string Reason);

	private readonly Dictionary<ISymbol, List<TraceEdge>> edges = new(SymbolEqualityComparer.IncludeNullability);

	public void Add(ISymbol source, ISymbol target, IdKind kind, string reason)
	{
		if (!edges.TryGetValue(target, out List<TraceEdge>? list))
			edges[target] = list = [];

		list.Add(new TraceEdge(source,  target, kind, reason));
	}

	public IEnumerable<TraceEdge> GetIncoming(ISymbol symbol) =>
		edges.TryGetValue(symbol, out List<TraceEdge>? list) ? list : [];

	public IEnumerable<KeyValuePair<ISymbol, List<TraceEdge>>> AllEdges() => edges;
}