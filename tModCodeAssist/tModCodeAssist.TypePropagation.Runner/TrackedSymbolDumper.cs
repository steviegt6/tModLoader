using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace tModCodeAssist.TypePropagation.Runner;

internal static class TrackedSymbolDumper
{
	private static readonly JsonSerializerOptions json_options = new() { WriteIndented = true };

	public static void ToJson(SymbolTracker tracker, out string symbolData, out string seedData)
	{
		Dictionary<string, Dictionary<string, Dictionary<string, string>>> symbols =
			BuildTable(tracker.SymbolKinds.Where(x => !tracker.IsSeed(x.Key)));
		Dictionary<string, Dictionary<string, Dictionary<string, string>>> seeds =
			BuildTable(tracker.SymbolKinds.Where(x => tracker.IsSeed(x.Key)));

		symbolData = JsonSerializer.Serialize(symbols, json_options);
		seedData = JsonSerializer.Serialize(seeds, json_options);
	}

	private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> BuildTable(IEnumerable<KeyValuePair<ISymbol, IdKind>> data)
	{
		// assembly -> type -> symbol -> data (w/ kind)
		var result = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

		foreach ((ISymbol symbol, IdKind kind) in data) {
			if (!IsSerializableSymbol(symbol))
				continue;

			string kindName = kind.ToString();
			string asmName = symbol.ContainingAssembly?.Name ?? "<unknown>";
			string typeName = symbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "<global>";
			string symbolName = GetReadableSymbolName(symbol);

			if (!result.TryGetValue(asmName, out Dictionary<string, Dictionary<string, string>>? typeDict))
				result[asmName] = typeDict = [];

			if (!typeDict.TryGetValue(typeName, out Dictionary<string, string>? symbolsDict))
				typeDict[typeName] = symbolsDict = [];

			symbolsDict[symbolName] = kindName;
		}

		return result;
	}

	private static bool IsSerializableSymbol(ISymbol symbol)
	{
		/*
		if (symbol.IsImplicitlyDeclared)
			return false;
		*/

		return symbol.Kind switch {
			// Notably does not include Local.
			SymbolKind.Field => true,
			SymbolKind.Property => true,
			SymbolKind.Method => true,
			SymbolKind.Parameter => true,
			SymbolKind.NamedType => true,
			_ => false,
		};
	}

	private static string GetReadableSymbolName(ISymbol symbol)
	{
		return symbol switch {
			// MethodName(Type arg1, Type2 arg2).arg1
			IParameterSymbol param => $"{((IMethodSymbol)param.ContainingSymbol).ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}.{param.Name}",
			// MethodName(Type arg1, Type2 arg2)
			IMethodSymbol method => method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
			_ => symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
		};
	}
}