using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using System.Linq;
using MonoGame.Extended;
using RePlay_VNS_Triggering;
using System.IO;
using RePlay_Common;
using RePlay_Activity_Common;
using RePlay_Exercises;

namespace RePlay_Activity_FruitNinja.Main
{
    public class Knife
    {
        #region Private Properties

        private FruitNinjaGame Fruitninja;
        private Texture2D BladeTexture;
        private float BladeTimer = 0f;
        private VNSAlgorithm_Standard VNS;
        private PCM_Manager PCM;

        #endregion

        #region Public Properties

        public const int InitialDistance = 10;
        public const int MinDistance = 20;

        public List<Vector2> Path { get; }
        public List<DateTime> PathTimes { get; }
        public double KnifeFadeTime { get; set; } = 0.6;
        public int Score { get; set; } = 0;
        public bool IsCutting { get; private set; } = false;
        public bool CheckForCollision { get; private set; } = false;
        public bool CheckCombo { get; set; } = false;
        public int Hits { get; set; } = 0;
        public Color BladeColor = Color.LightSteelBlue;
        public bool TapInsteadOfSwipe { get; set; } = true;
        public Vector2 LastTapLocation { get; private set; }
        public int TotalSwipes { get; private set; } = 0;

        public double CalculatedCutVelocity { get; private set; } = 0;

        #endregion

        #region Constructor

        public Knife(FruitNinjaGame g, PCM_Manager pcm, VNSAlgorithmParameters vns_algorithm_parameters)
        {
            Path = new List<Vector2>();
            PathTimes = new List<DateTime>();
            Fruitninja = g;
            PCM = pcm;
            VNS = new VNSAlgorithm_Standard();
            VNS.Initialize_VNS_Algorithm(DateTime.Now, vns_algorithm_parameters);
            PCM.PropertyChanged += (a, b) =>
            {
                //empty
            };
        }

        #endregion

        #region Public Methods

