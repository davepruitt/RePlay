using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace RePlay_Activity_SpaceRunner.Main
{
    public class FlyingObstacle : Obstacle
    {
        #region Private Properties

        private bool TopDown;
        private ObstacleType Type { get; set; }
        private SpaceManager space_manager = null;

        #endregion

        #region Public Properties

        public static List<Texture2D> Textures;

        #endregion

        #region Constructor

        public FlyingObstacle(SpaceManager s, ObstacleType type, int posx, int posy, bool topDown)
        {
            space_manager = s;
            Type = type;
            PositionX = posx;
            PositionY = posy;
            TopDown = topDown;
        }

        public static void LoadContent(ContentManager Content)
        {
            Textures = new List<Texture2D>();

            // index 0
            var boulder = Content.Load<Texture2D>("boulder");
            Textures.Add(boulder);

            // index 1
            var spaceship = Content.Load<Texture2D>("spaceship");
            Textures.Add(spaceship);
        }

        #endregion

        #region Obstacle Overrides

        public override void Draw(SpriteBatch batch)
        {
            var idx = (int)Type;
            batch.Draw(Textures[idx], new Vector2(PositionX, PositionY), null, Color.White, 0f, new Vector2(Textures[idx].Width / 2, Textures[idx].Height / 2), 1f, SpriteEffects.None, 1f);
        }

        public override void Update(GameTime time, GameState state, bool crashed)
        {
            if (state == GameState.RUNNING && !crashed)
            {
                PositionX -= space_manager.Speed;
                if (PositionY <= Textures[(int)Type].Height / 2) TopDown = true;
                else if (PositionY >= SpaceRunnerGame.VirtualScreenHeight - Textures[(int)Type].Height / 2) TopDown = false;
                if (TopDown) PositionY += space_manager.FallingSpeed;
                else PositionY -= space_manager.FallingSpeed;
            }
        }

        // Obstacle is offscreen
        public override bool IsOffScreen()
        {
            var idx = (int)Type;
            return PositionX + Textures[idx].Width / 2 < 0;
        }

        public override Rectangle GetBodyRectangle()
        {
            return new Rectangle(PositionX - Textures[(int)Type].Width / 2, PositionY - Textures[(int)Type].Height / 2, Textures[(int)Type].Width, Textures[(int)Type].Height);
        }

        #endregion

    }
}