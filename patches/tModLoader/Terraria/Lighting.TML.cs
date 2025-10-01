using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria;

partial class Lighting
{
	/// <summary>
	/// Requests the current lightmap as a texture, with the width and height
	/// corresponding to the size of the buffer in tiles (may flow offscreen,
	/// use the returned <c>tileArea</c> rectangle to account for padding).
	/// <para/> This should be called every time you need the texture across
	/// frames, caching is handled by the engine/lightmap. The texture may be
	/// arbitrarily mutated or reinitialized across frames in response to
	/// lighting updates or screen resizes.
	/// <para/> This should be called on the main thread.
	/// </summary>
	/// <returns>
	/// The full buffer texture as well as a rectangle encompassing the visible
	/// tile area within the buffer.
	/// </returns>
	public static (Texture2D texture, Rectangle tileArea) GetBufferTexture()
	{
		return _activeEngine.GetBufferTexture();
	}
}
