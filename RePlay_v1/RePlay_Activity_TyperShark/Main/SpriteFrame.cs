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
    public class SpriteFrame
    {
        #region Constructor

        public SpriteFrame (Texture2D t, Vector2 r)
        {
            Texture = t;
            RotationCenter = r;
        }

        #endregion

        #region Properties

        public Texture2D Texture { get; set; }
        public Vector2 RotationCenter { get; set; } = Vector2.Zero;

        #endregion
    }
}