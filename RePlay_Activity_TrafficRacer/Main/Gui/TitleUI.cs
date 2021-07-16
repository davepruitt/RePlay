using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RePlay_Activity_TrafficRacer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Activity_TrafficRacer.Gui
{
	class TitleUI
	{
        //Parameters
        const string trafficTexName = "traffic";
        const string racerTexName = "racer";
        const string fontName = "GameFont";
        const string easyText = "Stay in the lane marked with green arrows.";
        const string middleText = "Collect as many coins as possible.";
        const string hardText = "Watch out for cars!";

        //State
        static Texture2D traffic;
		static Texture2D racer;
		static SpriteFont font;

		float trafficOffset;
		float racerOffset;
        float objectiveAlpha;
        float titleAlpha;
        string objective;

        public TitleUI()
        {
            if (TrafficGame.Difficulty < 0.2f)
            {
                objective = easyText;
            } else if (TrafficGame.Difficulty <= 0.3)
            {
                objective = middleText;
            } else
            {
                objective = hardText;
            }
        }

		public void Render(SpriteBatch spriteBatch, GameTime gameTime)
		{
            int viewportWidth = TrafficGame.Graphics.Viewport.Width;
            int viewportHeight = TrafficGame.Graphics.Viewport.Height;

            Color c = new Color(titleAlpha, titleAlpha, titleAlpha, titleAlpha);
            spriteBatch.Draw(traffic, new Vector2(viewportWidth / 2 - traffic.Width / 2 + trafficOffset, 30), c);
			spriteBatch.Draw(racer, new Vector2(viewportWidth / 2 - racer.Width / 2 + racerOffset, traffic.Height + 50), c);

            CenteredText("Objective:", spriteBatch, viewportWidth, 500, 1f, objectiveAlpha);
            CenteredText(objective, spriteBatch, viewportWidth, 600, 1f, objectiveAlpha);
		}

		void CenteredText(string text, SpriteBatch spriteBatch, float viewportWidth, float y, float scale, float alpha)
		{
			float x = viewportWidth / 2 - font.MeasureString(text).X * scale / 2;
			y = y - font.MeasureString(text).Y * scale / 2;
			spriteBatch.DrawString(font, text, new Vector2(x - 5*scale, y + 5*scale), new Color(0, 0, 0, alpha), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
			spriteBatch.DrawString(font, text, new Vector2(x, y), new Color(alpha, alpha, alpha, alpha), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
		}

		public void Update(GameTime gameTime)
		{
			float t = (float) gameTime.TotalGameTime.TotalSeconds - 1; //Time since we began

            //Compute various normalized time ranges used in title animations
			float t1 = t.Map(0, 0.5f, 0, 1, true);
			float t2 = t.Map(0.5f, 1f, 0, 1, true);
            float t3 = t.Map(1.5f, 2f, 0, 1, true);
            float t4 = t.Map(2.5f, 3f, 0, 1, true);
            float t5 = t.Map(5f, 5.5f, 0, 1, true);

            //Compute various animation parameters based on those time ranges
			trafficOffset = (t1 * (2 - t1)).Map(0, 1, -800, 0, true);
			racerOffset = (t2 * (2 - t2)).Map(0, 1, 800, 0, true);
            titleAlpha = t4.Map(0, 1, 1, 0);

            if (t < 2)
            {
                objectiveAlpha = t3;
            } else
            {
                objectiveAlpha = t5.Map(0, 1, 1, 0);
            }
		}

		public static void LoadContent(ContentManager content)
		{
            traffic = content.Load<Texture2D>(trafficTexName);
			racer = content.Load<Texture2D>(racerTexName);
			font = content.Load<SpriteFont>(fontName);
		}
	}
}
