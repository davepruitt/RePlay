using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;

namespace RePlay_Activity_Breakout
{
    public class BreakoutInstructions
    {
        private Texture2D breakout_title_texture;
        private Texture2D powerup_fireball;
        private Texture2D powerup_multiball;
        private Texture2D powerup_widepaddle;
        private SpriteFont instructions_font;
        private RenderTarget2D renderTarget;

        public BreakoutInstructions(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
        {
            renderTarget = new RenderTarget2D(graphicsDevice, screenWidth, screenHeight);
        }

        public void LoadContent (ContentManager Content)
        {
            breakout_title_texture = Content.Load<Texture2D>("breakout");
            powerup_fireball = Content.Load<Texture2D>("fireball_powerUp");
            powerup_multiball = Content.Load<Texture2D>("multi_ball_powerUp");
            powerup_widepaddle = Content.Load<Texture2D>("wide_paddle_powerUp");
            instructions_font = Content.Load<SpriteFont>("GameFont");
        }

        public Texture2D DrawBreakoutInstructions (GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, int screenWidth, int screenHeight)
        {
            graphicsDevice.SetRenderTarget(renderTarget);

            //Clear the screen to be some background color
            graphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            
            //Draw the game logo at the top of the screen
            int widthRemaining = screenWidth - breakout_title_texture.Width;
            int halfWidthRemaining = widthRemaining / 2;
            int title_text_xpos = halfWidthRemaining;
            spriteBatch.Draw(breakout_title_texture, new Vector2(halfWidthRemaining, 50), Color.White);

            //Draw some text that says "GET READY!!!"
            string get_ready_text = "GET READY!!! THE GAME IS ABOUT TO START!";
            var get_ready_size = instructions_font.MeasureString(get_ready_text);
            widthRemaining = screenWidth - Convert.ToInt32(get_ready_size.X);
            halfWidthRemaining = widthRemaining / 2;
            spriteBatch.DrawString(instructions_font, get_ready_text, new Vector2(halfWidthRemaining, 500), Color.White);

            //Draw the instructions text
            string instructions_text = "CATCH THESE POWERUPS FOR HELP DURING THE GAME!";
            var instructions_size = instructions_font.MeasureString(instructions_text);
            widthRemaining = screenWidth - Convert.ToInt32(instructions_size.X);
            halfWidthRemaining = widthRemaining / 2;
            spriteBatch.DrawString(instructions_font, instructions_text, new Vector2(halfWidthRemaining, 700), Color.White);

            //Draw each of the powerups
            int screenWidth_4divs = screenWidth / 4;

            int powerup_fireball_xpos = screenWidth_4divs - (powerup_fireball.Width / 2);
            spriteBatch.Draw(powerup_fireball, new Vector2(powerup_fireball_xpos, 900), Color.White);
            string str1 = "FIREBALL";
            var str1_size = instructions_font.MeasureString(str1);
            int str1_xpos = screenWidth_4divs - Convert.ToInt32(str1_size.X / 2);
            spriteBatch.DrawString(instructions_font, str1, new Vector2(str1_xpos, 1000), Color.White);

            int powerup_multiball_xpos = (screenWidth_4divs * 2) - (powerup_multiball.Width / 2);
            spriteBatch.Draw(powerup_multiball, new Vector2(powerup_multiball_xpos, 900), Color.White);
            string str2 = "3 BALLS";
            var str2_size = instructions_font.MeasureString(str2);
            int str2_xpos = (screenWidth_4divs * 2) - Convert.ToInt32(str2_size.X / 2);
            spriteBatch.DrawString(instructions_font, str2, new Vector2(str2_xpos, 1000), Color.White);

            int powerup_widepaddle_xpos = (screenWidth_4divs * 3) - (powerup_widepaddle.Width / 2);
            spriteBatch.Draw(powerup_widepaddle, new Vector2(powerup_widepaddle_xpos, 900), Color.White);
            string str3 = "WIDE PADDLE";
            var str3_size = instructions_font.MeasureString(str3);
            int str3_xpos = (screenWidth_4divs * 3) - Convert.ToInt32(str3_size.X / 2);
            spriteBatch.DrawString(instructions_font, str3, new Vector2(str3_xpos, 1000), Color.White);

            spriteBatch.End();

            graphicsDevice.SetRenderTarget(null);

            return renderTarget;
        }
    }
}