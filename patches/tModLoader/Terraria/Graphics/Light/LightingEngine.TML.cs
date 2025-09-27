using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Graphics.Light;

partial class LightingEngine
{
	public (Texture2D texture, Rectangle tileArea) GetBufferTexture()
	{
		return _activeLightMap.GetBufferTexture();
	}
}
