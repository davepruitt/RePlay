using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace RePlay_Activity_SpaceRunner.Main
{
    public class Coin
    {
        #region Private Properties

        private Texture2D Texture;
        private float Rotation;
        private int PositionX;
        private int PositionY;
        private SpaceManager space_manager;

        #endregion

        #region Public Properties

        public Guid UniqueID { get; protected set; } = Guid.NewGuid();

        public bool IsAlive { get; set; } = true;
        
        public Rectangle BodyRectangle
        {
            get 
            { 
                return new Rectangle(PositionX - Texture.Width / 2, PositionY - Texture.Height / 2, Texture.Width, Texture.Height); 
            }
        }

        #endregion

        #region Constructor

        public Coin(SpaceManager s, Texture2D texture, int posx, int posy)
        {
            space_manager = s;
            Texture = texture;
            PositionX = posx;
            PositionY = posy;
            Rotation = 0;
        }

        #endregion

        #region Public Methods

        public void Draw(SpriteBatch batch)
        {
            var RotationRadians = (float)(Rotation * Math.PI / 180);
            batch.Draw(Texture, new Vector2(PositionX, PositionY), null, Color.White, RotationRadians, new Vector2(Texture.Width / 2, Texture.Height / 2), 1f, SpriteEffects.None, 0f);
        }

        public void Update(GameTime time, GameState state, bool crashed)
        {
            if (state == GameState.RUNNING && !crashed)
            {
                PositionX -= space_manager.Speed;
            }
        }

        // Coin is offscreen
        public bool IsOffScreen()
        {
            return PositionX + Texture.Width / 2 < 0;
        }

        #endregion

    }
}