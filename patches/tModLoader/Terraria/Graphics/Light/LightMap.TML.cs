#nullable enable

using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace Terraria.Graphics.Light;

partial class LightMap
{
	private Texture2D? bufferTexture;
	private bool bufferNeedsUpdating;

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
	public unsafe (Texture2D texture, Rectangle tileArea) GetBufferTexture()
	{
		var width = Width + 1;
		var height = Height + 1;

		// TODO
		Rectangle tileArea = new(0, 0, width, height);

		if (bufferTexture == null) {
			bufferTexture = InitBufferTexture(width, height);
			bufferNeedsUpdating = true;
		}
		else if (bufferTexture.Width != width || bufferTexture.Height != height) {
			bufferTexture?.Dispose();
			bufferTexture = InitBufferTexture(width, height);
			bufferNeedsUpdating = true;
		}

		if (bufferNeedsUpdating) {
			fixed (Vector4* pColors = &_colors[0]) {
				bufferTexture.SetDataPointerEXT(0, null, (nint)pColors, width * height);
			}

			bufferNeedsUpdating = false;
		}

		return (bufferTexture, tileArea);
	}

	private static Texture2D InitBufferTexture(int width, int height)
	{
		if (!AssetRepository.IsMainThread) {
			return Main.RunOnMainThread(() => InitBufferTexture(width, height)).GetAwaiter().GetResult();
		}

		return new Texture2D(Main.instance.GraphicsDevice, width, height, mipMap: false, format: SurfaceFormat.Vector4);
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
