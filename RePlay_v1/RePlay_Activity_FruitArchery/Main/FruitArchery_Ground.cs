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
using tainicom.Aether.Physics2D.Dynamics;

namespace RePlay_Activity_FruitArchery.Main
{
    public class FruitArchery_Ground : FruitArchery_Sprite
    {
        #region Protected data members

        protected Body _ground_body;
        protected Vector2 _ground_position = new Vector2(-10f, 10f);
        
        #endregion

        #region Constructor

        public FruitArchery_Ground (World world)
            : base()
        {
            _ground_body = world.CreateRectangle(10, 1, 1, new Vector2(0, -3.1f), 0, BodyType.Static);
            _ground_body.SetCollisionGroup(2);
        }

        #endregion
    }
}