using System;
using Terraria.ID;

namespace tModCodeAssist.TypePropagation;

/// <summary>
///		Represents a reference to a well-defined magic number type/ID.
/// </summary>
public readonly record struct IdKind(string Name)
{
	/// <summary>
	///		Represents an unknown type, used in place of no entry if a value
	///		must exist.
	/// </summary>
	public static IdKind Unknown { get; } = new("<unknown>");

	/// <summary>
	///		Represents an error case where a symbol is assigned more than one
	///		symbol kind.
	/// </summary>
	public static IdKind Multiple { get; } = new("<multiple>");

#region Terraria IDs
	/// <summary>
	///		Represents <see cref="Terraria.ID.BuffID"/>.
	/// </summary>
	public static IdKind BuffID { get; } = new(typeof(BuffID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.DustID"/>.
	/// </summary>
	public static IdKind DustID { get; } = new(typeof(DustID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.ExtrasID"/>.
	/// </summary>
	public static IdKind ExtrasID { get; } = new(typeof(ExtrasID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.ItemID"/>.
	/// </summary>
	public static IdKind ItemID { get; } = new(typeof(ItemID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.ItemRarityID"/>.
	/// </summary>
	public static IdKind ItemRarityID { get; } = new(typeof(ItemRarityID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.ItemUseStyleID"/>.
	/// </summary>
	public static IdKind ItemUseStyleID { get; } = new(typeof(ItemUseStyleID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.LiquidID"/>.
	/// </summary>
	public static IdKind LiquidID { get; } = new(typeof(LiquidID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.MessageID"/>.
	/// </summary>
	public static IdKind MessageID { get; } = new(typeof(MessageID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.MountID"/>.
	/// </summary>
	public static IdKind MountID { get; } = new(typeof(MountID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.NetmodeID"/>.
	/// </summary>
	public static IdKind NetmodeID { get; } = new(typeof(NetmodeID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.NPCAIStyleID"/>.
	/// </summary>
	public static IdKind NPCAIStyleID { get; } = new(typeof(NPCAIStyleID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.NPCID"/>.
	/// </summary>
	public static IdKind NPCID { get; } = new(typeof(NPCID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.PaintID"/>.
	/// </summary>
	public static IdKind PaintID { get; } = new(typeof(PaintID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.ProjAIStyleID"/>.
	/// </summary>
	public static IdKind ProjAIStyleID { get; } = new(typeof(ProjAIStyleID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.ProjectileID"/>.
	/// </summary>
	public static IdKind ProjectileID { get; } = new(typeof(ProjectileID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.TileID"/>.
	/// </summary>
	public static IdKind TileID { get; } = new(typeof(TileID));

	/// <summary>
	///		Represents <see cref="Terraria.ID.WallID"/>.
	/// </summary>
	public static IdKind WallID { get; } = new(typeof(WallID));
#endregion

	private IdKind(Type type) : this(type.FullName ?? type.Name) { }
}