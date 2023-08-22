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
using tainicom.Aether.Physics2D.Dynamics;

using RePlay_Common;
using System.IO;

namespace RePlay_Activity_FruitArchery.Main
{
    public class FruitArchery_World
    {
        #region Private data members

        private World _world;
        private Vector2 _world_dimensions = new Vector2(25.6f, 16.0f);
        private Vector2 _screen_dimensions = Vector2.One;
        private Vector2 _world_scaling_factor = Vector2.One;

        private Vector2 background_texture_size = Vector2.One;
        private Vector2 background_texture_midpoint = Vector2.One;

        private FruitArchery_Bow _player_bow = null;
        private FruitArchery_Arrow _player_arrow = null;
        private FruitArchery_Fruit _piece_of_fruit = null;
        private List<FruitArchery_Fruit> falling_pieces_of_cut_fruit = new List<FruitArchery_Fruit>();
        private Vector2 _last_fruit_position = new Vector2(float.NaN, float.NaN);

        private float out_of_bounds_left = float.NaN;
        private float out_of_bounds_right = float.NaN;
        private float out_of_bounds_bottom = float.NaN;

        #endregion

        #region Properties
        public FruitArchery_Bow GetBow
        {
            get
            {
                return _player_bow;
            }
        }

        public FruitArchery_Arrow GetArrow
        {
            get
            {
                return _player_arrow;
            }
        }

        public FruitArchery_Fruit GetFruit
        {
            get
            {
                return _piece_of_fruit;
            }
        }


        public World PhysicsEngineWorld
        {
            get
            {
                return _world;
            }
        }

        public Vector2 WorldScalingFactor
        {
            get
            {
                return _world_scaling_factor;
            }
        }

        public Vector2 WorldSize
        {
            get
            {
                return _world_dimensions;
            }
        }

        #endregion

        #region Constructor

        public FruitArchery_World (World w, int screen_width, int screen_height)
        {
            InitializeWorld(w, screen_width, screen_height);
        }

        #endregion

        #region Private methods

        private void InitializeWorld(World w, int screen_w, int screen_h)
        {
            //Keep a handle to the physics engine world
            _world = w;
            _screen_dimensions = new Vector2(screen_w, screen_h);
            _world_scaling_factor = _world_dimensions / _screen_dimensions;

            //Set the "out of bounds" numbers
            out_of_bounds_left = (_world_dimensions.X / 2.0f) + 2.0f;
            out_of_bounds_right = -(_world_dimensions.X / 2.0f) - 2.0f;
            out_of_bounds_bottom = -(_world_dimensions.Y / 2.0f) - 2.0f;

            //Calculate some stuff regarding the background texture
            background_texture_size = new Vector2(FruitArchery_GameSettings.BackgroundTexture.Width,
                FruitArchery_GameSettings.BackgroundTexture.Height);
            background_texture_midpoint = new Vector2(FruitArchery_GameSettings.BackgroundTexture.Width / 2.0f,
                FruitArchery_GameSettings.BackgroundTexture.Height / 2.0f);

            //Create the player's bow
            _player_bow = FruitArchery_Bow.GetInstance(this);
        }

        #endregion

        #region Methods

        public bool IsArrowInAir ()
        {
            if (_player_arrow != null)
            {
                return _player_arrow.IsArrowFlying;
            }

            return false;
        }

        public void FireArrow ()
        {
            if (_player_arrow != null)
            {
                _player_bow.FireArrow();
                _player_arrow.FireArrow();
            }
        }
        
        public void UpdateWorld (GameTime t)
        {
            //Step the physics engine
            _world.Step((float)t.ElapsedGameTime.TotalSeconds);

            //If there is not an arrow, create a new arrow
            if (_player_arrow == null)
            {
                _player_arrow = new FruitArchery_Arrow(this, _player_bow.Position, _player_bow.RotationRadians);
                _player_bow.DrawArrow();
            }

            //If there is not a piece of fruit on the screen, create a new piece of fruit
            if (_piece_of_fruit == null)
            {
                if (_player_arrow != null &&  !_player_arrow.IsArrowFlying)
                {
                    var selected_fruit = FruitArchery_GameSettings.GetRandomFruitType();
                    _piece_of_fruit = new FruitArchery_Fruit(selected_fruit, this, _last_fruit_position.X, _last_fruit_position.Y);
                }
            }

            //Update the player's bow based on user input
            _player_bow.Update(t, _world);
            
            //Update the player's arrow
            if (_player_arrow != null)
            {
                _player_arrow.Update(t, _world);
            }
            
            //Update the piece of fruit that is currently in the world
            if (_piece_of_fruit != null)
            {
                _piece_of_fruit.Update(t, _world);
            }
            
            //Check to see if the arrow is out of bounds
            if (_player_arrow.IsOutOfBounds(out_of_bounds_left, out_of_bounds_right, out_of_bounds_bottom))
            {
                _player_arrow.RemoveBody();
                _player_arrow = null;
            }
            
            //Check to see if the fruit is out of bounds
            if (_piece_of_fruit != null &&  (_piece_of_fruit.HasBeenHitByArrow || _piece_of_fruit.IsOutOfBounds(out_of_bounds_left, out_of_bounds_right, out_of_bounds_bottom)))
            {
                _last_fruit_position = _piece_of_fruit.FruitPosition;
                _piece_of_fruit.RemoveBody();
                _piece_of_fruit = null;
            }
        }

        public void DrawWorld (GameTime t, SpriteBatch current_sprite_batch)
        {
            //Draw the background
            current_sprite_batch.Draw(FruitArchery_GameSettings.BackgroundTexture, Vector2.Zero, null, Color.White, 0f,
                background_texture_midpoint, _world_scaling_factor, SpriteEffects.FlipVertically | SpriteEffects.FlipHorizontally, 0.0f);

            //Draw the player's bow
            _player_bow.Draw(current_sprite_batch);

            //Draw the arrow
            if (_player_arrow != null)
            {
                _player_arrow.Draw(current_sprite_batch);
            }

            //Draw the fruit
            if (_piece_of_fruit != null)
            {
                _piece_of_fruit.Draw(current_sprite_batch);
            }
        }
        #endregion
    }
}