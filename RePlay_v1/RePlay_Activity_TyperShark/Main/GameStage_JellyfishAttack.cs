using System;
using System.Collections.Generic;
using System.IO;
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

using RePlay_Common;

namespace RePlay_Activity_TyperShark.Main
{
    public class GameStage_JellyfishAttack : GameStage
    {
        #region Private data members

        private string jellyfish_stage_instructions = "Press space bar to eliminate the jellyfish!";
        private Vector2 jellyfish_stage_instructions_size = Vector2.One;
        private Vector2 jellyfish_stage_instructions_position = Vector2.One;
        private TimeSpan jellyfish_stage_instructions_duration = TimeSpan.FromSeconds(4.0);

        private int minimum_jellyfish_speed = 200;
        private int maximum_jellyfish_speed = 400;
        private double jellyfish_frequency_mean = 2;
        private double jellyfish_frequency_std = 0.25;
        private double jellyfish_accumulator = 0;
        private double current_jellyfish_multiplier = 0.1;

        private int jellyfish_x_spread = 0;
        private int jellyfish_y_start = 0;
        private int jellyfish_score = 10;

        private List<Shark> jellyfish = new List<Shark>();

        private DateTime stage_start_time = DateTime.MinValue;
        private TimeSpan stage_duration = TimeSpan.FromSeconds(1);

        #endregion

        #region Constructor

        public GameStage_JellyfishAttack(GraphicsDevice graphicsDevice, SpriteFont f, TimeSpan duration, double frequency = 2)
            : base(graphicsDevice, f, 0, 0, 0, 0, 0, 0, SharkType.Jellyfish, StageType.Jellyfish_Attack, false)
        {
            jellyfish_stage_instructions_size = f.MeasureString(jellyfish_stage_instructions);
            jellyfish_stage_instructions_position = new Vector2(GameConfiguration.VirtualScreenHalfWidth - (jellyfish_stage_instructions_size.X / 2),
                                                                GameConfiguration.VirtualScreenHalfHeight - (jellyfish_stage_instructions_size.Y / 2));
            
            jellyfish_frequency_mean = frequency;
            stage_duration = duration;
            jellyfish_x_spread = Convert.ToInt32(GameConfiguration.VirtualScreenHalfWidth);
            jellyfish_y_start = Convert.ToInt32(GameConfiguration.VirtualScreenHeight);
            current_jellyfish_multiplier = RePlay_Common.RandomNumberStatic.RandomNumbers.NextGaussian(jellyfish_frequency_mean, jellyfish_frequency_std);
            if (current_jellyfish_multiplier < 0.1)
            {
                current_jellyfish_multiplier = 0.1;
            }
        }

        #endregion
        
        #region Properties

        public bool IsActive { get; set; } = true;

        public override bool IsStageCompleted
        {
            get
            {
                if (stage_start_time == DateTime.MinValue)
                {
                    return false;
                }
                else
                {
                    return (DateTime.Now >= (stage_start_time + stage_duration) && !IsActive && jellyfish.Count == 0);
                }
            }
        }

        #endregion

        #region Methods

        public override void ResetStage()
        {
            stage_start_time = DateTime.Now;
        }

        public override void HandleKeyboardInput(Keys key, char keypress)
        {
            if (key == Keys.Space)
            {
                if (jellyfish.Count > 0)
                {
                    var jellyfish_to_kill = jellyfish.Where(x => x.IsActive).ToList();
                    if (jellyfish_to_kill.Count > 0)
                    {
                        var idx_of_min = jellyfish_to_kill.Select(x => x.PositionY).ToList().IndexOfMin();
                        jellyfish_to_kill[idx_of_min].ZapShark();

                        Vector2 score_position = new Vector2(jellyfish_to_kill[idx_of_min].PositionX, jellyfish_to_kill[idx_of_min].PositionY);
                        FloatingScore new_score = new FloatingScore(shark_font, score_position, jellyfish_score);
                        FloatingScores.Add(new_score);
                        GameConfiguration.CurrentScore += jellyfish_score;
                        NotifyPropertyChanged("WordCompleted");
                    }
                }
            }
        }

