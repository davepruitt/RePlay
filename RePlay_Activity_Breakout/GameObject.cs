using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RePlay_Activity_Breakout
{
    public class GameObject
    {
        #region Protected Properties

        protected string textureName = "";
        protected Texture2D texture;
        protected Game game;

        #endregion

        #region Public Properties

        public Vector2 position;

        public virtual float Width
        {
            get { return texture.Width; }
        }
        
        public virtual float Height
        {
            get { return texture.Height; }
        }

        public Rectangle BoundingRect
        {
            get
            {
                return new Rectangle((int)(position.X - Width / 2),
                    (int)(position.Y + Height / 2),
                    (int)Width,
                    (int)Height);
            }
        }

        public string TextureName
        {
            get
            {
                return textureName;
            }
            set
            {
                textureName = value;
            }
        }

        #endregion

        #region Constructor

        public GameObject(Game myGame)
        {
            game = myGame;
        }

        #endregion

        #region Virtual Methods

        public virtual void LoadContent()
        {
            if (textureName != "")
            {
                texture = game.Content.Load<Texture2D>(textureName);
            }
        }

        public virtual bool Update(float deltaTime, BreakoutGame game = null)
        {
            return false;
        }

        public virtual void Draw(SpriteBatch batch)
        {
            if (texture != null)
            {
                Vector2 drawPosition = position;
                drawPosition.X -= texture.Width / 2;
                drawPosition.Y -= texture.Height / 2;
                batch.Draw(texture, drawPosition, Color.White);
            }
        }

        #endregion
    }
}