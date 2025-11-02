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

	/// <summary>
	///		Attempts to mark a symbol as of a given symbol kind.
	/// </summary>
	/// <returns>Whether a change has been made.</returns>
	public bool TryUpdate(ISymbol symbol, IdKind kind)
	{
		if (SymbolKinds.TryGetValue(symbol, out IdKind existing)) {
			if (existing == kind)
				return false;

			if (existing == IdKind.Multiple)
				return false;

			// Error case.
			SymbolKinds[symbol] = IdKind.Multiple;
			return true;
		}

		SymbolKinds[symbol] = kind;
		return true;
	}

	/// <summary>
	///		Gets the <see cref="IdKind"/> of a symbol.
	/// </summary>
	/// <returns>The symbol kind, or <see cref="IdKind.Unknown"/>.</returns>
	public IdKind GetKind(ISymbol symbol) =>
		SymbolKinds.TryGetValue(symbol, out IdKind kind) ? kind : IdKind.Unknown;
}