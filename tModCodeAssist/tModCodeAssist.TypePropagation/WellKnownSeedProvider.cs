using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using tModCodeAssist.Bindings;

namespace tModCodeAssist.TypePropagation;

/// <summary>
///		Provides well-known starting seed sets to propagate types from.
/// </summary>
public static class WellKnownSeedProvider
{
	private static readonly IdKind[] id_kinds = [
		IdKind.BuffID,
		IdKind.DustID,
		IdKind.ExtrasID,
		IdKind.ItemID,
		IdKind.ItemRarityID,
		IdKind.ItemUseStyleID,
		IdKind.LiquidID,
		IdKind.MessageID,
		IdKind.MountID,
		IdKind.NetmodeID,
		IdKind.NPCAIStyleID,
		IdKind.NPCID,
		IdKind.PaintID,
		IdKind.ProjAIStyleID,
		IdKind.ProjectileID,
		IdKind.TileID,
		IdKind.WallID,
	];

	/// <summary>
	///		Gets well-defined seeds for a compilation from the basic data.
	/// </summary>
	public static IEnumerable<(ISymbol symbol, IdKind kind)> GetSeedsForCompilation(Compilation compilation)
	{
		foreach (IdKind idKind in id_kinds) {
			INamedTypeSymbol? idType = compilation.GetTypeByMetadataName(idKind.GetCorrespondingTypeName());
			if (idType is null) {
				Debug.Assert(idType is not null);
				continue;
			}

			foreach (ISymbol? member in idType.GetMembers()) {
				if (member is not IFieldSymbol { IsStatic: true } field || !PropagationEngine.IsNumericType(field.Type.SpecialType))
					continue;

				yield return (field, idKind);
			}

			if (idType.GetTypeMembers("Sets").FirstOrDefault() is not { } sets)
				continue;

			foreach (ISymbol? setMember in sets.GetMembers()) {
				if (setMember is not IFieldSymbol { IsStatic: true } field)
					continue;

				if (field.Type is not IArrayTypeSymbol)
					continue;

				yield return (setMember, idKind);
			}
		}
	}
}