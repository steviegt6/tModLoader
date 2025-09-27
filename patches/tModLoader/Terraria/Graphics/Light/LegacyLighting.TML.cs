using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Graphics.Light;

partial class LegacyLighting
{
	public (Texture2D texture, Rectangle tileArea) GetBufferTexture()
	{
		return _lightMap.GetBufferTexture();
	}
}
