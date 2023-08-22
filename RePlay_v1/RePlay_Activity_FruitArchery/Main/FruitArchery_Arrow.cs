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
using Microsoft.Xna.Framework.Content;
using tainicom.Aether.Physics2D.Dynamics.Contacts;
using tainicom.Aether.Physics2D.Dynamics.Joints;

namespace RePlay_Activity_FruitArchery.Main
{
    public class FruitArchery_Arrow : FruitArchery_Sprite
    {
        #region Protected data members

        protected FruitArchery_World game_world;
        protected FixedMouseJoint fixed_mouse_anchor_joint;

        protected Body arrow_body;
        protected Vector2 anchor_position = Vector2.Zero;
        protected Vector2 arrow_body_size_worldunits;
        protected Vector2 arrow_texture_size_screenunits;
        protected float half_arrow_texture_width_screenunits = 0;
        protected float half_arrow_body_width_worldunits = 0;
        
        #endregion

        #region Constructor

        public FruitArchery_Arrow(FruitArchery_World world, Vector2 bow_position, float bow_rotation)
            : base()
        {
            game_world = world;

            texture = FruitArchery_GameSettings.ArrowTexture;
            texture_scale = world.WorldScalingFactor;
            texture_position = bow_position + new Vector2(0.1f, 0.0f);
            texture_rotation = bow_rotation;

            arrow_texture_size_screenunits = new Vector2(FruitArchery_GameSettings.ArrowTexture.Width, FruitArchery_GameSettings.ArrowTexture.Height);
            texture_origin = new Vector2(arrow_texture_size_screenunits.X, arrow_texture_size_screenunits.Y / 2.0f);
            texture_effect = SpriteEffects.FlipHorizontally;
            half_arrow_texture_width_screenunits = arrow_texture_size_screenunits.X / 2.0f;

            arrow_body_size_worldunits = arrow_texture_size_screenunits * texture_scale;
            half_arrow_body_width_worldunits = arrow_body_size_worldunits.X / 2.0f;
            anchor_position = new Vector2(texture_position.X, texture_position.Y);
            
            Vector2 arrow_position = new Vector2(texture_position.X - arrow_body_size_worldunits.X, texture_position.Y);
            arrow_body = world.PhysicsEngineWorld.CreateCircle(0.25f, 1.0f, arrow_position, BodyType.Dynamic);
            arrow_body.IgnoreGravity = true;
            arrow_body.FixedRotation = true;
            arrow_body.Mass = 1.0f;
            arrow_body.SetCollisionGroup((short)FruitArchery_CollisionGroups.ArrowGroup);
        }
        
        public void RemoveBody()
        {
            try
            {
                game_world.PhysicsEngineWorld.Remove(arrow_body);
            }
            catch (Exception)
            {
                //empty
            }
        }

        #endregion

        #region Method overrides

        public override void Update(GameTime gameTime, World _world)
        {
            if (!IsArrowFlying)
            {
                //Calculate "theta" from the angle of the bow
                texture_rotation = FruitArchery_Bow.GetInstance(game_world).RotationRadians;
                
                float hyp = arrow_body_size_worldunits.X;
                float adj = Convert.ToSingle(hyp * Math.Cos(texture_rotation));
                float opp = Convert.ToSingle(hyp * Math.Sin(texture_rotation));
                arrow_body.Position = new Vector2(anchor_position.X - adj, anchor_position.Y - opp);
            }
            else
            {
                //Calculate "theta" from the arrow body's velocity vector
                texture_rotation = Convert.ToSingle(Math.Atan2(-arrow_body.LinearVelocity.Y, -arrow_body.LinearVelocity.X));

                float theta = texture_rotation;
                float hyp = arrow_body_size_worldunits.X;
                float adj = Convert.ToSingle(hyp * Math.Cos(theta));
                float opp = Convert.ToSingle(hyp * Math.Sin(theta));
                texture_position = new Vector2(arrow_body.Position.X + adj, arrow_body.Position.Y + opp);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }

        #endregion

        #region Properties

        public bool IsArrowFlying { get; protected set; } = false;

        public Vector2 Position
        {
            get
            {
                return arrow_body.Position;
            }
        }

        public Vector2 Velocity
        {
            get
            {
                return arrow_body.LinearVelocity;
            }
        }

        #endregion

        #region Public methods

        public void FireArrow ()
        {
            if (!IsArrowFlying)
            {
                float x = Convert.ToSingle(-20.0 * Math.Cos(texture_rotation));
                float y = Convert.ToSingle(-20.0 * Math.Sin(texture_rotation));

                IsArrowFlying = true;
                arrow_body.ApplyLinearImpulse(new Vector2(x, y));
                arrow_body.IgnoreGravity = false;
            }
        }

        public override bool IsOutOfBounds (float left, float right, float bottom)
        {
            return (arrow_body.Position.X >= left || arrow_body.Position.X <= right || arrow_body.Position.Y <= bottom);
        }

        #endregion
    }
}