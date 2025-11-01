using Microsoft.CodeAnalysis;

namespace tModCodeAssist.TypePropagation;

/// <summary>
///		Provides information about a known symbol.
/// </summary>
public sealed class SymbolInfo(ISymbol symbol, IdKind kind)
{
	public ISymbol Symbol { get; } = symbol;

	public IdKind Kind { get; set; } = kind;
}