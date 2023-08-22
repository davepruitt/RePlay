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
using RePlay_Activity_Common;
using RePlay_Common;
using RePlay_Exercises;

namespace RePlay_Activity_TyperShark.Main
{
    public class GameLevel
    {
        #region Private data members

        private GraphicsDevice game_graphics_device;
        private SpriteFont regular_font;
        private SpriteFont large_font;
        private GameBackground.BackgroundType level_background_type = GameBackground.BackgroundType.ClearDay;
        private GameBackground level_background;

        private List<GameStage> level_stages = new List<GameStage>();
        private int current_stage_index = 0;

        private string get_ready_string = "GET READY!";
        private int get_ready_string_width = 0;
        private int get_ready_string_height = 0;
        private int get_ready_string_half_width = 0;
        private int get_ready_string_half_height = 0;

        private bool has_level_begun = false;
        private bool has_descent_begun = false;
        private DateTime level_start_time = DateTime.MinValue;
        private DateTime descent_start_time = DateTime.MinValue;
        private TimeSpan start_descent_delay = TimeSpan.FromSeconds(3.0);
        private TimeSpan first_stage_delay = TimeSpan.FromSeconds(3.0);
        private SharkZapper shark_zapper;
        private GameLevelCompletionType level_completion_type = GameLevelCompletionType.Forever_RepeatLastStage;

        private bool waiting = false;
        private GameStage_ShipwreckBonus shipwreck_bonus_stage;

        private bool use_dynamic_stage_creation = false;
        private double level_difficulty_mean = 0;
        private double level_difficulty_stddev = 0;
        private bool times_almost_up = false;
        private bool prevent_jellyfish = false;

        private ExerciseType exercise_type = ExerciseType.Keyboard_Typing;
        
        #endregion

        #region Constructor

        public GameLevel (GameBackground background, SharkZapper zapper, GraphicsDevice graphicsDevice, SpriteFont f1, SpriteFont f2, GameLevelCompletionType completion_type, 
            bool is_level_dynamic, double difficulty_mean, double difficulty_stddev, ExerciseType exerciseType)
        {
            exercise_type = exerciseType;

            prevent_jellyfish = !GameConfiguration.AllowJellyfish;
            game_graphics_device = graphicsDevice;
            regular_font = f1;
            large_font = f2;

            use_dynamic_stage_creation = is_level_dynamic;
            level_difficulty_mean = difficulty_mean;
            level_difficulty_stddev = difficulty_stddev;

            if (level_difficulty_mean <= 0.1)
            {
                level_background_type = GameBackground.BackgroundType.ClearDay;
            }
            else if (level_difficulty_mean <= 0.2)
            {
                level_background_type = GameBackground.BackgroundType.Iceberg;
            }
            else if (level_difficulty_mean <= 0.3)
            {
                level_background_type = GameBackground.BackgroundType.Lighthouse;
            }
            else if (level_difficulty_mean <= 0.4)
            {
                level_background_type = GameBackground.BackgroundType.Shipwreck;
            }
            else if (level_difficulty_mean <= 0.5)
            {
                level_background_type = GameBackground.BackgroundType.Storm;
            }
            else
            {
                level_background_type = GameBackground.BackgroundType.Volcano;
            }
            
            level_background = background;
            shark_zapper = zapper;
            level_completion_type = completion_type;
            shipwreck_bonus_stage = new GameStage_ShipwreckBonus(graphicsDevice, f1, f2);
        }

        #endregion

        #region Properties

        public bool IsLevelCompleted { get; private set; } = false;

        public GameLevelCompletionType LevelCompletionType
        {
            get
            {
                return level_completion_type;
            }
        }

        #endregion

        #region Methods

        public void SignalTimesAlmostUp ()
        {
            times_almost_up = true;
        }

        public void SignalTimesUp ()
        {
            IsLevelCompleted = true;
        }

        public void SignalPreventJellyfish ()
        {
            prevent_jellyfish = true;
        }

        public void SaveCurrentLevelState (BinaryWriter fid, List<Keys> released_keys)
        {
            if (current_stage_index < level_stages.Count && has_level_begun && has_descent_begun)
            {
                //Write out a true value indicating that this block does contain information
                fid.Write(true);

                //Save a timestamp
                fid.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                //Save the index of the current stage
                fid.Write(current_stage_index);

                //Save the current stage
                level_stages[current_stage_index].SaveStageState(fid, released_keys);

                //Save whether a stimulation occurred or not
                fid.Write(TyperSharkSaveGameData.DidStimulationOccurFlag);

                //Reset the "save stim flag" for the next frame
                TyperSharkSaveGameData.DidStimulationOccurFlag = false;
            }
            else
            {
                //Write out a false value indicating that no further information is contained in this block.
                fid.Write(false);
            }
        }

