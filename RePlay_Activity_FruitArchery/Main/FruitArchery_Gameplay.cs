using Android.OS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;

namespace RePlay_Activity_FruitArchery.Main
{
    public class FruitArchery_Gameplay
    {
        private const string PausedButton = "pause_button";
        private const string PlayButton = "play_button";
        private const string FontName = "GameFont";
        private const int HeightToDraw = 80;
        private const int PauseWidth = 800;
        private const int PauseHeight = 500;

        private FruitArcheryGame Game;
        private Viewport GameplayViewport;
        private int Score;
        private int TimerMinutes;
        private double TimerSeconds;
        private SpriteFont Font;

        private Texture2D ButtonTexture;
        private Texture2D PausedButtonTexture;
        private Rectangle ButtonRectangle;

        private Rectangle quitRect;
        private Rectangle resumeRect;
        private Rectangle nextRect;

        public bool Paused { get; set; } = false;

        public bool display_stimulation = false;
        public DateTime display_stimulation_start_time = DateTime.MinValue;
        public TimeSpan display_stimulation_duration = TimeSpan.FromSeconds(1.0);
        public bool debug = false;

        public FruitArchery_Gameplay (FruitArcheryGame game, Viewport v)
        {
            Game = game;
            GameplayViewport = v;
        }

        public void DisplayStimulation()
        {
            display_stimulation = true;
            display_stimulation_start_time = DateTime.Now;
        }

        public void Update(GameTime time, int score)
        {
            if (display_stimulation)
            {
                if (DateTime.Now >= (display_stimulation_start_time + display_stimulation_duration))
                {
                    display_stimulation = false;
                }
            }

            if (Paused) ListenForMenuClicks();
            else
            {
                TimeSpan ts = TimeSpan.FromSeconds(FruitArchery_GameSettings.TimeRemainingInSeconds);
                if (FruitArchery_GameSettings.TimeRemainingInSeconds < 0)
                {
                    ts = TimeSpan.FromSeconds(0);
                }

                TimerMinutes = ts.Minutes;
                TimerSeconds = ts.Seconds;
                Score = score;
                CheckIfPaused();
            }
        }

        public void Render(SpriteBatch spriteBatch)
        {
            int viewportWidth = GameplayViewport.Width;
            int viewportHeight = GameplayViewport.Height;

            // Draw Score
            string scoreString = String.Format("Score: {0:#,###0}", Score);
            var s = Font.MeasureString(scoreString);
            spriteBatch.DrawString(Font, scoreString, new Vector2(50, HeightToDraw - s.Y / 2), Color.White);

            // Draw Timer
            string timeString = (TimerMinutes < 0) ? "Game Over" : "Time: " + String.Format("{0}:{1}", (int)TimerMinutes, TimerSeconds.ToString("00"));
            var d = Font.MeasureString(timeString);
            Vector2 timePos = new Vector2((viewportWidth - 300) - d.X / 2, HeightToDraw - d.Y / 2);
            spriteBatch.DrawString(Font, timeString, timePos, Color.White);

            // Draw Pause Button
            var buttonX = timePos.X + d.X + ButtonTexture.Width / 2 + 25;
            ButtonRectangle = new Rectangle((int)buttonX - ButtonTexture.Width / 2, HeightToDraw - ButtonTexture.Height / 2, ButtonTexture.Width, ButtonTexture.Height);
            if (Paused)
            {
                RenderPauseMenu(spriteBatch);
                spriteBatch.Draw(PausedButtonTexture, ButtonRectangle, Color.White);
            }
            else spriteBatch.Draw(ButtonTexture, ButtonRectangle, Color.White);
        }

        public void LoadContent()
        {
            Font = Game.Content.Load<SpriteFont>(FontName);
            ButtonTexture = Game.Content.Load<Texture2D>(PausedButton);
            PausedButtonTexture = Game.Content.Load<Texture2D>(PlayButton);
            CreatePauseButton();
        }

        public void CreatePauseButton()
        {
            var buttonPosition = new Vector2(GameplayViewport.Width - 200, HeightToDraw);
            ButtonRectangle = new Rectangle((int)buttonPosition.X, (int)buttonPosition.Y, ButtonTexture.Width, ButtonTexture.Height);
        }

        public void RenderPauseMenu(SpriteBatch spriteBatch)
        {
            // First let's create a big white rectangle over the screen
            Vector2 PauseRectanglePosition = new Vector2(GameplayViewport.Width / 2 - PauseWidth / 2, GameplayViewport.Height / 2 - PauseHeight / 2);
            Rectangle PauseRectangle = new Rectangle((int)PauseRectanglePosition.X, (int)PauseRectanglePosition.Y, PauseWidth, PauseHeight);

            Texture2D rect = new Texture2D(Game.GraphicsDevice, PauseWidth, PauseHeight);
            Color[] data = new Color[PauseWidth * PauseHeight];
            for (int i = 0; i < data.Length; i++) data[i] = Color.White;

            rect.SetData(data);

            spriteBatch.Draw(rect, PauseRectanglePosition, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);

            string menu1 = "GAME PAUSED";
            var startSize = Font.MeasureString(menu1);
            Vector2 pausedPos = new Vector2(PauseRectanglePosition.X + PauseWidth / 2 - startSize.X / 2, PauseRectanglePosition.Y + startSize.Y);
            spriteBatch.DrawString(Font, menu1, pausedPos, Color.Black);

            string menu2 = "Resume";
            var resumeSize = Font.MeasureString(menu2);
            Vector2 resumePos = new Vector2(PauseRectanglePosition.X + PauseWidth / 2 - resumeSize.X / 2, pausedPos.Y + startSize.Y + resumeSize.Y / 2 + 25);
            resumeRect = new Rectangle((int)resumePos.X, (int)resumePos.Y, (int)resumeSize.X, (int)resumeSize.Y);
            spriteBatch.DrawString(Font, menu2, new Vector2(resumePos.X, resumePos.Y), Color.Black);

            string menu3 = "Quit";
            var quitSize = Font.MeasureString(menu3);
            Vector2 quitPos = new Vector2(PauseRectanglePosition.X + PauseWidth / 2 - quitSize.X / 2, resumePos.Y + resumeSize.Y + quitSize.Y / 2 + 25);
            quitRect = new Rectangle((int)quitPos.X, (int)quitPos.Y, (int)quitSize.X, (int)quitSize.Y);
            spriteBatch.DrawString(Font, menu3, new Vector2(quitPos.X, quitPos.Y), Color.Black);
        }

        public void ListenForMenuClicks()
        {
            TouchCollection touch = TouchPanel.GetState();

            if (touch.Count > 0 && Paused && resumeRect != null && quitRect != null)
            {
                Rectangle touchRect = new Rectangle((int)touch[0].Position.X, (int)touch[0].Position.Y, 1, 1);

                if (touchRect.Intersects(resumeRect) && touch[0].State == TouchLocationState.Pressed)
                {
                    Paused = false;
                }
                else if (touchRect.Intersects(quitRect) && touch[0].State == TouchLocationState.Pressed)
                {
                    Game.EndGame();
                }
            }
        }

        public void CheckIfPaused()
        {
            TouchCollection touch = TouchPanel.GetState();

            if (touch.Count > 0 && !Paused)
            {
                Rectangle touchRect = new Rectangle((int)touch[0].Position.X, (int)touch[0].Position.Y, 1, 1);

                if (touchRect.Intersects(ButtonRectangle) && touch[0].State == TouchLocationState.Pressed)
                {
                    Paused = true;
                }
            }
        }
    }
}