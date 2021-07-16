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

namespace RePlay_Activity_TyperShark.Main
{
    public class Bubble
    {
        #region Public variables

        public int BubbleVelocity = 0;
        public Vector2 BubblePosition = Vector2.Zero;
        public Texture2D BubbleTexture;

        #endregion

        #region Constructor

        public Bubble (int v, Vector2 p, Texture2D t)
        {
            BubbleVelocity = v;
            BubblePosition = p;
            BubbleTexture = t;
        }

        #endregion

        #region Methods

        public void Update (GameTime gameTime)
        {
            int pixels_to_move = Convert.ToInt32(BubbleVelocity * gameTime.ElapsedGameTime.TotalSeconds);
            BubblePosition.Y += pixels_to_move;
        }

        public void Draw (SpriteBatch s)
        {
            s.Draw(BubbleTexture, BubblePosition, Color.White * 0.7f);
        }

        #endregion
    }
}