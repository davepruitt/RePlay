using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.IO;

namespace RePlay_Activity_FruitNinja.Main
{
    public class Fruit
    {
        #region Private Properties

        // Graphics
        private FruitNinjaGame Game;
        private Texture2D Texture;
        private Texture2D[] AlternativeTextures = new Texture2D[3];
        private bool Exploding = false;
        private int CurrentExplosionTexture = 0;
        private float ExplosionTimer = 0f;
        private int MaxWidth;
        private int MaxHeight;

        // Physics
        private float Gravity;
        private int Angle;
        private int Speed;
        private float RotationIncrement;
        private float FallingVelocity = 0.5f;
        private float TimeStep = 0.12f;

        // Position
        private int X;
        private int Y;
        private int AbsY = 0;
        private int XOffset;
        private bool RightToLeft;
        private Vector2 HitCoordinates;
        private int ID;

        #endregion

        #region Public Properties

        public double SpeedMultiplier { get; set; } = 0.2;
        public float Time { get; private set; } = 0.0f;
        public bool IsAlive { get; set; } = true;
        public bool IsObstacle { get; set; } = false;
        public float RotationAngle { get; private set; }
        public CircleF HitArea
        {
            get { return new CircleF(new Point2(X, AbsY), 80f); }
        }

        #endregion

        #region Constructors

        public Fruit(Texture2D[] textures, int angle, int speed, float g, bool r, float rotInc, float rotAngle, int startingX, float time, FruitNinjaGame game, int id)
        {
            Texture = textures[0];
            AlternativeTextures[0] = textures[1];
            AlternativeTextures[1] = textures[2];
            AlternativeTextures[2] = textures[3];
            Angle = angle;
            Speed = speed;
            Gravity = g;
            RightToLeft = r;
            RotationIncrement = rotInc;
            RotationAngle = rotAngle;
            XOffset = startingX;
            SpeedMultiplier = .002 * speed - .04;
            TimeStep = time;
            ID = id;

            Game = game;
            MaxWidth = FruitNinjaGame.VirtualScreenWidth;
            MaxHeight = FruitNinjaGame.VirtualScreenHeight;
        }

        public Fruit(Texture2D[] textures, int angle, int speed, float g, bool r, float rotInc, float rotAngle, int startingX, FruitNinjaGame game, bool obs, int id)
        {
            Texture = textures[0];
            AlternativeTextures[0] = textures[1];
            AlternativeTextures[1] = textures[2];
            AlternativeTextures[2] = textures[3];
            Angle = angle;
            Speed = speed;
            Gravity = g;
            RightToLeft = r;
            RotationIncrement = rotInc;
            RotationAngle = rotAngle;
            XOffset = startingX;
            SpeedMultiplier = .002 * speed - .04;
            IsObstacle = true;
            TimeStep = 0.12f;
            ID = id;

            Game = game;
            MaxWidth = FruitNinjaGame.VirtualScreenWidth;
            MaxHeight = FruitNinjaGame.VirtualScreenHeight;
        }

        #endregion

        #region Public Methods

        public void Update(GameTime delta)
        {
            if (IsAlive)
            {
                X = (int)(Speed * Math.Cos(Angle * Math.PI / 180) * Time) + XOffset;
                Y = (int)(Speed * Math.Sin(Angle * Math.PI / 180) * Time - SpeedMultiplier * Gravity * Time * Time);

                if (RightToLeft) X = MaxWidth - Texture.Width - X;
            }
            else
            {
                Y -= (int)(Time * (FallingVelocity + Time * Gravity / 2));
                FallingVelocity += Time;
            }

            AbsY = (Y * -1) + MaxHeight;

            if (Exploding)
            {
                ExplosionTimer += (float)delta.ElapsedGameTime.TotalMilliseconds;

                if(ExplosionTimer >= 80)
                {
                    CurrentExplosionTexture++;
                    ExplosionTimer = 0f;
                }

                if (CurrentExplosionTexture == 2)
                {
                    Exploding = false;
                }
            }

            Time += TimeStep;
        }

