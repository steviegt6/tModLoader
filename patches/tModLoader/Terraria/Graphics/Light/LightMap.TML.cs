using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Graphics.Light;

partial class LightMap
{
	/// <summary>
	/// Requests this lightmap as a texture, with the width and height
	/// corresponding to the size of the buffer in tiles (may flow offscreen,
	/// use the returned <c>tileArea</c> rectangle to account for padding).
	/// <para/> Rarely should you need to call this directly, instead see
	/// <see cref="ILightingEngine.GetBufferTexture"/>.
	/// <para/> This should be called every time you need the texture across
	/// frames, caching is handled by the lightmap. The texture may be
	/// arbitrarily mutated or reinitialized across frames in response to
	/// lighting updates or screen resizes.
	/// <para/> This should be called on the main thread.
	/// </summary>
	/// <returns>
	/// The full buffer texture as well as a rectangle encompassing the visible
	/// tile area within the buffer.
	/// </returns>
	public (Texture2D texture, Rectangle tileArea) GetBufferTexture()
	{
		throw new System.NotImplementedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector3 ToVector3(Vector4 value)
	{
		return new Vector3(value.X, value.Y, value.Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector4 FromVector3(Vector3 value)
	{
		return new Vector4(value.X, value.Y, value.Z, 1f);
	}
}
