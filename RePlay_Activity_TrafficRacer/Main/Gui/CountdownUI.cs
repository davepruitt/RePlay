using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Activity_TrafficRacer.Gui
{
    class CountdownUI
    {
        //Parameters
        public string FinalText { get; set; } = "GREAT WORKOUT";
        const string fontName = "GameFont";
        const string restartFontName = "RestartFont";
        public int Threshold { get; set; } = 10;
        public bool Restarting { get; set; } = false;

        //State
        static SpriteFont Font;
        static SpriteFont RestartFont;
        int countdown;

        public CountdownUI()
        {

        }

        public CountdownUI(int thresh, string text)
        {
            Threshold = thresh;
            FinalText = text;
            countdown = Threshold;
            Restarting = true;
        }

        public void Update(int countdown)
        {
            this.countdown = countdown;
        }

        public void Render(SpriteBatch spriteBatch)
        {
            int viewportWidth = TrafficGame.Graphics.Viewport.Width;
            int viewportHeight = TrafficGame.Graphics.Viewport.Height;

            if (countdown > Threshold)
            {
                return;
            }

            string s;

            if (countdown <= 0)
            {
                s = FinalText;
            } else
            {
                s = countdown.ToString();
            }

            var FontToDisplay = (Restarting) ? RestartFont : Font;

            Vector2 d = FontToDisplay.MeasureString(s);

            Vector2 pos = (Restarting) ? new Vector2(viewportWidth / 2 - d.X / 2, (viewportHeight - d.Y) / 2) : new Vector2(viewportWidth / 2 - d.X / 2, viewportHeight - d.Y - 30);

            spriteBatch.DrawString(FontToDisplay, s, pos, Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.DrawString(FontToDisplay, s, pos - new Vector2(4,4) , Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
        }

        public static void LoadContent(ContentManager content)
        {
            Font = content.Load<SpriteFont>(fontName);
            RestartFont = content.Load<SpriteFont>(restartFontName);
        }

    }
}
