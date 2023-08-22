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
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;
using Microsoft.Xna.Framework.Content;
using tainicom.Aether.Physics2D.Dynamics.Contacts;

namespace RePlay_Activity_FruitArchery.Main
{
    public class FruitArchery_Fruit : FruitArchery_Sprite
    {
        #region Protected data members

        protected FruitArchery_World game_world;
        protected Body fruit_body;
        protected float fruit_body_radius;
        protected Vector2 fruit_body_size;
        protected Vector2 fruit_texture_size;
        protected Vector2 original_position = Vector2.One;
        protected bool is_going_up = true;
        protected float vertical_range = 2.0f;

        #endregion

        #region Constructors
        
        public FruitArchery_Fruit(FruitArchery_FruitType t, FruitArchery_World world, float prev_x = float.NaN, float prev_y = float.NaN)
               : base()
        {
            InitializeFruit(t, world, prev_x, prev_y);   
        }
        
        public void RemoveBody ()
        {
            try
            {
                game_world.PhysicsEngineWorld.Remove(fruit_body);
            }
            catch (Exception)
            {
                //empty
            }
        }
        
        #endregion

        #region Properties

        public bool HasBeenHitByArrow { get; protected set; } = false;
        
        public FruitArchery_FruitType FruitType { get; protected set; } = FruitArchery_FruitType.Apple;

        public Vector2 FruitPosition
        {
            get
            {
                return fruit_body.Position;
            }
        }

        public Vector2 FruitTextureSize
        {
            get
            {
                return fruit_texture_size;
            }
        }

        public float FruitTextureRotation
        {
            get
            {
                return texture_rotation;
            }
        }


        #endregion

        #region Protected methods

        protected void InitializeFruit(FruitArchery_FruitType t, FruitArchery_World w, float prev_x = float.NaN, float prev_y = float.NaN)
        {
            FruitType = t;
            game_world = w;
            texture = FruitArchery_GameSettings.FruitTextures[t];
            fruit_texture_size = new Vector2(texture.Width, texture.Height);
            texture_origin = new Vector2(texture.Width / 2, texture.Height / 2);
            texture_rotation = MathHelper.ToRadians(180);
            texture_effect = SpriteEffects.None;
            texture_scale = w.WorldScalingFactor;

            //Determine a position for this new piece of fruit
            //float x_position = FruitArchery_GameSettings.RandomNumberGenerator.Next(-2, 1);
            float x_position = 0;
            float y_position = 0;

            switch (FruitArchery_GameSettings.CurrentStage)
            {
                case FruitArchery_Stage.Stage_01_StaticFruit:

                    bool done = false;
                    while (!done)
                    {
                        x_position = FruitArchery_GameSettings.RandomNumberGenerator.Next(-10, 6);
                        if (x_position <= 0)
                        {
                            y_position = FruitArchery_GameSettings.RandomNumberGenerator.Next(-7, 7);
                        }
                        else
                        {
                            y_position = FruitArchery_GameSettings.RandomNumberGenerator.Next(2, 7);
                        }

                        //The pear is green, and we don't want it to show up on the green background of the grass
                        //near the bottom of the screen
                        if (FruitType == FruitArchery_FruitType.Pear)
                        {
                            y_position = FruitArchery_GameSettings.RandomNumberGenerator.Next(2, 7);
                            done = true;
                            continue;
                        }

                        if (!float.IsNaN(prev_x) && !float.IsNaN(prev_y))
                        {
                            /*var dist = Vector2.Distance(new Vector2(prev_x, prev_y), new Vector2(x_position, y_position));
                            if (dist >= 8)
                            {
                                done = true;
                            }*/

                            var prev_angle = Math.Atan2(prev_y, prev_x);
                            var next_angle = Math.Atan2(y_position, x_position);
                            var angle_difference = Math.Abs(next_angle - prev_angle);
                            if (angle_difference >= (Math.PI / 4.0))
                            {
                                done = true;
                            }
                        }
                        else
                        {
                            done = true;
                        }
                    }
                    
                    break;
                case FruitArchery_Stage.Stage_02_FloatingFruit:
                    y_position = FruitArchery_GameSettings.RandomNumberGenerator.Next(-1, 2);
                    break;
                case FruitArchery_Stage.Stage_03_FallingFruit:
                    y_position = FruitArchery_GameSettings.RandomNumberGenerator.Next(0, 4);
                    break;
            }

            original_position = new Vector2(x_position, y_position);

            //Initialize the body for the physics engine
            fruit_body_size = Vector2.One;
            fruit_body = w.PhysicsEngineWorld.CreateCompoundPolygon(FruitArchery_GameSettings.FruitCollisionPolygons[t], 1.0f);
            fruit_body.SetCollisionGroup(0);
            fruit_body.SetRestitution(0.3f);
            fruit_body.Rotation = MathHelper.ToRadians(180);

            fruit_body.SetIsSensor(true);
            fruit_body.SetCollisionGroup((short)FruitArchery_CollisionGroups.FruitGroup);
            fruit_body.Position = original_position;
            fruit_body.OnCollision += OnPhysicsBodyCollision;

            //Set some properties on the body based upon the game stage
            switch (FruitArchery_GameSettings.CurrentStage)
            {
                case FruitArchery_Stage.Stage_01_StaticFruit:
                    fruit_body.BodyType = BodyType.Static;
                    break;
                case FruitArchery_Stage.Stage_02_FloatingFruit:
                    fruit_body.BodyType = BodyType.Dynamic;
                    fruit_body.IgnoreGravity = true;
                    break;
                case FruitArchery_Stage.Stage_03_FallingFruit:
                    fruit_body.BodyType = BodyType.Dynamic;
                    fruit_body.IgnoreGravity = false;
                    break;
            }
        }
        
        #endregion

        #region Public methods

        public bool OnPhysicsBodyCollision(Fixture a, Fixture b, Contact c)
        {
            if (!HasBeenHitByArrow && (b.CollisionGroup == (short)FruitArchery_CollisionGroups.ArrowGroup))
            {
                FruitArchery_GameSettings.CurrentGameScore++;
                HasBeenHitByArrow = true;
            }
            
            return true;
        }

        #endregion

        #region Overrides

        public override bool IsOutOfBounds(float left, float right, float bottom)
        {
            return (fruit_body.Position.X >= left || fruit_body.Position.X <= right || fruit_body.Position.Y <= bottom);
        }

        public override void Update(GameTime gameTime, World world)
        {
            if (FruitArchery_GameSettings.CurrentStage == FruitArchery_Stage.Stage_02_FloatingFruit)
            {
                if (is_going_up)
                {
                    if (fruit_body.Position.Y >= (original_position.Y + vertical_range))
                    {
                        is_going_up = false;
                    }
                    else
                    {
                        if (fruit_body.LinearVelocity.Y < 1.0f)
                        {
                            fruit_body.ApplyLinearImpulse(new Vector2(0, 0.01f));
                        }
                    }
                }
                else
                {
                    if (fruit_body.Position.Y <= (original_position.Y - vertical_range))
                    {
                        is_going_up = true;
                    }
                    else
                    {
                        if (fruit_body.LinearVelocity.Y > -1.0f)
                        {
                            fruit_body.ApplyLinearImpulse(new Vector2(0, -0.01f));
                        }
                    }
                }
            }
            
            texture_rotation = fruit_body.Rotation;
            texture_position = fruit_body.Position;
        }
        
        #endregion



    }
}