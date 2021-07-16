using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RePlay_Activity_FruitNinja.UI
{
    public class TitleUI
    {
        //Parameters
        private const string fruit_logo_texture_name = "fruit_logo";
        private const string ninja_logo_texture_name = "ninja_logo";
        private const string objective_string = "Slice as many fruit as you can while avoiding bombs!";

        //State
        private static Texture2D FruitLogo;
        private static Texture2D NinjaLogo;
        private static SpriteFont DisplayFont;

        private int viewportWidth;
        private int viewportHeight;

        private float FruitOffset;
        private float NinjaOffset;
        private float ObjectiveAlpha;
        private float TitleAlpha;

        public TitleUI(int w, int h)
        {
            viewportWidth = w;
            viewportHeight = h;
        }

        public void Render(SpriteBatch spriteBatch, GameTime gameTime)
        {
            Color c = new Color(TitleAlpha, TitleAlpha, TitleAlpha, TitleAlpha);
            spriteBatch.Draw(FruitLogo, new Vector2(viewportWidth / 2 - FruitLogo.Width / 2 - NinjaLogo.Width / 2 + FruitOffset, 30), c);
            spriteBatch.Draw(NinjaLogo, new Vector2(viewportWidth / 2 + FruitLogo.Width / 2 - NinjaLogo.Width / 2 + NinjaOffset, 30), c);

            CenteredText("Objective:", spriteBatch, viewportWidth, 250, 1f, ObjectiveAlpha);
            CenteredText(objective_string, spriteBatch, viewportWidth, 320, .75f, ObjectiveAlpha);
        }

        void CenteredText(string text, SpriteBatch spriteBatch, float viewportWidth, float y, float scale, float alpha)
        {
            float x = viewportWidth / 2 - DisplayFont.MeasureString(text).X * scale / 2;
            y = y - DisplayFont.MeasureString(text).Y * scale / 2;
            spriteBatch.DrawString(DisplayFont, text, new Vector2(x - 5 * scale, y + 5 * scale), new Color(0, 0, 0, alpha), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            spriteBatch.DrawString(DisplayFont, text, new Vector2(x, y), new Color(alpha, alpha, alpha, alpha), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        public void Update(GameTime gameTime)
        {
            float t = (float)gameTime.TotalGameTime.TotalSeconds - 1; //Time since we began

            //Compute various normalized time ranges used in title animations
            float t1 = t.Map(0, 0.5f, 0, 1, true);
            float t2 = t.Map(0.5f, 1f, 0, 1, true);
            float t3 = t.Map(1.5f, 2f, 0, 1, true);
            float t4 = t.Map(2.5f, 3f, 0, 1, true);
            float t5 = t.Map(5f, 5.5f, 0, 1, true);

            //Compute various animation parameters based on those time ranges
            FruitOffset = (t1 * (2 - t1)).Map(0, 1, -2000, 0, true) - 20;
            NinjaOffset = (t2 * (2 - t2)).Map(0, 1, 2000, 0, true) + 20;
            TitleAlpha = t4.Map(0, 1, 1, 0);

            if (t < 2)
            {
                ObjectiveAlpha = t3;
            }
            else
            {
                ObjectiveAlpha = t5.Map(0, 1, 1, 0);
            }
        }

        public void LoadContent(ContentManager content, SpriteFont font)
        {
            FruitLogo = content.Load<Texture2D>(fruit_logo_texture_name);
            NinjaLogo = content.Load<Texture2D>(ninja_logo_texture_name);
            DisplayFont = font;
        }
    }
}
