using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace tModCodeAssist.TypePropagation;

/// <summary>
///		Globally tracks known symbol types, also tracking whether a dirtying
///		update has occurred.
/// </summary>
public sealed class SymbolTracker
{
	// IncludeNullability means symbols representing int and int? will not be
	// considered equal.
	// TODO: Is this behavior desirable?
	public Dictionary<ISymbol, IdKind> SymbolKinds { get; } = new(SymbolEqualityComparer.IncludeNullability);

	private readonly HashSet<ISymbol> seeds = new(SymbolEqualityComparer.IncludeNullability);

	/// <summary>
	///		Attempts to mark a symbol as of a given symbol kind.
	/// </summary>
	/// <returns>Whether a change has been made.</returns>
	public bool TryUpdate(ISymbol? symbol, IdKind kind)
	{
		if (symbol is null || kind.IsUnknown())
			return false;

		if (seeds.Contains(symbol))
			return false;

		if (SymbolKinds.TryGetValue(symbol, out IdKind existing)) {
			if (kind.IsAmbiguous() && existing.IsSingle())
				return false;

			IdKind merged = existing | kind;
			if (merged.IsAmbiguous() && !existing.IsAmbiguous())
				Console.WriteLine($"Ambiguity: {symbol.ToDisplayString()} merged {existing} + {kind} = {merged}");

			if (merged == existing)
				return false;

			SymbolKinds[symbol] = merged;
			return true;
		}

		SymbolKinds[symbol] = kind;
		return true;
	}

	/// <summary>
	///		Adds a seed value which may not be overwritten, creating a canonical
	///		symbol-kind relation.
	/// </summary>
	public void AddSeed(ISymbol symbol, IdKind kind)
	{
		SymbolKinds[symbol] = kind;
		seeds.Add(symbol);
	}

	/// <summary>
	///		Gets the <see cref="IdKind"/> of a symbol.
	/// </summary>
	/// <returns>The symbol kind, or <see cref="IdKind.Unknown"/>.</returns>
	public IdKind GetKind(ISymbol symbol) =>
		SymbolKinds.TryGetValue(symbol, out IdKind kind) ? kind : IdKind.Unknown;

	/// <summary>
	///		Whether this symbol is an immutable seed value.
	/// </summary>
	public bool IsSeed(ISymbol symbol) => seeds.Contains(symbol);
}