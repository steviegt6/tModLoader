using System.Collections.Generic;
using System.Diagnostics;
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
	///		Gets well-defined seeds for a compilation from the basic data
	///		defined in <see cref="MagicNumberBindings"/>.
	/// </summary>
	/// <param name="compilation"></param>
	/// <returns></returns>
	public static IEnumerable<(ISymbol symbol, IdKind kind)> GetSeedsForCompilation(Compilation compilation)
	{
		foreach (IdKind idKind in id_kinds) {
			INamedTypeSymbol? idType = compilation.GetTypeByMetadataName(idKind.Name);
			if (idType is null) {
				Debug.Assert(idType is not null);
				continue;
			}

			foreach (ISymbol? member in idType.GetMembers()) {
				if (member is IFieldSymbol { IsStatic: true } field && IsIntegerType(field.Type.SpecialType)) {
					yield return (field, idKind);
				}
			}
		}
	}

	private static bool IsIntegerType(SpecialType type)
	{
		return type is SpecialType.System_Byte
		            or SpecialType.System_SByte
		            or SpecialType.System_Int16
		            or SpecialType.System_UInt16
		            or SpecialType.System_Int32
		            or SpecialType.System_UInt32
		            or SpecialType.System_Int64
		            or SpecialType.System_UInt64;
	}
}