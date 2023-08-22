using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RePlay_Exercises;
using Android.Util;
using RePlay_VNS_Triggering;
using RePlay_Exercises.RePlay;
using System.Collections.Generic;
using RePlay_Common;
using System.Linq;
using RePlay_Activity_Common;

namespace RePlay_Activity_Breakout
{
    public class Paddle : GameObject
    {
        #region Private Properties

        private List<double> debounce_list = new List<double>();
        private int debounce_size = 10;

        private ExerciseBase Exercise = null;
        private float countdown = 3f;
        private bool launch = false;
        private VNSAlgorithm_Standard VNS = null;

        #endregion

        #region Public Properties

        public enum PADDLE_WIDTHS
        {
            SIZE_00 = 480,
            SIZE_01 = 410,
            SIZE_02 = 350,
            SIZE_03 = 290,
            SIZE_04 = 250,
            SIZE_05 = 210,
            SIZE_06 = 180,
            SIZE_07 = 150,
            SIZE_08 = 130,
            SIZE_09 = 110
        }
        
        private int min_paddle_width = (int)PADDLE_WIDTHS.SIZE_00;
        public int MaxPaddleWidth = 1200;
        public int PaddleWidth = (int)PADDLE_WIDTHS.SIZE_00;
        public int PaddleHeight = 60;

        private int paddle_border_width = 10;
        private int actual_border_width = 10; 

        private Texture2D black_pixel;
        private Texture2D white_pixel;
        private Texture2D yellow_pixel;

        public double current_data;
        
        public float Speed { get; set; } = 1000;
        public float LongPaddleTimer { get; set; } = 0f;
        public bool IsLongPaddle { get; set; } = false;
        public int LaunchTimer { get; private set; } = 3;
        public float LongPaddleDuration = 10.0f;

        public int ShrinkRate = 1;
        public bool IsPaddleShrinking = false;
        
        #endregion

        #region Constructor

