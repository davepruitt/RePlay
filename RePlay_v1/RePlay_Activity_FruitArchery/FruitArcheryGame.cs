using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Diagnostics;
using System;
using System.IO;
using RePlay_Activity_FruitArchery.Main;
using Microsoft.Xna.Framework.Input.Touch;
using RePlay_VNS_Triggering;
using FitMiAndroid;
using RePlay_Exercises;
using RePlay_Common;
using RePlay_Activity_Common;
using System.Collections.Generic;

namespace RePlay_Activity_FruitArchery
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class FruitArcheryGame : RePlay_Game
    {
        #region Private data members

        private GraphicsDeviceManager graphics;
        private SpriteBatch sprite_batch;
        private BasicEffect sprite_batch_effect;
        private SpriteFont score_font;

        private FruitArchery_World world;

        private DebugView debug_view;
        private bool debug_flag = false;
        
        private Color background_color = Color.CornflowerBlue;

        private Vector3 camera_position = new Vector3(0, 0, -1);        //change to +1 to flip(correct) and -1 to flip
        private float camera_view_width = 2560;       //25.6 meters
        private float camera_view_height = 1600;       //16 meters
        private Matrix screen_to_virtual_scale_matrix = Matrix.Identity;

        private ExerciseDeviceType Device;
        private BinaryWriter controller_save_file_handle;
        private BinaryWriter gamedata_save_file_handle;

        private PCM_Manager PCM;
        private VNSAlgorithm_Standard VNS;
        private VNSAlgorithmParameters vns_parameters;
        
        private RePlay_Game_GameplayUI gameplay_ui;
        private Int32 baseline_puck_force = 0;
        private Int32 puck_force_threshold = 50;
        private bool show_stim_icon = false;

        private enum FruitArcheryGameStates
        {
            Running,
            ErrorEncountered,
            Exiting
        }

        private FruitArcheryGameStates state = FruitArcheryGameStates.Running;

        #endregion

        #region Constructor

        public FruitArcheryGame(GameLaunchParameters game_launch_parameters)
        {
            graphics = new GraphicsDeviceManager(this);

            //Initialize the error logging service
            string external_file_storage = Game.Activity.ApplicationContext.GetExternalFilesDir(null).AbsolutePath;
            TxBDC_ErrorLogging.InitializeErrorLogging(external_file_storage);
            TxBDC_ErrorLogging.LogString("Initializing FruitArchery");

            //Set the content folder according the value passed in
            Content.RootDirectory = game_launch_parameters.ContentDirectory;
            
            //Set the game duration according to the value passed in
            FruitArchery_GameSettings.TimeRemainingInSeconds = Convert.ToInt32(game_launch_parameters.Duration) * 60;
            FruitArchery_GameSettings.CurrentGameScore = 0;
            FruitArchery_GameSettings.ExerciseGain = game_launch_parameters.Gain;
            FruitArchery_GameSettings.ShowPCMConnectionStatus = game_launch_parameters.ShowPCMConnectionStatus;
            FruitArchery_GameSettings.IsRePlayDebugMode = game_launch_parameters.DebugMode;
            FruitArchery_GameSettings.StimulationExercise = game_launch_parameters.Exercise;
            show_stim_icon = game_launch_parameters.ShowStimulationRequests;

            //Set the camera parameters
            camera_view_width = FruitArchery_GameSettings.VirtualScreenWidth;
            camera_view_height = FruitArchery_GameSettings.VirtualScreenHeight;

            //Get the default screen resolution from Android and set our back buffer to be that size

            graphics.ToggleFullScreen();
            Android.Graphics.Point actual_screen_size = new Android.Graphics.Point();
            Game.Activity.WindowManager.DefaultDisplay.GetSize(actual_screen_size);
            graphics.PreferredBackBufferWidth = actual_screen_size.X;
            graphics.PreferredBackBufferHeight = actual_screen_size.Y;
            graphics.ApplyChanges();

            //Create a "scale matrix" to deal with conversions from virtual screen size to actual screen size and vice versa
            var scaleX = (float)actual_screen_size.X / (float)FruitArchery_GameSettings.VirtualScreenWidth;
            var scaleY = (float)actual_screen_size.Y / (float)FruitArchery_GameSettings.VirtualScreenHeight;
            screen_to_virtual_scale_matrix = Matrix.CreateScale(scaleX, scaleY, 1.0f);

            //Grab the device from the exercise passed in
            Device = game_launch_parameters.Device;

            //Get build information
            var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(Game.Activity);
            var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
            var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

            vns_parameters = game_launch_parameters.VNS_AlgorithmParameters;

            //Initialize the save file
            string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string subject_id = game_launch_parameters.SubjectID;
            string game_name = "FruitArchery";
            string exercise_name = ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(game_launch_parameters.Exercise);
            string file_name = subject_id + "_" + game_name + "_" + current_date_time_stamp + ".txt";
            controller_save_file_handle = Exercise_SaveData.OpenFileForSaving(Game.Activity, 
                file_name,
                build_date,
                version_name,
                version_code,
                game_launch_parameters.TabletID,
                subject_id, 
                game_name, 
                exercise_name, 
                double.NaN,
                game_launch_parameters.Gain, 
                double.NaN,
                game_launch_parameters.LaunchedFromPrescription,
                vns_parameters);

            string game_file_name = subject_id + "_" + game_name + "_" + current_date_time_stamp + "_gamedata.txt";
            gamedata_save_file_handle = Exercise_SaveData.OpenFileForSaving(Game.Activity, 
                game_file_name, 
                build_date,
                version_name,
                version_code,
                game_launch_parameters.TabletID,
                subject_id, 
                game_name, 
                exercise_name, 
                double.NaN,
                game_launch_parameters.Gain, 
                double.NaN,
                game_launch_parameters.LaunchedFromPrescription,
                vns_parameters);

            PCM = new PCM_Manager(Activity);
            VNS = new VNSAlgorithm_Standard();
            VNS.Initialize_VNS_Algorithm(DateTime.Now, vns_parameters);

            PCM.PropertyChanged += (a, b) =>
            {
                //empty
            };

            PCM.PCM_Event += (a, b) =>
            {
                try
                {
                    Exercise_SaveData.SaveMessageFromReStoreService(controller_save_file_handle, b);
                }
                catch (Exception)
                {
                    //empty
                }
            };

            if (FruitArchery_GameSettings.IsRePlayDebugMode)
            {
                var game_activity = Game.Activity as RePlay_Game_Activity;
                if (game_activity != null)
                {
                    game_activity.GameGraphingLayout.Visibility = Android.Views.ViewStates.Visible;
                    game_activity.game_signal_chart.SetYAxisLabel("Game signal");
                    game_activity.game_signal_chart.SetYAxisLimits(double.NaN, double.NaN);

                    game_activity.vns_signal_chart.SetYAxisLabel("VNS signal");
                    game_activity.vns_signal_chart.SetNoiseThresholds(vns_parameters.NoiseFloor,
                        -vns_parameters.NoiseFloor);
                }
            }
        }

        #endregion

        #region RePlay_Game overrides

        public override void ContinueGame()
        {
            if (state == FruitArcheryGameStates.ErrorEncountered)
            {
                state = FruitArcheryGameStates.Running;
            }
        }

        public override bool ConnectToDevice()
        {
            try
            {
                FruitArchery_GameSettings.PuckDongle = new HIDPuckDongle(Game.Activity);
                FruitArchery_GameSettings.PuckDongle.Open();
            }
            catch (Exception e)
            {
                //empty
            }

            return FruitArchery_GameSettings.PuckDongle.IsOpened();
        }

        public override void EndGame()
        {
            ExitGame();
        }

        #endregion

        #region Method overrides

        protected override void Initialize()
        {
            base.Initialize();
            
            if (Device == ExerciseDeviceType.FitMi)
            {
                FruitArchery_GameSettings.PuckDongle = new HIDPuckDongle(Game.Activity);
                FruitArchery_GameSettings.PuckDongle.Open();
            }
            
            NotifySetupCompleted(true);
        }
        
        /// <summary>
        /// This function gets called inside of base.Initialize()
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            //Load the gameplay ui
            gameplay_ui = new RePlay_Game_GameplayUI(this, this.GraphicsDevice.Viewport, PCM, 
                FruitArchery_GameSettings.ShowPCMConnectionStatus, 
                FruitArchery_GameSettings.IsRePlayDebugMode,
                true,
                show_stim_icon,
                vns_parameters.Enabled);
            gameplay_ui.LoadContent();

            //Load all game textures
            FruitArchery_GameSettings.LoadGameTextures(Content);

            //Create the physics world
            World physics_world = new World();
            physics_world.Gravity = new Vector2(0, -10f);

            //Create the game world object
            world = new FruitArchery_World(physics_world, FruitArchery_GameSettings.VirtualScreenWidth, FruitArchery_GameSettings.VirtualScreenHeight);

            //Load the polygon sets for each piece of fruit
            FruitArchery_GameSettings.LoadFruitPolygons(world);

            //Create the debug view
            debug_view = new DebugView(physics_world);
            debug_view.AppendFlags(DebugViewFlags.DebugPanel | DebugViewFlags.PolygonPoints);
            debug_view.LoadContent(GraphicsDevice, Content);
            
            // Create a new SpriteBatch, which can be used to draw textures.
            sprite_batch = new SpriteBatch(GraphicsDevice);
            sprite_batch_effect = new BasicEffect(graphics.GraphicsDevice);
            sprite_batch_effect.TextureEnabled = true;
            
            score_font = Content.Load<SpriteFont>("Score");

            FruitArcherySaveGameData.SaveMetaData(gamedata_save_file_handle, 
                FruitArchery_GameSettings.CurrentStage, 
                puck_force_threshold);
        }
        
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            double actual_force = 0;
            double transformed_force = 0;

            if (state != FruitArcheryGameStates.ErrorEncountered)
            {
                //Get the state of the touch screen to handle any screen touches
                var touch_collection = TouchPanel.GetState();

                //Update the gameplay UI
                bool center = gameplay_ui.Update(gameTime, FruitArchery_GameSettings.TimeRemainingInSeconds, FruitArchery_GameSettings.CurrentGameScore, touch_collection);

                if (!gameplay_ui.Paused && state != FruitArcheryGameStates.Exiting)
                {
                    //If the FitMi is the chosen device...
                    if (Device == ExerciseDeviceType.FitMi && FruitArchery_GameSettings.PuckDongle != null)
                    {
                        //Grab new data from the puck
                        if (FruitArchery_GameSettings.PuckDongle.IsOpen)
                        {
                            FruitArchery_GameSettings.PuckDongle.CheckForNewPuckData();
                        }
                        else
                        {
                            state = FruitArcheryGameStates.ErrorEncountered;
                            NotifyDeviceCommunicationError();
                        }

                        //Check to see if the loadcell returned a value of 0
                        if (FruitArchery_GameSettings.PuckDongle.PuckPack0.Loadcell == 0)
                        {
                            //If so, return immediately
                            return;
                        }

                        //Handle a touch anywhere on the screen...
                        if (center)
                        {
                            NotifyDeviceRebaseline();
                            VNS.Flush_VNS_Buffers();
                            baseline_puck_force = FruitArchery_GameSettings.PuckDongle.PuckPack0.Loadcell;
                            world.GetBow?.ResetBaselineBowAngle();
                            if (world.GetBow != null)
                            {
                                FruitArcherySaveGameData.SaveRebaselineEvent(gamedata_save_file_handle, this, new List<double>() { world.GetBow.baseline_angle, baseline_puck_force });
                            }
                        }

                        if (baseline_puck_force == 0)
                        {
                            baseline_puck_force = FruitArchery_GameSettings.PuckDongle.PuckPack0.Loadcell;
                            FruitArcherySaveGameData.SaveRebaselineEvent(gamedata_save_file_handle, this, new List<double>() { world.GetBow.baseline_angle, baseline_puck_force });
                        }

                        //Save the current puck data to the data file
                        Exercise_SaveData.SaveCurrentPuckData(controller_save_file_handle, FruitArchery_GameSettings.PuckDongle);

                        //Calculate the current force applied to the loadcell of the puck
                        actual_force = FruitArchery_GameSettings.PuckDongle.PuckPack0.Loadcell - baseline_puck_force;
                        transformed_force = actual_force;

                        //Now let's apply the gain to the force (if "grip" is the chosen exercise)
                        if (FruitArchery_GameSettings.StimulationExercise == ExerciseType.FitMi_Grip)
                        {
                            transformed_force *= FruitArchery_GameSettings.ExerciseGain;
                        }

                        //Check to see if the transformed force overcomes the required threshold
                        if (transformed_force >= puck_force_threshold)
                        {
                            //If the force is greater than the threshold, fire an arrow
                            if (!world.IsArrowInAir())
                            {
                                world.FireArrow();
                            }
                        }

                        //Now let's grab the rotation angle of the bow
                        float bow_rotation_radians = 0;
                        var player_bow = world.GetBow;
                        if (player_bow != null)
                        {
                            bow_rotation_radians = player_bow.RotationRadians;
                        }

                        //Choose whether to pass the force or the rotation into the VNS algorithm
                        //The default is to use force, which is used if grip is the exercise
                        double vns_algorithm_sample = transformed_force;
                        if (FruitArchery_GameSettings.StimulationExercise == ExerciseType.FitMi_Supination)
                        {
                            //Otherwise, if supination is the exercise, we use the bow rotation
                            vns_algorithm_sample = bow_rotation_radians;
                        }

                        //Now pass the new sample into the vns algorithm
                        bool trigger = VNS.Determine_VNS_Triggering(DateTime.Now, vns_algorithm_sample);
                        if (trigger)
                        {
                            gameplay_ui.DisplayStimulationIcon(VNS.Parameters.Enabled, TimeSpan.FromSeconds(2.0));
                            Exercise_SaveData.SaveStimulationTriggerAtCurrentTime(controller_save_file_handle);
                            if (VNS.Parameters.Enabled)
                            {
                                PCM.QuickStim();
                            }
                        }

                        //If we are in debug mode, then let's chart the signals on the screen
                        if (FruitArchery_GameSettings.IsRePlayDebugMode)
                        {
                            var game_activity = Game.Activity as RePlay_Game_Activity;
                            if (game_activity != null)
                            {
                                game_activity.game_signal_chart.AddDataPoint(vns_algorithm_sample);
                                game_activity.vns_signal_chart.AddDataPoint(
                                    VNS.Plotting_Get_Latest_Calculated_Value(),
                                    VNS.Plotting_Get_VNS_Positive_Threshold(),
                                    VNS.Plotting_Get_VNS_Negative_Threshold()
                                    );
                            }
                        }
                    }
                    else
                    {
                        //If the touchscreen is the chosen control device...
                        
                        if (touch_collection.Count > 0)
                        {
                            var first_touch = touch_collection[0];
                            if (first_touch.State == TouchLocationState.Released)
                            {
                                if (!world.IsArrowInAir())
                                {
                                    world.FireArrow();
                                    
                                    if (DateTime.Now >= (PCM.MostRecentTriggerAttempt + PCM.CurrentStimulationTimeoutPeriod_SafeToUse))
                                    {
                                        gameplay_ui.DisplayStimulationIcon(vns_parameters.Enabled, TimeSpan.FromSeconds(2.0));
                                        Exercise_SaveData.SaveStimulationTriggerAtCurrentTime(controller_save_file_handle);
                                        if (vns_parameters.Enabled)
                                        {
                                            PCM.QuickStim();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var matrix = Matrix.Invert(screen_to_virtual_scale_matrix);
                                Vector2 touch_position_screen = first_touch.Position;
                                Vector2 transformed_touch_position_screen = Vector2.Transform(touch_position_screen, matrix);

                                float temp_x = -((transformed_touch_position_screen.X * world.WorldScalingFactor.X) - (world.WorldSize.X / 2.0f));
                                float temp_y = -((transformed_touch_position_screen.Y * world.WorldScalingFactor.Y) - (world.WorldSize.Y / 2.0f));

                                Vector2 touch_position_world = new Vector2(temp_x, temp_y);

                                Exercise_SaveData.SaveCurrentTouchData(controller_save_file_handle, touch_position_world.X, touch_position_world.Y);
                                FruitArchery_Bow.GetInstance(world).AimBow(touch_position_world);
                            }
                        }
                    }

                    //Update the time remaining in the game
                    FruitArchery_GameSettings.TimeRemainingInSeconds -= gameTime.ElapsedGameTime.TotalSeconds;

                    //Save the gamedata out to a file
                    FruitArcherySaveGameData.SaveCurrentGameData(gamedata_save_file_handle, world, Device, actual_force);

                    //Check to see if the amount of time this game is supposed to run has been exceeded.
                    if (FruitArchery_GameSettings.TimeRemainingInSeconds < 0 || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                    {
                        EndGame();
                        return;
                    }
                    else
                    {
                        //Update the world
                        world.UpdateWorld(gameTime);
                    }
                }
            }
        }
        
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            GraphicsDevice.Clear(Color.Black);

            if (Device == ExerciseDeviceType.FitMi && FruitArchery_GameSettings.PuckDongle.PuckPack0.Loadcell == 0)
            {
                //In this circumstance, the FitMi may be plugged in, but the pucks are still sitting in the dock
                string output_string = "Please remove the blue FitMi puck from the dock";
                var output_string_size = score_font.MeasureString(output_string);
                var xpos = (GraphicsDevice.Viewport.Width / 2) - (output_string_size.X / 2);
                var ypos = (GraphicsDevice.Viewport.Height / 2) - (output_string_size.Y / 2);
                
                sprite_batch.Begin();
                sprite_batch.DrawString(score_font, output_string, new Vector2(xpos, ypos), Color.Black);
                sprite_batch.End();
            }
            else
            {
                sprite_batch_effect.View = Matrix.CreateLookAt(camera_position, Vector3.Zero, Vector3.Up);
                sprite_batch_effect.Projection = Matrix.CreateOrthographic(camera_view_width / 100f, camera_view_height / 100f, -100, 100);

                sprite_batch.Begin(SpriteSortMode.Deferred, null, null, null, RasterizerState.CullNone, sprite_batch_effect, transformMatrix: screen_to_virtual_scale_matrix);

                world.DrawWorld(gameTime, sprite_batch);

                sprite_batch.End();

                sprite_batch.Begin();
                gameplay_ui.Render(sprite_batch);
                sprite_batch.End();

                if (debug_flag)
                {
                    debug_view.RenderDebugData(sprite_batch_effect.Projection, sprite_batch_effect.View, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, 0.8f);
                }
            }
        }

        public void ExitGame()
        {
            state = FruitArcheryGameStates.Exiting;

            TxBDC_ErrorLogging.LogString("Exiting FruitArchery");

            FruitArchery_GameSettings.FruitTextures.Clear();
            FruitArchery_GameSettings.FruitCollisionPolygons.Clear();

            if (Device == ExerciseDeviceType.FitMi || Device == ExerciseDeviceType.Touchscreen)
            {
                Exercise_SaveData.CloseFile(controller_save_file_handle);
            }

            FruitArcherySaveGameData.CloseFile(gamedata_save_file_handle);
            FruitArchery_Bow.GetInstance(world).DestroyBow();

            //If so, close the game.
            Game.Activity.SetResult(Android.App.Result.Ok);
            Game.Activity.Finish();
        }

        #endregion
    }
}