        public void SetLevelBackgroundType (GameBackground.BackgroundType background_type)
        {
            level_background_type = background_type;
        }

        public void AddStage (GameStage stage)
        {
            //Stages can only be added to a level that has not yet begun
            if (!has_level_begun && !use_dynamic_stage_creation)
            {
                level_stages.Add(stage);
            }
        }

        public void AdvanceStage ()
        {
            if (!use_dynamic_stage_creation && level_stages.Count > 0)
            {
                current_stage_index++;
                
                if (current_stage_index >= level_stages.Count)
                {
                    if (level_completion_type == GameLevelCompletionType.Finish_AfterLastStage)
                    {
                        IsLevelCompleted = true;
                        level_background.PlaceOceanFloor();
                    }
                    else if (level_completion_type == GameLevelCompletionType.Forever_RepeatLastStage)
                    {
                        current_stage_index = level_stages.Count - 1;
                        level_stages[current_stage_index].ResetStage();
                        shark_zapper.SubscribeToStageNotifications(level_stages[current_stage_index]);
                    }
                    else if (level_completion_type == GameLevelCompletionType.Forever_AfterLastStageReturnToFirstStage)
                    {
                        current_stage_index = 0;
                        level_stages[current_stage_index].ResetStage();
                        shark_zapper.SubscribeToStageNotifications(level_stages[current_stage_index]);
                    }
                }
                else
                {
                    //If we have reached the "shipwreck bonus" stage, set the "waiting" flag to true so that she stage doesn't
                    //actually begin until we reach the ocean floor
                    if (level_stages[current_stage_index].StageType == StageType.OceanFloor_ShipwreckBonus)
                    {
                        level_background.PlaceOceanFloor();
                        waiting = true;
                    }

                    shark_zapper.SubscribeToStageNotifications(level_stages[current_stage_index]);
                }
            }
            else if (use_dynamic_stage_creation)
            {
                if (times_almost_up)
                {
                    //Add the shipwreck bonus stage to the list of stages at the end
                    level_stages.Add(shipwreck_bonus_stage);

                    //Place the ocean floor
                    level_background.PlaceOceanFloor();

                    //Set a flag so we wait to hit the ocean floor before the final stage begins
                    waiting = true;

                    //Increment the stage index
                    current_stage_index++;

                    //Subscribe to notifications from the stage
                    shark_zapper.SubscribeToStageNotifications(level_stages[current_stage_index]);
                }
                else
                {
                    //Dynamically create a new stage
                    var new_stage_difficulty = RePlay_Common.RandomNumberStatic.RandomNumbers.NextGaussian(level_difficulty_mean, level_difficulty_stddev);
                    GameStage new_stage = StageDifficultyGenerator.GenerateStage(
                        game_graphics_device, 
                        regular_font, 
                        new_stage_difficulty, 
                        prevent_jellyfish,
                        exercise_type);
                    level_stages.Add(new_stage);

                    //Increment the stage index
                    current_stage_index = level_stages.Count - 1;

                    //Subscribe to notifications from the stage
                    shark_zapper.SubscribeToStageNotifications(level_stages[current_stage_index]);
                }
            }
        }

        public void BeginLevel ()
        {
            //Add the special shipwreck bonus stage to the list of stages if necessary
            if (level_completion_type == GameLevelCompletionType.Finish_WithShipwreckBonus)
            {
                //level_stages.Add(shipwreck_bonus_stage);
            }

            //Reset the background before we start
            level_background.ResetBackground(level_background_type);

            //Record the start time for the level
            level_start_time = DateTime.Now;

            //Set a flag indicating the level has begun
            has_level_begun = true;

            //Set the waiting flag to be false
            waiting = false;

            //The time is not almost up
            times_almost_up = false;
            IsLevelCompleted = false;
            
            //Create the first stage
            if (use_dynamic_stage_creation)
            {
                AdvanceStage();
            }

            //Reset the current stage index to zero
            current_stage_index = 0;

            //Subscribe to notifications from the first stage
            shark_zapper.SubscribeToStageNotifications(level_stages[current_stage_index]);
        }

