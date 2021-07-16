using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using RePlay_Activity_Common;
using RePlay_VNS_Triggering;
using System;
using System.Collections.Generic;

namespace RePlay_Activity_FruitNinja.UI
{
    public class Gameplay : RePlay_Game_GameplayUI
    {
        #region Monogame Content constants
        
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
        private Random Randomizer;
        private List<Color> Colors;
        private SpriteFont NotifFont;
        private Color NotifColor;
        private Texture2D Menu;
        
        private bool DisplayCountdown = false;
        private bool DisplayGameOverScreen = false;
        private bool DisplayCombo = false;
        private float NotifTimer = 0f;
        private float GameOverTimer = 0f;
        private float DisplayComboTimer = 0f;
        private int ComboCount = 0;

        private GameTime gameTime;

        #endregion

        #region Public Properties

        public TitleUI TitleView;

        #endregion

        #region Constructor

        public Gameplay(FruitNinjaGame game, Viewport v, PCM_Manager pcm, bool show_pcm_status, 
            bool debug_mode, bool stim_icon_enabled, bool stim_enabled)
            : base(game, v, pcm, show_pcm_status, debug_mode, true, stim_icon_enabled, stim_enabled)
        {
            Game = game;
            GameplayViewport = v;
            Notifications = new Queue<string>();
            Randomizer = new Random();
            Colors = new List<Color>();
            TitleView = new TitleUI(v.Width, v.Height);
        }

        #endregion

        #region Public Methods

        public override bool Update(GameTime time, double secondsRemaining, int score, TouchCollection touchCollection)
        {
            bool result = base.Update(time, secondsRemaining, score, touchCollection);

            gameTime = time;

            FruitNinjaGame fruitNinjaGame = Game as FruitNinjaGame;
            if (fruitNinjaGame.State == FruitNinjaGame.GameState.RUNNING)
            {
                if (Notifications.Count != 0)
                {
                    NotifTimer += (float)time.ElapsedGameTime.TotalSeconds;

                    if (NotifTimer >= 0.8)
                    {
                        Notifications.Dequeue();
                        NotifTimer = 0f;
                        NotifColor = Colors[Randomizer.Next(0, Colors.Count)];
                    }
                }

                if (TimerMinutes == 0 && TimerSeconds <= 10)
                {
                    DisplayCountdown = true;
                }

                if (DisplayCombo)
                {
                    DisplayComboTimer += (float)time.ElapsedGameTime.TotalSeconds;
                    if (DisplayComboTimer >= 0.8)
                    {
                        DisplayComboTimer = 0f;
                        DisplayCombo = false;
                    }
                }
                if (time.TotalGameTime.TotalSeconds < 10)
                {
                    TitleView.Update(time);
                }
            }
            else if (fruitNinjaGame.State == FruitNinjaGame.GameState.GAMEOVER)
            {
                Score = score;
                DisplayGameOverScreen = true;
                GameOverTimer += (float)time.ElapsedGameTime.TotalSeconds;
                if (GameOverTimer >= 5.0)
                {
                    Game.EndGame();
                }
            }

            return result;
        }

        public override void Render(SpriteBatch spriteBatch, bool highlight_score = false)
        {
            base.Render(spriteBatch, true);

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

            // Draw Notifs
            if (Notifications.Count != 0)
            {
                RenderNotifications(spriteBatch);
            }

            if (DisplayGameOverScreen)
            {
                RenderGameOver(spriteBatch);
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();

            NotifFont = Game.Content.Load<SpriteFont>(NotifFontName);
            Menu = Game.Content.Load<Texture2D>(Menutexture);
            TitleView.LoadContent(Game.Content, Font);
            CreateColorsList();
        }

        // Add to notifs queue
        public void AddNotificationString(string prop)
        {
            Notifications.Enqueue(prop);
        }

        // Comboooooo
        public void DisplayComboImage(int count)
        {
            DisplayCombo = true;
            ComboCount = count;
            string combo = "COMBO " + ComboCount + "x " + " +" + ComboCount * 50;
            AddMessage(combo);
        }

        #endregion

        #region Private Methods
        
        // Render notifications
        private void RenderNotifications(SpriteBatch batch)
        {
            string current = Notifications.Peek();
            var s = NotifFont.MeasureString(current);
            if(current == "Kaboom!" || current == "Missed")
                batch.DrawString(NotifFont, current, new Vector2(GameplayViewport.Width / 2 - s.X / 2, HeightToDraw - s.Y / 2), Color.DarkRed);
            else
                batch.DrawString(NotifFont, current, new Vector2(GameplayViewport.Width / 2 - s.X / 2, HeightToDraw - s.Y / 2), NotifColor);
        }

        // Render game over screen
        private void RenderGameOver(SpriteBatch batch)
        {
            Vector2 GameOverPos = new Vector2(GameplayViewport.Width / 2 - GameOverWidth / 2, GameplayViewport.Height / 2 - GameOverHeight / 2);
            batch.Draw(Menu, new Rectangle((int)GameOverPos.X, (int)GameOverPos.Y, GameOverWidth, GameOverHeight), Color.White);

            string menu1 = "GREAT WORKOUT!";
            var gameOverSize = NotifFont.MeasureString(menu1);
            Vector2 gameOverPos = new Vector2(GameOverPos.X + GameOverWidth / 2 - gameOverSize.X / 2, GameOverPos.Y + gameOverSize.Y);
            batch.DrawString(NotifFont, menu1, gameOverPos, Color.Black);

            string menu2 = "Final score: " + Score;
            var scoreSize = NotifFont.MeasureString(menu2);
            Vector2 scorePos = new Vector2(GameOverPos.X + GameOverWidth / 2 - scoreSize.X / 2, gameOverPos.Y + gameOverSize.Y + scoreSize.Y / 2 + 50);
            batch.DrawString(NotifFont, menu2, new Vector2(scorePos.X, scorePos.Y), Color.Black);
        }

        // Colors and stuff
        private void CreateColorsList()
        {
            Colors.Add(Color.Yellow);
            Colors.Add(Color.RoyalBlue);
            Colors.Add(Color.Green);
            Colors.Add(Color.Orange);
            Colors.Add(Color.Purple);
            Colors.Add(Color.IndianRed);
            Colors.Add(Color.Gray);
        }

        #endregion

    }
}