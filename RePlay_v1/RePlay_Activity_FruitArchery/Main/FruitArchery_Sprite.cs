using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace RePlay_Activity_FruitArchery.Main
{
    public class FruitArchery_Sprite : ICloneable
    {
        #region Protected data members

        protected Texture2D texture;
        protected SpriteEffects texture_effect;

        protected float texture_rotation;
        protected Vector2 texture_position;
        protected Vector2 texture_origin;
        protected Vector2 texture_scale;
        
        #endregion

        #region Public properties

        #endregion

        #region Constructors

        public FruitArchery_Sprite()
        {
            //empty
        }

        public FruitArchery_Sprite(Texture2D texture)
        {
            this.texture = texture;
        }

        #endregion

        #region Methods

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion

        #region Virtual methods

        public virtual bool IsOutOfBounds(float left, float right, float bottom)
        {
            return false;
        }

        public virtual void Update(GameTime gameTime, World _world)
        {
            //empty
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, texture_position, null, Color.White, texture_rotation, texture_origin, texture_scale, texture_effect, 0);
        }

        #endregion
    }
}