        public void LoadContent(SpriteBatch batch)
        {
            BladeTexture = new Texture2D(batch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Color[] data = new Color[1 * 1];
            for (int i = 0; i < data.Length; i++) data[i] = Color.White;
            BladeTexture.SetData(data);
        }

        public bool Update(GameTime time, TouchCollection touch_collection, BinaryWriter controller_save_file_handle)
        {
            bool result = false;
            double cut_velocity = 0;
            CalculatedCutVelocity = 0;

            if (touch_collection.Count > 0)
            {
                TouchLocation touch = touch_collection[0];

                // Blade cutting logic
                if (touch.State == TouchLocationState.Pressed && !IsCutting)
                {
                    StartCutting(touch);
                }
                else if (touch.State == TouchLocationState.Moved && IsCutting)
                {
                    UpdateCut(touch, time);
                }
                else if (touch.State == TouchLocationState.Released)
                {
                    StopCutting(touch);
                }

                // Tapping logic to run at the same time
                if (touch.State == TouchLocationState.Pressed && TapInsteadOfSwipe)
                {
                    LastTapLocation = touch.Position;
                }

                // Stim logic
                if (IsCutting && Path.Count > 1)
                {
                    float dist = Vector2.Distance(Path.First(), Path.Last());
                    var delta_time = (PathTimes.Last() - PathTimes.First()).TotalSeconds;
                    cut_velocity = dist / delta_time;
                    CalculatedCutVelocity = cut_velocity;

                    bool stim = VNS.Determine_VNS_Triggering(DateTime.Now, cut_velocity);
                    if (stim)
                    {
                        Fruitninja.GameManager.DisplayStimulationIcon(VNS.Parameters.Enabled, TimeSpan.FromSeconds(2.0));
                        Exercise_SaveData.SaveStimulationTriggerAtCurrentTime(controller_save_file_handle);
                        if (VNS.Parameters.Enabled)
                        {
                            PCM.QuickStim();
                            result = true;
                        }
                    }

                    if (Fruitninja.is_replay_debug_mode)
                    {
                        var game_activity = Game.Activity as RePlay_Game_Activity;
                        if (game_activity != null)
                        {
                            game_activity.vns_signal_chart.AddDataPoint(
                                VNS.Plotting_Get_Latest_Calculated_Value(),
                                VNS.Plotting_Get_VNS_Positive_Threshold(),
                                VNS.Plotting_Get_VNS_Negative_Threshold()
                                );
                        }
                    }
                }
            }

            if (Fruitninja.is_replay_debug_mode)
            {
                var game_activity = Game.Activity as RePlay_Game_Activity;
                if (game_activity != null)
                {
                    game_activity.game_signal_chart.AddDataPoint(cut_velocity);
                }
            }
            
            return result;
        }

        public void Draw(SpriteBatch batch)
        {
            if (IsCutting)
            {
                for (int i = 0; i < Path.Count - 1; i++)
                {
                    var thickness = 2*i + 1 / Path.Count + 1;
                    DrawLine(batch, Path[i], Path[i + 1], thickness);
                }
            }
        }

        public void IncreaseDifficulty()
        {
            if (KnifeFadeTime > .4)
            {
                KnifeFadeTime -= .06;
            }

            TapInsteadOfSwipe = (Fruitninja.Dojo.MaxFruitSpeed <= 150);
        }

        public void DecreaseDifficulty()
        {
            if (KnifeFadeTime < 1.0)
            {
                KnifeFadeTime += .06;
            }

            TapInsteadOfSwipe = (Fruitninja.Dojo.MaxFruitSpeed <= 150);
        }

        // Check point distance
        public float EuclideanDistance(Vector2 p1, Vector2 p2)
        {
            float x2 = (float)Math.Pow(p1.X - p2.X, 2);
            float y2 = (float)Math.Pow(p1.Y - p2.Y, 2);

            return (float)Math.Sqrt(x2 + y2);
        }
        
        public void SaveCurrentKnifeData(BinaryWriter file)
        {
            // IsCutting
            file.Write(IsCutting);
            
            if (IsCutting)
            {
                // Current number of strokes
                file.Write(Path.Count);

                for (int i =0; i < Path.Count; i++)
                {
                    // Datetime of stroke
                    file.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(PathTimes[i]));
                    file.Write(Path[i].X);
                    file.Write(Path[i].Y);
                }
            }
        }

        #endregion

        #region Private Methods

        // On press down
        private void StartCutting(TouchLocation touch)
        {
            IsCutting = true;
            Path.Clear();
            PathTimes.Clear();
            Path.Add(touch.Position);
            PathTimes.Add(DateTime.Now);
            BladeTimer = 0f;
            Hits = 0;
        }

        // On press up
        private void StopCutting(TouchLocation touch)
        {
            IsCutting = false;
            CheckForCollision = false;
            Path.Add(touch.Position);
            PathTimes.Add(DateTime.Now);
            TotalSwipes++;
            CheckCombo = true;
        }

        // On drag event
        private void UpdateCut(TouchLocation touch, GameTime time)
        {
            BladeTimer += (float)time.ElapsedGameTime.TotalSeconds;

            if (BladeTimer >= .10)
            {
                CheckForCollision = true;
            }

            if (BladeTimer >= KnifeFadeTime)
            {
                StopCutting(touch);
            }
            else if (EuclideanDistance(Path.First(), touch.Position) > 1250)
            {
                StopCutting(touch);
            }
            else
            {
                float dx = touch.Position.X - Path.Last().X;
                float dy = touch.Position.Y - Path.Last().Y;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);

                if (len < MinDistance && (Path.Count > 1 || len < InitialDistance)) return;

                Path.Add(touch.Position);
                PathTimes.Add(DateTime.Now);
            }
        }

        // Draw blade
        private void DrawLine(SpriteBatch batch, Vector2 p1, Vector2 p2, int thickness)
        {
            float angle = (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
            float dist = Vector2.Distance(p1, p2);
            var origin = new Vector2(0f, 0.5f);
            var scale = new Vector2(dist, thickness);

            batch.Draw(BladeTexture, p1, null, BladeColor, angle, origin, scale, SpriteEffects.None, 0);
        }

        #endregion

    }
}