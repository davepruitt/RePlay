using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Dynamics.Contacts;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

using FitMiAndroid;

using RePlay_Common;

namespace RePlay_Activity_FruitArchery.Main
{
    public class FruitArchery_Bow : FruitArchery_Sprite
    {
        #region Enumeration of actions the bow can take

        public enum BowActions
        {
            HoldSteady,
            DrawArrow,
            FireArrow
        }

        #endregion

        #region Private data members

        protected FruitArchery_World game_world;

        protected int frame_width = 236;
        protected int frame_height = 409;
        protected List<int> frame_locations = new List<int>() { 1, 239, 477, 715, 953 };
        protected int frame_duration_ms_firearrow = 30;
        protected int frame_duration_ms_drawarrow = 30;
        protected int current_frame_index = 0;
        protected BowActions current_action = BowActions.HoldSteady;
        protected Rectangle current_texture_rectangle = Rectangle.Empty;
        protected DateTime current_frame_start = DateTime.Now;
        protected bool is_current_action_completed = false;
        public double baseline_angle = 0;

        protected List<float> rotation_history_x = new List<float>();
        protected List<float> rotation_history_y = new List<float>();
        
        #endregion

        #region Public properties

        public float RotationRadians
        {
            get
            {
                return texture_rotation;
            }
        }

        public Vector2 Position
        {
            get
            {
                return texture_position;
            }
        }

        public double CalculatedPolarCoordinateBeforeGainApplied { get; private set; }

        #endregion

        #region Singleton Constructor

        private static FruitArchery_Bow _instance = null;
        private static object _instance_lock = new object();

        private FruitArchery_Bow(FruitArchery_World world)
           : base()
        {
            game_world = world;

            texture = FruitArchery_GameSettings.BowTexture;
            //texture_origin = new Vector2(texture.Width - 1, texture.Height / 2);
            texture_origin = new Vector2(frame_width - 1, frame_height / 2);
            texture_scale = world.WorldScalingFactor;
            texture_effect = SpriteEffects.FlipHorizontally;
            texture_position = new Vector2(10, -4);

            current_texture_rectangle.X = frame_locations[current_frame_index];
            current_texture_rectangle.Y = 0;
            current_texture_rectangle.Width = frame_width;
            current_texture_rectangle.Height = frame_height;
            current_action = BowActions.DrawArrow;
        }

        public static FruitArchery_Bow GetInstance (FruitArchery_World w)
        {
            if (_instance == null)
            {
                lock (_instance_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new FruitArchery_Bow(w);
                    }
                }
            }

            return _instance;
        }

        #endregion

        #region Overrides

        public override void Update(GameTime gameTime, World world)
        {
            //Determine animation frames
            if (current_action != BowActions.HoldSteady)
            {
                if (current_action == BowActions.DrawArrow)
                {
                    if ((DateTime.Now - current_frame_start).TotalMilliseconds >= frame_duration_ms_drawarrow)
                    {
                        current_frame_index--;

                        if (current_frame_index < 0)
                        {
                            current_frame_index = 0;
                            is_current_action_completed = true;
                        }

                        current_texture_rectangle.X = frame_locations[current_frame_index];
                        current_frame_start = DateTime.Now;
                    }
                }
                else if (current_action == BowActions.FireArrow)
                {
                    if ((DateTime.Now - current_frame_start).TotalMilliseconds >= frame_duration_ms_firearrow)
                    {
                        current_frame_index++;

                        if (current_frame_index >= frame_locations.Count)
                        {
                            current_frame_index = frame_locations.Count - 1;
                            is_current_action_completed = true;
                        }

                        current_texture_rectangle.X = frame_locations[current_frame_index];
                        current_frame_start = DateTime.Now;
                    }
                }
            }

            //Determine angle of rotation
            if (FruitArchery_GameSettings.PuckDongle != null && FruitArchery_GameSettings.PuckDongle.IsPlugged())
            {
                double new_x = FruitArchery_GameSettings.PuckDongle.PuckPack0.Gyrometer[2];
                double new_y = FruitArchery_GameSettings.PuckDongle.PuckPack0.Gyrometer[0];
                
                rotation_history_x.Add((float)new_x);
                rotation_history_y.Add((float)new_y);
                rotation_history_x.LimitTo(10, true);
                rotation_history_y.LimitTo(10, true);

                float avg_x = rotation_history_x.Average();
                float avg_y = rotation_history_y.Average();
                double almost_puck_angle = TxBDC_Math.CartesianToPolar(avg_x, avg_y);
                double puck_angle = almost_puck_angle;
                if (FruitArchery_GameSettings.StimulationExercise == RePlay_Exercises.ExerciseType.FitMi_Supination)
                {
                    puck_angle *= FruitArchery_GameSettings.ExerciseGain;
                }

                CalculatedPolarCoordinateBeforeGainApplied = almost_puck_angle;
                
                double r = MathHelper.ToRadians(-(float)puck_angle);
                float new_texture_rotation = (float)r - (float)baseline_angle;
                texture_rotation = (float)r - (float)baseline_angle;
            }
        }

        public void ResetBaselineBowAngle ()
        {
            double new_x = FruitArchery_GameSettings.PuckDongle.PuckPack0.Gyrometer[2];
            double new_y = FruitArchery_GameSettings.PuckDongle.PuckPack0.Gyrometer[0];
            double puck_angle = RePlay_Common.TxBDC_Math.CartesianToPolar(new_x, new_y);
            if (FruitArchery_GameSettings.StimulationExercise == RePlay_Exercises.ExerciseType.FitMi_Supination)
            {
                puck_angle *= FruitArchery_GameSettings.ExerciseGain;
            }
            
            double r = MathHelper.ToRadians(-(float)puck_angle);
            baseline_angle = r;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, texture_position, current_texture_rectangle, Color.White, texture_rotation, texture_origin, texture_scale, texture_effect, 0);
        }

        #endregion

        #region Public methods

        public void FireArrow ()
        {
            current_action = BowActions.FireArrow;
        }

        public void DrawArrow ()
        {
            current_action = BowActions.DrawArrow;
        }

        public void AimBow (Vector2 world_position)
        {
            float adj = (texture_position.X - world_position.X) / game_world.WorldSize.X;
            float opp = (texture_position.Y - world_position.Y) / game_world.WorldSize.Y;
            float theta = Convert.ToSingle(Math.Atan2(opp, adj));
            texture_rotation = theta;
        }

        public void DestroyBow ()
        {
            _instance = null;
        }

        #endregion
    }
}