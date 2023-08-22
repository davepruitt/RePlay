using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Activity_TrafficRacer.Graphics
{
    //Class which allows us to render everything to a separate texture,
    //and then apply an effect when rendering that texture to screen
	class PostProcessor
	{
		SpriteBatch spriteBatch;
		RenderTarget2D target;
		Effect effect;

		public PostProcessor(SpriteBatch spriteBatch, Effect effect)
		{
			this.spriteBatch = spriteBatch;
			this.effect = effect;
			target = new RenderTarget2D(TrafficGame.Graphics, TrafficGame.Graphics.Viewport.Width, TrafficGame.Graphics.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
		}

		public void Begin()
		{
            TrafficGame.Graphics.SetRenderTarget(target);
		}

        //Only applies post processing effects if apply = true
		public void End(bool apply=true)
		{
            TrafficGame.Graphics.SetRenderTarget(null);
			if (apply)
				spriteBatch.Begin(effect: effect);
			else
				spriteBatch.Begin();
			spriteBatch.Draw(target, Vector2.Zero, Color.White);
			spriteBatch.End();
		}

	}
}
