using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using tainicom.Aether.Physics2D.Dynamics;
using static RePlay_Activity_SpaceRunner.SpaceRunnerGame;

namespace RePlay_Activity_SpaceRunner.Main
{
    public class Astronaut
    {
        #region Private Properties

        private SpaceRunnerGame Game;
        private List<Texture2D> Textures;
        private int CurrentTexture = 0;
        private float X;
        private float Y;
        private int UpdateFrames = 0;
        private bool Impulse = false;
        private float Velocity = 600;
        private float FallingVelocity = 750;
        private bool Launched = false;

        #endregion

        #region Public Properties

        public int Score { get; set; } = 0;
        public bool Flying { get; set; } = false;
        public bool Crashed { get; set; } = false;
        public Rectangle BodyRectangle
        {
            get { return new Rectangle((int)X - Textures[CurrentTexture].Width / 2, (int)Y - Textures[CurrentTexture].Height / 2, Textures[CurrentTexture].Width, Textures[CurrentTexture].Height); }
        }

        #endregion

        #region Constructor

        public Astronaut(SpaceRunnerGame game)
        {
            Game = game;
        }

        #endregion

        #region Public Methods

        public void LoadContent(ContentManager Content)
        {
            Textures = new List<Texture2D>(new Texture2D[] { Content.Load<Texture2D>("astro-off"), Content.Load<Texture2D>("astro-rocket"), Content.Load<Texture2D>("astro-fall") });
        }

        public void Draw(SpriteBatch batch)
        {
            if (Crashed)
                batch.Draw(Textures[2], new Vector2(X, Y), null, Color.White, 0, new Vector2(Textures[2].Width / 2, Textures[2].Height / 2), 1f, SpriteEffects.None, 0f);
            else if (Flying)
                batch.Draw(Textures[CurrentTexture], new Vector2(X, Y), null, Color.White, 0, new Vector2(Textures[CurrentTexture].Width / 2, Textures[CurrentTexture].Height / 2), 1f, SpriteEffects.None, 0f);

            //var body = BodyRectangle;
            //Texture2D rect = new Texture2D(Game1.GameGraphicsDevice, body.Width, body.Height);
            //Color[] data2 = new Color[body.Width * body.Height];
            //for (int i = 0; i < data2.Length; i++) data2[i] = Color.White;
            //rect.SetData(data2);
            //batch.Draw(rect, new Vector2(X, Y), null, Color.White * 0.5f, 0, new Vector2(body.Width / 2, body.Height / 2), 1f, SpriteEffects.None, 0f);
        }

        public void Update(GameTime time, double data, GameState state, BinaryWriter game_data_file_handle)
        {
            var deltaTime = (float)time.ElapsedGameTime.TotalSeconds;

            if (state == GameState.RUNNING)
            {
                if (!Crashed)
                {
                    Impulse = (data > 0);

                    CurrentTexture = (Impulse) ? 1 : 0;

                    if (Impulse) Y -= Velocity * deltaTime;
                    else Y += FallingVelocity * deltaTime;

                    Y = MathHelper.Clamp(Y, Textures[CurrentTexture].Height / 2, SpaceRunnerGame.VirtualScreenHeight - Textures[CurrentTexture].Height / 2);

                    if (UpdateFrames % 5 == 0) Score++;
                    UpdateFrames++;
                }
                else
                {
                    Y += FallingVelocity * .001f;
                }
            }
            else if (Launched && state == GameState.STARTING)
            {
                if (data > 0)
                {
                    Flying = true;
                    Game.RemoveInstructions();

                    SpaceRunnerSaveGameData.SaveStartOfAttemptData(game_data_file_handle);
                }
            }
        }

        // Reset Astronaut
        public void Reset()
        {
            Y = SpaceRunnerGame.VirtualScreenHeight / 2;
            Crashed = false;
            Score = 0;
            UpdateFrames = 0;
            Velocity = 600;
            FallingVelocity = 750;
            Impulse = false;
        }

        // Astronaut hit the floor
        public bool HitFloor()
        {
            if (Crashed)
            {
                return false;
            }

            return ((Y - Textures[CurrentTexture].Height / 2) >= SpaceRunnerGame.VirtualScreenHeight);
        }

        // Increase difficulty
        public void IncreaseDifficulty()
        {
            if (FallingVelocity < 950) FallingVelocity += 20;
            if (Velocity < 800) Velocity += 20;
        }

        public void LaunchAstronaut(int rockety)
        {
            Launched = true;
            X = SpaceRunnerGame.VirtualScreenWidth / 4;
            Y = rockety;
        }

        #endregion

    }
}