        public override void Update(GameTime gameTime, bool zap)
        {
            if (stage_start_time == DateTime.MinValue)
            {
                stage_start_time = DateTime.Now;
            }
            else if (DateTime.Now >= (stage_start_time + stage_duration))
            {
                IsActive = false;
            }

            if (zap)
            {
                //Update the score upon zapping
                var active_sharks_list = jellyfish.Where(x => x.IsAlive).ToList();
                int score_per_shark = GameConfiguration.ScorePerWord;
                var total_score_change = score_per_shark * active_sharks_list.Count;
                foreach (Shark s in active_sharks_list)
                {
                    //Create a "floating score" object that will be displayed near the fish on the screen for a little bit
                    FloatingScore new_floating_score = new FloatingScore(shark_font, new Vector2(s.PositionX, s.PositionY), score_per_shark);
                    FloatingScores.Add(new_floating_score);
                }

                GameConfiguration.CurrentScore += total_score_change;

                jellyfish.ForEach(x => x.ZapShark());
            }

            if (IsActive)
            {
                double jellyfish_to_generate = current_jellyfish_multiplier * gameTime.ElapsedGameTime.TotalSeconds;
                jellyfish_accumulator += jellyfish_to_generate;

                //Create some new jellyfish
                if (jellyfish_accumulator >= 1.0)
                {
                    int num_jellyfish = Convert.ToInt32(Math.Floor(jellyfish_accumulator));
                    for (int i = 0; i < num_jellyfish; i++)
                    {
                        int vel = -RePlay_Common.RandomNumberStatic.RandomNumbers.Next(minimum_jellyfish_speed, maximum_jellyfish_speed);
                        int xpos = Convert.ToInt32(GameConfiguration.VirtualScreenHalfWidth) +
                            (RePlay_Common.RandomNumberStatic.RandomNumbers.Next(0, jellyfish_x_spread * 2) - jellyfish_x_spread);
                        int ypos = jellyfish_y_start;

                        Shark new_jellyfish = new Shark(shark_font, string.Empty, graphics_device)
                        {
                            VelocityX = 0,
                            VelocityY = vel,
                            PositionX = xpos,
                            PositionY = ypos,
                            UseDestinationY = true,
                            DestinationY = GameConfiguration.VirtualScreenHeight - 100,
                            SharkType = SharkType.Jellyfish
                        };

                        jellyfish.Add(new_jellyfish);
                    }
                    
                    //Reset the jellyfish accumulator
                    jellyfish_accumulator -= num_jellyfish;
                }
            }

            //Remove scores that have been up for more than 2 seconds
            FloatingScores.RemoveAll(x => DateTime.Now >= (x.ScorePostedTime + TimeSpan.FromSeconds(2.0)));

            //Update the position of each floating score in the stage
            FloatingScores.Where(x => !x.IsOutOfBounds).ToList().ForEach(x => x.Update(gameTime));

            //Update all bubbles
            jellyfish.ForEach(x => x.UpdateShark(gameTime));

            //Remove all bubbles that have passed beyond the top of the screen
            jellyfish.RemoveAll(x => x.IsOutOfBounds);
        }

        public override void DrawStage(SpriteBatch spriteBatch)
        {
            if (DateTime.Now <= (stage_start_time + jellyfish_stage_instructions_duration))
            {
                spriteBatch.DrawString(shark_font, jellyfish_stage_instructions, jellyfish_stage_instructions_position, Color.White);
            }

            FloatingScores.Where(x => !x.IsOutOfBounds).ToList().ForEach(x => x.Draw(spriteBatch));
            jellyfish.ForEach(x => x.DrawShark(spriteBatch, stage_type));
        }

        public override void SaveStageState(BinaryWriter fid, List<Keys> released_keys)
        {
            Sharks = jellyfish;
            base.SaveStageState(fid, released_keys);
        }

        #endregion
    }
}