        public void Draw(SpriteBatch batch)
        {
            var RotationRadians = (float)(RotationAngle * Math.PI / 180);
            if (IsAlive)
            {
                // Draw Fruit
                batch.Draw(Texture, new Vector2(X, AbsY), null, Color.White, RotationRadians, new Vector2(Texture.Width / 2, Texture.Height / 2), 1f, SpriteEffects.None, 0f);
                if (Game.State == FruitNinjaGame.GameState.RUNNING) RotationAngle += RotationIncrement;

                // Debug View
                if (Game.Debug)
                {
                    Texture2D rect = new Texture2D(Game.GraphicsDevice, Texture.Width, Texture.Height);
                    Color[] data2 = new Color[Texture.Width * Texture.Height];
                    for (int i = 0; i < data2.Length; i++) data2[i] = Color.White;
                    rect.SetData(data2);
                    batch.Draw(rect, new Vector2(X, AbsY), null, Color.White * 0.5f, RotationRadians, new Vector2(Texture.Width / 2, Texture.Height / 2), 1f, SpriteEffects.None, 0f);
                    batch.DrawCircle(HitArea, 36, Color.Red);
                }
            }
            else
            {
                if(!IsObstacle)
                {
                    // Splat texture
                    batch.Draw(AlternativeTextures[2], HitCoordinates, null, Color.White * 0.30f, RotationRadians, 
                        new Vector2(AlternativeTextures[2].Width / 2, AlternativeTextures[2].Height / 2), 1f, SpriteEffects.None, 0f);

                    // Split fruit texture positions
                    Vector2 splitFruit_1, splitFruit_2;
                    if (RotationAngle <= 90)
                    {
                        splitFruit_1 = new Vector2(X - Texture.Width / 2 - 2, AbsY);
                        splitFruit_2 = new Vector2(X + Texture.Width / 2 + 2, AbsY);
                    }
                    else if (RotationAngle <= 180)
                    {
                        splitFruit_1 = new Vector2(X, AbsY - Texture.Height / 2 - 2);
                        splitFruit_2 = new Vector2(X, AbsY + Texture.Height / 2 + 2);
                    }
                    else if (RotationAngle <= 270)
                    {
                        splitFruit_1 = new Vector2(X + Texture.Width / 2 + 2, AbsY);
                        splitFruit_2 = new Vector2(X - Texture.Width / 2 - 2, AbsY);
                    }
                    else
                    {
                        splitFruit_1 = new Vector2(X, AbsY + Texture.Height / 2 + 2);
                        splitFruit_2 = new Vector2(X, AbsY - Texture.Height / 2 - 2);
                    }

                    // Draw split fruit textures
                    batch.Draw(AlternativeTextures[0], splitFruit_1, null, Color.White, RotationRadians, 
                        new Vector2(AlternativeTextures[0].Width / 2, AlternativeTextures[0].Height / 2), 1f, SpriteEffects.None, 0f);
                    batch.Draw(AlternativeTextures[1], splitFruit_2, null, Color.White, RotationRadians, 
                        new Vector2(AlternativeTextures[1].Width / 2, AlternativeTextures[1].Height / 2), 1f, SpriteEffects.None, 0f);
                }
                else
                {
                    // Bomb explosion logic
                    batch.Draw(AlternativeTextures[CurrentExplosionTexture], HitCoordinates, null, Color.White * 0.5f, RotationRadians,
                        new Vector2(AlternativeTextures[CurrentExplosionTexture].Width / 2, AlternativeTextures[CurrentExplosionTexture].Height / 2), 2f, SpriteEffects.None, 0f);
                }
            }
        }

        // Check if fruit is off screen
        public bool IsOffScreen()
        {
            return Y < 0 || X + Texture.Width < 0 || X > MaxWidth;
        }

        // Slice fruit
        public void Slice()
        {
            Gravity /= 8.0f;
            Time = 0.0f;
            IsAlive = false;
            HitCoordinates = new Vector2(X, AbsY);
        }

        // Explode bomb
        public void Explode()
        {
            Time = 0.0f;
            IsAlive = false;
            HitCoordinates = new Vector2(X, AbsY);
            Exploding = true;
        }

        public void SaveFruitData(BinaryWriter file)
        {
            // Write out fruit id
            file.Write(ID);

            // Fruit properties
            file.Write(Speed);
            file.Write(Gravity);
            file.Write(X);
            file.Write(AbsY);
            file.Write(IsAlive);
            file.Write(IsObstacle);
            file.Write(Time);
        }

        #endregion

    }
}