        public void UpdateLevel (GameTime gameTime, List<Keys> released_keys, bool zap)
        {
            //Update the level background
            level_background.Update(gameTime);
            
            //If the level has begun, and it is also not yet completed...
            if (has_level_begun && !IsLevelCompleted)
            {
                //If the descent into the ocean has not yet begun...
                if (!has_descent_begun)
                {
                    //Check to see if we should start descending...
                    if (DateTime.Now >= (level_start_time + start_descent_delay))
                    {
                        //If so, set the descent start time and begin the descent
                        descent_start_time = DateTime.Now;
                        has_descent_begun = true;
                        level_background.BeginDescent();
                    }
                }
                else
                {
                    //If we are already descending...

                    //Check to see if we have descended long enough to start the first stage...
                    if (DateTime.Now >= (descent_start_time + first_stage_delay))
                    {
                        //This is a special condition if the level is supposed to finish off with the "shipwreck bonus" stage.
                        //Check to see if the descent is not active anymore. If so, we can turn off the "waiting" flag and 
                        //start the shipwreck bonus stage
                        if (level_completion_type == GameLevelCompletionType.Finish_WithShipwreckBonus)
                        {
                            if (!level_background.IsDescentActive)
                            {
                                waiting = false;
                            }
                        }
                        
                        //Handle any stage updates as necessary
                        if (level_stages.Count > 0 && current_stage_index < level_stages.Count && !waiting)
                        {
                            //This is for debugging purposes
                            if (GameConfiguration.IsRePlayDebugMode)
                            {
                                if (released_keys.Count == 0)
                                {
                                    var game_activity = Game.Activity as RePlay_Game_Activity;
                                    if (game_activity != null)
                                    {
                                        game_activity.game_signal_chart.AddDataPoint(0);
                                    }
                                }
                            }
                            
                            //Handle keyboard input for the current stage
                            foreach (Keys key in released_keys)
                            {
                                //Send the keyboard input to the current stage so that it can handle it
                                level_stages[current_stage_index].HandleKeyboardInput(key, GameUtilities.ConvertKeyToChar(key));

                                //Handle VNS stimulation
                                bool stim = GameConfiguration.VNS_Manager.Determine_VNS_Triggering(DateTime.Now, 0);
                                if (stim)
                                {
                                    GameConfiguration.GameplayUI.DisplayStimulationIcon(GameConfiguration.VNS_Manager.Parameters.Enabled, TimeSpan.FromSeconds(2.0));
                                    TyperSharkSaveGameData.DidStimulationOccurFlag = true;
                                    if (GameConfiguration.VNS_Manager.Parameters.Enabled)
                                    {
                                        GameConfiguration.PCM_Manager.QuickStim();
                                    }
                                }

                                //Handle plotting of the VNS signal onto the screen
                                if (GameConfiguration.IsRePlayDebugMode)
                                {
                                    var game_activity = Game.Activity as RePlay_Game_Activity;
                                    if (game_activity != null)
                                    {
                                        game_activity.game_signal_chart.AddDataPoint(1);

                                        game_activity.vns_signal_chart.AddDataPoint(
                                            GameConfiguration.VNS_Manager.Plotting_Get_Latest_Calculated_Value(),
                                            GameConfiguration.VNS_Manager.Plotting_Get_VNS_Positive_Threshold(),
                                            GameConfiguration.VNS_Manager.Plotting_Get_VNS_Negative_Threshold()
                                            );
                                    }
                                }
                            }

                            //Update the current stage
                            level_stages[current_stage_index].Update(gameTime, zap);

                            //If the current stage has been completed, advance to the next stage in the level
                            if (level_stages[current_stage_index].IsStageCompleted)
                            {
                                AdvanceStage();
                            }
                        }
                    }
                }
            }
        }

        public void DrawLevel (SpriteBatch spriteBatch)
        {
            //Draw the background
            level_background.Draw(spriteBatch);

            if (!IsLevelCompleted)
            {
                if (has_level_begun && has_descent_begun && DateTime.Now >= (descent_start_time + first_stage_delay) && !waiting)
                {
                    if (level_stages.Count > 0 && current_stage_index < level_stages.Count)
                    {
                        //Draw the stage (sharks and such)
                        level_stages[current_stage_index].DrawStage(spriteBatch);
                    }
                }
            }
            else
            {
                string level_complete_string = "Level complete!";
                string score_string = "Score: " + GameConfiguration.CurrentScore.ToString();

                Vector2 level_complete_string_size = large_font.MeasureString(level_complete_string);
                Vector2 score_string_size = large_font.MeasureString(score_string);

                Vector2 level_complete_string_position = new Vector2(GameConfiguration.VirtualScreenHalfWidth - level_complete_string_size.X / 2, 200);
                Vector2 score_string_position = new Vector2(GameConfiguration.VirtualScreenHalfWidth - score_string_size.X / 2, 600);

                spriteBatch.DrawString(large_font, level_complete_string, level_complete_string_position, Color.White);
                spriteBatch.DrawString(large_font, score_string, score_string_position, Color.LimeGreen);
            }
            
            //Draw the shark zapper UI
            shark_zapper.Draw(spriteBatch);
        }

        #endregion
    }
}