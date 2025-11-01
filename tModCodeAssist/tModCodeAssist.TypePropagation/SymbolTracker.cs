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
	private readonly Dictionary<ISymbol, IdKind> symbolKinds = new(SymbolEqualityComparer.IncludeNullability);

	/// <summary>
	///		Attempts to mark a symbol as of a given symbol kind.
	/// </summary>
	/// <returns>Whether a change has been made.</returns>
	public bool TryUpdate(ISymbol symbol, IdKind kind)
	{
		if (symbolKinds.TryGetValue(symbol, out IdKind existing)) {
			if (existing == kind) {
				return false;
			}

			// Error case.
			symbolKinds[symbol] = IdKind.Multiple;
			return true;
		}

		symbolKinds[symbol] = existing;
		return true;
	}

	/// <summary>
	///		Gets the <see cref="IdKind"/> of a symbol.
	/// </summary>
	/// <returns>The symbol kind, or <see cref="IdKind.Unknown"/>.</returns>
	public IdKind GetKind(ISymbol symbol) =>
		symbolKinds.TryGetValue(symbol, out IdKind kind) ? kind : IdKind.Unknown;
}