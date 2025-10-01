using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExampleMod
{
	internal class test : ModSystem
	{
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			base.ModifyInterfaceLayers(layers);

			layers.Add(new LegacyGameInterfaceLayer("abc", () => {
				var a = Lighting.GetBufferTexture();
				Main.spriteBatch.Draw(a.texture, new Vector2(256f), Color.White);
				return true;
			}, InterfaceScaleType.None));
		}
	}
}
