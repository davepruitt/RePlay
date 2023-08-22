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
    class FPSUI
    {
        //Parameters
        const string fontName = "GameFont";

        //State
        static SpriteFont Font;
        int total_frames;
        float total_time;
        int fps;

        public void Update(GameTime gameTime)
        {
            total_time += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            if (total_time >= 1000)
            {
                fps = total_frames;
                total_frames = 0;
                total_time = 0;
            }
        }

        public void Render(SpriteBatch spriteBatch)
        {
            int viewportWidth = TrafficGame.Graphics.Viewport.Width;
            int viewportHeight = TrafficGame.Graphics.Viewport.Height;

            total_frames += 1;
            spriteBatch.DrawString(Font, "FPS: " + fps, new Vector2(viewportWidth - 80, 10), Color.White, 0, Vector2.Zero, 0.25f, SpriteEffects.None, 0);
        }

        public static void LoadContent(ContentManager content)
        {
            Font = content.Load<SpriteFont>(fontName);
        }

    }
}
