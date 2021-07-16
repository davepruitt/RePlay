using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using RePlay_Activity_Common;
using RePlay_VNS_Triggering;
using System;
using System.Collections.Generic;

namespace RePlay_Activity_SpaceRunner.UI
{
    public class SpaceRunner_GameplayUI : RePlay_Game_GameplayUI
    {
        #region Monogame Content constants

        private const string PausedButton = "pause_button";
        private const string PlayButton = "play_button";
        private const string FontName = "GameFont";
        private const string NotifFontName = "NotifFont";
        private const string Menutexture = "menu";
        private const int HeightToDraw = 60;
        private const int PauseWidth = 800;
        private const int PauseHeight = 600;
        private const int GameOverWidth = 800;
        private const int GameOverHeight = 500;

        #endregion

        #region Private Properties

        private Queue<string> Notifications;
        private SpriteFont Font;
        private SpriteFont NotifFont;
        private Color NotifColor;
        private Texture2D Menu;
        
        private bool DisplayCountdown = false;
        private bool DisplayGameOverScreen = false;
        private float NotifTimer = 0f;
        private float GameOverTimer = 0f;

        private GameTime gameTime;

        #endregion

        #region Public Properties

        public bool DisplayInstructions { get; set; } = false;
        public TitleUI TitleView;

        #endregion

        #region Constructor

        public SpaceRunner_GameplayUI (SpaceRunnerGame game, Viewport v, PCM_Manager pcm, bool show_pcm_status, 
            bool debug_mode, bool is_stimulation_icon_enabled, bool is_stimulation_enabled)
            : base(game, v, pcm, show_pcm_status, debug_mode, true, is_stimulation_icon_enabled, is_stimulation_enabled)
        {
            Notifications = new Queue<string>();
            NotifColor = Color.Goldenrod;
            TitleView = new TitleUI(v.Width, v.Height);
        }

        #endregion

        #region Public Methods
        
        public override bool Update(GameTime time, double secondsRemaining, int score, TouchCollection touchCollection)
        {
            bool result = base.Update(time, secondsRemaining, score, touchCollection);

            gameTime = time;

            SpaceRunnerGame space_runner_game = Game as SpaceRunnerGame;
            if (space_runner_game != null)
            {
                if (space_runner_game.State == GameState.RUNNING || space_runner_game.State == GameState.STARTING)
                {
                    if (Notifications.Count != 0)
                    {
                        NotifTimer += (float)time.ElapsedGameTime.TotalSeconds;

                        if (NotifTimer >= 1.0)
                        {
                            Notifications.Dequeue();
                            NotifTimer = 0f;
                        }
                    }

                    if (time.TotalGameTime.TotalSeconds < 10)
                    {
                        TitleView.Update(time);
                    }

                    if (TimerMinutes == 0 && TimerSeconds <= 10)
                    {
                        DisplayCountdown = true;
                    }
                }
                else if (space_runner_game.State == GameState.GAMEOVER)
                {
                    DisplayGameOverScreen = true;
                    GameOverTimer += (float)time.ElapsedGameTime.TotalSeconds;
                    if (GameOverTimer >= 5.0)
                    {
                        Game.EndGame();
                    }
                }
            }

            return result;
        }

        public override void Render(SpriteBatch spriteBatch, bool highlight_score = false)
        {
            base.Render(spriteBatch);

            int viewportWidth = GameplayViewport.Width;
            int viewportHeight = GameplayViewport.Height;

            if (gameTime.TotalGameTime.TotalSeconds < 10)
            {
                TitleView.Render(spriteBatch, gameTime);
            }
            
            // Display countdown
            if (DisplayCountdown)
            {
                string countdown = TimerSeconds.ToString();
                var c = NotifFont.MeasureString(countdown);
                Vector2 countdownPos = new Vector2((viewportWidth - 300) - c.X / 2, timerPosition.Y + timerDimensions.Y + 50 - c.Y / 2);
                spriteBatch.DrawString(NotifFont, countdown, countdownPos, Color.Red);
            }

            // Display instructions
            if (DisplayInstructions)
            {
                SpaceRunnerGame space_runner_game = Game as SpaceRunnerGame;
                if (space_runner_game != null)
                {
                    string instructions = space_runner_game.GameInput.GetInstructions();
                    var inst = Font.MeasureString(instructions);
                    Vector2 titlePos = new Vector2(viewportWidth / 2 - inst.X / 2, viewportHeight / 2 - 250 - inst.Y / 2);
                    spriteBatch.DrawString(Font, instructions, titlePos, Color.White);
                }
            }

            // Draw Notifs
            if (Notifications.Count != 0)
            {
                RenderNotifications(spriteBatch, scorePosition, scoreDimensions);
            }

            if (DisplayGameOverScreen)
            {
                RenderGameOver(spriteBatch);
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();

            Font = Game.Content.Load<SpriteFont>(FontName);
            NotifFont = Game.Content.Load<SpriteFont>(NotifFontName);
            Menu = Game.Content.Load<Texture2D>(Menutexture);
            TitleView.LoadContent(Game.Content, Font); 
        }

        // Add to notifs queue
        public void AddNotificationString(string prop)
        {
            Notifications.Enqueue(prop);
        }

        #endregion

        #region Private Methods
        
        // Render notifications
        private void RenderNotifications(SpriteBatch batch, Vector2 scorePos, Vector2 scoreSize)
        {
            string current = Notifications.Peek();
            var n = NotifFont.MeasureString(current);
            batch.DrawString(NotifFont, current, new Vector2(50, scorePos.Y + scoreSize.Y + 50 - n.Y / 2), NotifColor);
        }

        // Render game over screen
        private void RenderGameOver(SpriteBatch batch)
        {
            Vector2 GameOverPos = new Vector2(GameplayViewport.Width / 2 - GameOverWidth / 2, GameplayViewport.Height / 2 - GameOverHeight / 2);
            batch.Draw(Menu, new Rectangle((int)GameOverPos.X, (int)GameOverPos.Y, GameOverWidth, GameOverHeight), Color.White);

            string menu1 = "GREAT WORKOUT!";
            var gameOverSize = NotifFont.MeasureString(menu1);
            Vector2 gameOverPos = new Vector2(GameOverPos.X + GameOverWidth / 2 - gameOverSize.X / 2, GameOverPos.Y + gameOverSize.Y);
            batch.DrawString(NotifFont, menu1, gameOverPos, Color.White);

            SpaceRunnerGame space_runner_game = Game as SpaceRunnerGame;
            if (space_runner_game != null)
            {
                string menu2 = "Best score: " + space_runner_game.BestScore + "m";
                var scoreSize = NotifFont.MeasureString(menu2);
                Vector2 scorePos = new Vector2(GameOverPos.X + GameOverWidth / 2 - scoreSize.X / 2, gameOverPos.Y + gameOverSize.Y + scoreSize.Y / 2 + 50);
                batch.DrawString(NotifFont, menu2, new Vector2(scorePos.X, scorePos.Y), Color.White);
            }
        }

        #endregion

    }
}