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
	BuffID = 1 << 0,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.DustID"/>.
	/// </summary>
	DustID = 1 << 1,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ExtrasID"/>.
	/// </summary>
	ExtrasID = 1 << 2,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ItemID"/>.
	/// </summary>
	ItemID = 1 << 3,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ItemRarityID"/>.
	/// </summary>
	ItemRarityID = 1 << 4,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ItemUseStyleID"/>.
	/// </summary>
	ItemUseStyleID = 1 << 5,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.LiquidID"/>.
	/// </summary>
	LiquidID = 1 << 6,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.MessageID"/>.
	/// </summary>
	MessageID = 1 << 7,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.MountID"/>.
	/// </summary>
	MountID = 1 << 8,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.NetmodeID"/>.
	/// </summary>
	NetmodeID = 1 << 9,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.NPCAIStyleID"/>.
	/// </summary>
	NPCAIStyleID = 1 << 10,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.NPCID"/>.
	/// </summary>
	NPCID = 1 << 11,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.PaintID"/>.
	/// </summary>
	PaintID = 1 << 12,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ProjAIStyleID"/>.
	/// </summary>
	ProjAIStyleID = 1 << 13,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.ProjectileID"/>.
	/// </summary>
	ProjectileID = 1 << 14,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.TileID"/>.
	/// </summary>
	TileID = 1 << 15,

	/// <summary>
	///     Corresponds to <see cref="Terraria.ID.WallID"/>.
	/// </summary>
	WallID = 1 << 16,
}

internal static class IdKindExtensions
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
	public static Type GetCorrespondingType(this IdKind @this) => type_map[@this];

	public static string GetCorrespondingTypeName(this IdKind @this)
		=> GetCorrespondingType(@this).FullName ?? throw new InvalidOperationException($"Couldn't get type name: {@this}");
}