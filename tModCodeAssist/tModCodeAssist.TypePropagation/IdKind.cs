using System;
using System.Collections.Generic;

namespace tModCodeAssist.TypePropagation;

/// <summary>
///		Represents a reference to a well-defined magic number type/ID.
/// </summary>
[Flags]
public enum IdKind : uint
{
	/// <summary>
	///		In place of no known ID.
	/// </summary>
	Unknown = 0,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.BuffID"/>.
	/// </summary>
	BuffID = 1u << 0,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.DustID"/>.
	/// </summary>
	DustID = 1u << 1,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ExtrasID"/>.
	/// </summary>
	ExtrasID = 1u << 2,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ItemID"/>.
	/// </summary>
	ItemID = 1u << 3,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ItemRarityID"/>.
	/// </summary>
	ItemRarityID = 1u << 4,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ItemUseStyleID"/>.
	/// </summary>
	ItemUseStyleID = 1u << 5,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.LiquidID"/>.
	/// </summary>
	LiquidID = 1u << 6,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.MessageID"/>.
	/// </summary>
	MessageID = 1u << 7,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.MountID"/>.
	/// </summary>
	MountID = 1u << 8,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.NetmodeID"/>.
	/// </summary>
	NetmodeID = 1u << 9,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.NPCAIStyleID"/>.
	/// </summary>
	NPCAIStyleID = 1u << 10,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.NPCID"/>.
	/// </summary>
	NPCID = 1u << 11,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.PaintID"/>.
	/// </summary>
	PaintID = 1u << 12,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ProjAIStyleID"/>.
	/// </summary>
	ProjAIStyleID = 1u << 13,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ProjectileID"/>.
	/// </summary>
	ProjectileID = 1u << 14,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.TileID"/>.
	/// </summary>
	TileID = 1u << 15,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.WallID"/>.
	/// </summary>
	WallID = 1u << 16,

	Ambiguous = 1u << 31,
}

public static class IdKindExtensions
{
	private static readonly Dictionary<IdKind, Type> type_map = new() {
		[IdKind.BuffID] = typeof(Terraria.ID.BuffID),
		[IdKind.DustID] = typeof(Terraria.ID.DustID),
		[IdKind.ExtrasID] = typeof(Terraria.ID.ExtrasID),
		[IdKind.ItemID] = typeof(Terraria.ID.ItemID),
		[IdKind.ItemRarityID] = typeof(Terraria.ID.ItemRarityID),
		[IdKind.ItemUseStyleID] = typeof(Terraria.ID.ItemUseStyleID),
		[IdKind.LiquidID] = typeof(Terraria.ID.LiquidID),
		[IdKind.MessageID] = typeof(Terraria.ID.MessageID),
		[IdKind.MountID] = typeof(Terraria.ID.MountID),
		[IdKind.NetmodeID] = typeof(Terraria.ID.NetmodeID),
		[IdKind.NPCAIStyleID] = typeof(Terraria.ID.NPCAIStyleID),
		[IdKind.NPCID] = typeof(Terraria.ID.NPCID),
		[IdKind.PaintID] = typeof(Terraria.ID.PaintID),
		[IdKind.ProjAIStyleID] = typeof(Terraria.ID.ProjAIStyleID),
		[IdKind.ProjectileID] = typeof(Terraria.ID.ProjectileID),
		[IdKind.TileID] = typeof(Terraria.ID.TileID),
		[IdKind.WallID] = typeof(Terraria.ID.WallID),
	};

	// Not very safe, only used when we know it's fine.
	private static Type GetCorrespondingType(this IdKind kind) => type_map[kind];

	internal static string GetCorrespondingTypeName(this IdKind kind)
		=> GetCorrespondingType(kind).FullName ?? throw new InvalidOperationException($"Couldn't get type name: {kind}");

	/// <summary>
	///		Whether this kind is precise and represents only a single known
	///		value.
	/// </summary>
	public static bool IsSingle(this IdKind kind)
		=> CountBits((uint)(kind & ~IdKind.Ambiguous)) == 1;

	/// <summary>
	///		Whether this kind is ambiguous and represents multiple known values.
	/// </summary>
	public static bool IsAmbiguous(this IdKind kind) =>
		(kind & IdKind.Ambiguous) != 0;

	/// <summary>
	///		Merges the kinds, producing a new kind.
	/// </summary>
	public static IdKind Merge(this IdKind left, IdKind right)
	{
		if (left == IdKind.Unknown)
			return right;

		if (right == IdKind.Unknown)
			return left;

		IdKind merged = left | right;
		if (CountBits((uint)(merged & ~IdKind.Ambiguous)) > 1)
			merged |= IdKind.Ambiguous;

		return merged;
	}

	// https://stackoverflow.com/a/12171691
	private static int CountBits(uint value)
	{
		int count = 0;
		while (value != 0) {
			count++;
			value &= value - 1;
		}

		return count;
	}
}