        public Paddle(Game myGame, ExerciseBase e, VNSAlgorithm_Standard vns) :
            base(myGame)
        {
            position = new Vector2(0, 0);
            
            black_pixel = new Texture2D(myGame.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            black_pixel.SetData(new [] { Color.Black });
            white_pixel = new Texture2D(myGame.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            white_pixel.SetData(new[] { Color.White });
            yellow_pixel = new Texture2D(myGame.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            yellow_pixel.SetData(new[] { Color.Yellow });
            
            Exercise = e;
            VNS = vns;
        }

        #endregion

        #region Private Methods
        
        private void Countdown(float time)
        {
            countdown -= time;
            if (countdown <= 0)
            {
                launch = true;
            }

            LaunchTimer = (int)Math.Round(countdown);
        }

        #endregion

        #region Methods

        public void WidenPaddle ()
        {
            PaddleWidth += 100;
            PaddleWidth = Convert.ToInt32(MathHelper.Clamp(PaddleWidth, MinPaddleWidth, MaxPaddleWidth));
            LongPaddleTimer = 0;
            IsLongPaddle = true;
            IsPaddleShrinking = false;
        }

        public void SetPaddlePosition(int xpos, int ypos)
        {
            if (position == null)
            {
                position = new Vector2();
            }

            position.X = xpos;
            position.Y = ypos;
        }

        public void SetPaddleHorizontalPosition (int xpos)
        {
            if (position == null)
            {
                position = new Vector2();
            }

            position.X = xpos;
        }

        #endregion

        #region Properties

        public int MinPaddleWidth
        {
            get
            {
                return min_paddle_width;
            }
            set
            {
                min_paddle_width = value;
                actual_border_width = 
                    Math.Max(2, (MinPaddleWidth * paddle_border_width) / ((int)PADDLE_WIDTHS.SIZE_00));
            }
        }

        public Vector2 PaddleTopCenterPosition
        {
            get
            {
                return new Vector2(position.X, position.Y - PaddleHeight);
            }
        }

        public Rectangle PaddleRectangle
        {
            get
            {
                return new Rectangle(
                    Convert.ToInt32(position.X) - (PaddleWidth / 2), 
                    Convert.ToInt32(position.Y) - PaddleHeight,
                    PaddleWidth, 
                    PaddleHeight);
            }
        }

        #endregion

        #region Overidden properties and methods

        public override float Width
        {
            get
            {
                return PaddleWidth;
            }
        }

        public override float Height
        {
            get
            {
                return PaddleHeight;
            }
        }

        public override void LoadContent()
        {
            //empty
        }
        
        public override bool Update(float deltaTime, BreakoutGame game = null)
        {
            bool result = false;
            
            if (game != null)
            {
                float ScreenWidth = BreakoutGame.VirtualScreenWidth;

                // Paddle movement
                Exercise.Update();
                Exercise.SaveExerciseData();

                current_data = -Exercise.CurrentNormalizedValue;
                if (Exercise.ConvertSignalToVelocity)
                {
                    debounce_list.Add(current_data * 100.0f);
                    debounce_list.LimitTo(debounce_size, true);
                    if (debounce_list.Count == debounce_size)
                    {
                        current_data = TxBDC_Math.Diff(debounce_list).Average();
                    }
                }

                double pos_thresh = 0.1;
                double neg_thresh = pos_thresh * -1;

                bool stim = VNS.Determine_VNS_Triggering(DateTime.Now, current_data);
                if (stim)
                {
                    game.GameManager.DisplayStimulationIcon(VNS.Parameters.Enabled, TimeSpan.FromSeconds(2.0));
                    Exercise_SaveData.SaveStimulationTriggerAtCurrentTime(Exercise.DataSaver);
                    if (VNS.Parameters.Enabled)
                    {
                        game.PCM.QuickStim();
                        result = true;
                    }
                }

                if (game.is_replay_debug_mode)
                {
                    var game_activity = Game.Activity as RePlay_Game_Activity;
                    if (game_activity != null)
                    {
                        game_activity.game_signal_chart.AddDataPoint(current_data);
                        game_activity.vns_signal_chart.AddDataPoint(
                            VNS.Plotting_Get_Latest_Calculated_Value(),
                            VNS.Plotting_Get_VNS_Positive_Threshold(),
                            VNS.Plotting_Get_VNS_Negative_Threshold()
                            );
                    }
                }

                float speed_mulitplier = (float)Math.Abs(current_data);
                Log.Debug("data", current_data.ToString());

                if (Ball.isPaddleBall)
                {
                    Countdown(deltaTime);
                }

                if (launch && !game.startOfLevel && Ball.isPaddleBall)
                {
                    launch = false;
                    countdown = 3f;
                    Ball.isPaddleBall = false;
                }
                else if (current_data < neg_thresh)
                {
                    position.X -= Speed * deltaTime * speed_mulitplier;
                }
                else if (current_data > pos_thresh)
                {
                    position.X += Speed * deltaTime * speed_mulitplier;
                }

                // Clamp Paddle to valid range
                SetPaddleHorizontalPosition(Convert.ToInt32(MathHelper.Clamp(position.X, 
                        PaddleWidth / 2, 
                        ScreenWidth - (PaddleWidth / 2))));

                if (IsPaddleShrinking)
                {
                    PaddleWidth -= ShrinkRate;
                    if (PaddleWidth < MinPaddleWidth)
                    {
                        PaddleWidth = MinPaddleWidth;
                        IsPaddleShrinking = false;
                    }
                }

                if (IsLongPaddle)
                {
                    LongPaddleTimer += deltaTime;

                    if (LongPaddleTimer > LongPaddleDuration)
                    {
                        //PaddleWidth = (int)PADDLE_WIDTHS.NORMAL;
                        LongPaddleTimer = 0;
                        IsLongPaddle = false;
                        IsPaddleShrinking = true;
                    }
                }
            }

            return result;
        }

        public override void Draw(SpriteBatch batch)
        {
            Point drawPosition = new Point(Convert.ToInt32(position.X) - (PaddleWidth / 2),
                Convert.ToInt32(position.Y) - PaddleHeight);
            batch.Draw(black_pixel, new Rectangle(drawPosition.X, drawPosition.Y,
                PaddleWidth, PaddleHeight), Color.White);
            batch.Draw(yellow_pixel, new Rectangle(
                drawPosition.X + actual_border_width,
                drawPosition.Y + actual_border_width,
                PaddleWidth - (actual_border_width * 2),
                PaddleHeight - (actual_border_width * 2)), Color.White);
        }

        #endregion

    }
}
