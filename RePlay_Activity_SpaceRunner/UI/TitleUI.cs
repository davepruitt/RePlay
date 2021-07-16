using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RePlay_Activity_SpaceRunner.UI
{
    public class TitleUI
    {
        //Parameters
        private const string space_logo_texture_name = "space_logo";
        private const string runner_logo_texture_name = "runner_logo";
        private const string objective_string = "Collect coins and avoid obstacles!";

        //State
        private Texture2D SpaceLogo = null;
        private Texture2D RunnerLogo = null;
        private SpriteFont DisplayFont = null;

        private int viewportWidth;
        private int viewportHeight;

        private float SpaceOffset;
        private float RunnerOffset;
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
            spriteBatch.Draw(SpaceLogo, new Vector2(viewportWidth / 2 - SpaceLogo.Width / 2 - RunnerLogo.Width / 2 + SpaceOffset, 30), c);
            spriteBatch.Draw(RunnerLogo, new Vector2(viewportWidth / 2 + SpaceLogo.Width / 2 - RunnerLogo.Width / 2 + RunnerOffset, 30), c);

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
            SpaceOffset = (t1 * (2 - t1)).Map(0, 1, -2000, 0, true) - 20;
            RunnerOffset = (t2 * (2 - t2)).Map(0, 1, 2000, 0, true) + 20;
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
            SpaceLogo = content.Load<Texture2D>(space_logo_texture_name);
            RunnerLogo = content.Load<Texture2D>(runner_logo_texture_name);
            DisplayFont = font;
        }
    }
}
