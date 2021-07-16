using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using RePlay_Activity_Common;
using RePlay_Activity_TyperShark.Main;
using RePlay_Common;
using RePlay_Exercises;
using RePlay_VNS_Triggering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RePlay_Activity_TyperShark
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class TyperSharkGame : RePlay_Game
    {
        #region Stage generator

        private int difficulty_setting = 1;

        private Dictionary<int, Tuple<double, double>> difficulty_levels = new Dictionary<int, Tuple<double, double>>()
        {
            { 1, new Tuple<double, double>(0.05, 0.05) },
            { 2, new Tuple<double, double>(0.1, 0.05) },
            { 3, new Tuple<double, double>(0.2, 0.05) },
            { 4, new Tuple<double, double>(0.25, 0.1) },
            { 5, new Tuple<double, double>(0.3, 0.1) },
            { 6, new Tuple<double, double>(0.35, 0.1) },
            { 7, new Tuple<double, double>(0.4, 0.1) },
            { 8, new Tuple<double, double>(0.5, 0.1) },
            { 9, new Tuple<double, double>(0.6, 0.2) },
            { 10, new Tuple<double, double>(0.7, 0.3) },
        };

        #endregion

        ExerciseType exercise_type = ExerciseType.Keyboard_Typing;
        string tablet_id = string.Empty;
        string subject_id = string.Empty;
        GameLevel game_level;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont shark_font;
        SpriteFont shark_font_big;

        GameBackground game_background = new GameBackground();
        
        List<Keys> previous_frame_pressed_keys = new List<Keys>();

        SharkZapper shark_zapper = new SharkZapper();

        BinaryWriter game_save;

        public double TimeRemainingInSeconds = 300;
        public bool DeviceErrorState = false;

        private bool show_pcm_connection_status = false;
        private bool is_replay_debug_mode = false;
        private bool from_prescription;
        private VNSAlgorithmParameters vns_algorithm_parameters;
        private bool show_stim_icon = false;

        public TyperSharkGame(GameLaunchParameters game_launch_parameters)
        {
            graphics = new GraphicsDeviceManager(this);
            subject_id = game_launch_parameters.SubjectID;
            tablet_id = game_launch_parameters.TabletID;
            exercise_type = game_launch_parameters.Exercise;
            from_prescription = game_launch_parameters.LaunchedFromPrescription;
            show_pcm_connection_status = game_launch_parameters.ShowPCMConnectionStatus;
            is_replay_debug_mode = game_launch_parameters.DebugMode;
            vns_algorithm_parameters = game_launch_parameters.VNS_AlgorithmParameters;
            show_stim_icon = game_launch_parameters.ShowStimulationRequests;
            GameConfiguration.PCM_Manager = new PCM_Manager(Game.Activity);

            //Initialize the error logging service
            string external_file_storage = Game.Activity.ApplicationContext.GetExternalFilesDir(null).AbsolutePath;
            TxBDC_ErrorLogging.InitializeErrorLogging(external_file_storage);
            TxBDC_ErrorLogging.LogString("Initializing TyperShark");

            //Set the content folder based upon input parameters
            Content.RootDirectory = game_launch_parameters.ContentDirectory;

            //Set the duration and difficulty based upon the input parameters
            TimeRemainingInSeconds = Convert.ToInt32(game_launch_parameters.Duration) * 60;
            difficulty_setting = game_launch_parameters.Difficulty;

            //Toggle full screen mode
            graphics.ToggleFullScreen();

            //Initialize static variables due to the way Android handles statics
            GameConfiguration.InitializeStatics();
            GameConfiguration.IsRePlayDebugMode = is_replay_debug_mode;

            //Initialize charting
            if (is_replay_debug_mode)
            {
                var game_activity = Game.Activity as RePlay_Game_Activity;
                if (game_activity != null)
                {
                    game_activity.GameGraphingLayout.Visibility = Android.Views.ViewStates.Visible;
                    game_activity.game_signal_chart.SetYAxisLabel("Game signal");
                    game_activity.game_signal_chart.SetYAxisLimits(0, 1);

                    game_activity.vns_signal_chart.SetYAxisLabel("VNS signal");
                    game_activity.vns_signal_chart.SetNoiseThresholds(vns_algorithm_parameters.NoiseFloor,
                        -vns_algorithm_parameters.NoiseFloor);
                }
            }
        }

        #region RePlay_Game overrides

        public override bool ConnectToDevice()
        {
            return GameConfiguration.IsHardwareKeyboardAvailable(Game.Activity);
        }

        public override void ContinueGame()
        {
            DeviceErrorState = false;
        }

        public override void EndGame()
        {
            ExitTyperShark();
        }

        #endregion

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            
            try
            {
                //Initialize the PCM manager
                GameConfiguration.VNS_Manager = new VNSAlgorithm_TyperShark();
                GameConfiguration.VNS_Manager.Initialize_VNS_Algorithm(DateTime.Now, vns_algorithm_parameters);
                GameConfiguration.PCM_Manager.PCM_Event += (a, b) =>
                {
                    try
                    {
                        TyperSharkSaveGameData.SaveMessageFromReStoreService(game_save, b);
                    }
                    catch (Exception)
                    {
                        //empty
                    }
                };

                //Load the dictionary for typershark
                GameConfiguration.LoadGameDictionary(Game.Activity, exercise_type);

                //Set up the level and begin the level
                game_level = new GameLevel(
                    game_background, 
                    shark_zapper, 
                    GraphicsDevice, 
                    shark_font, 
                    shark_font_big, 
                    GameLevelCompletionType.Finish_WithShipwreckBonus,
                    true, 
                    difficulty_levels[difficulty_setting].Item1, 
                    difficulty_levels[difficulty_setting].Item2,
                    exercise_type);
                game_level.BeginLevel();

                //Get build information
                var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(Game.Activity);
                var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
                var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

                //Open a file for saving game data
                string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string game_name = "TyperShark";
                string exercise_name = ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(exercise_type);
                string game_file_name = subject_id + "_" + game_name + "_" + current_date_time_stamp + "_gamedata.txt";
                game_save = Exercise_SaveData.OpenFileForSaving(Game.Activity, 
                    game_file_name, 
                    build_date,
                    version_name,
                    version_code,
                    tablet_id,
                    subject_id, 
                    game_name, 
                    exercise_name, 
                    double.NaN, 
                    double.NaN, 
                    double.NaN,
                    from_prescription,
                    new VNSAlgorithmParameters());

                NotifySetupCompleted(true);
            }
            catch (Exception e)
            {
                TxBDC_ErrorLogging.LogString("Error attempting to initialize TyperShark!");
                TxBDC_ErrorLogging.LogException(e);
                NotifySetupCompleted(false);
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            try
            {
                // Create a new SpriteBatch, which can be used to draw textures.
                spriteBatch = new SpriteBatch(GraphicsDevice);

                //Load the shark font content
                shark_font = Content.Load<SpriteFont>("GameFont");
                shark_font_big = Content.Load<SpriteFont>("GameFontLarge");

                //Load various graphics and UI elements
                GameConfiguration.LoadBubbles(Content);
                GameConfiguration.LoadPredators(Content);
                GameConfiguration.GameplayUI = new RePlay_Game_GameplayUI(this, this.GraphicsDevice.Viewport, 
                    GameConfiguration.PCM_Manager, show_pcm_connection_status, 
                    is_replay_debug_mode, true, show_stim_icon, vns_algorithm_parameters.Enabled);
                GameConfiguration.GameplayUI.LoadContent();
                shark_zapper.LoadContent(Content, GraphicsDevice);
                game_background.LoadContent(Content);
            }
            catch (Exception e)
            {
                TxBDC_ErrorLogging.LogString("Error attempting to load TyperShark assets!");
                TxBDC_ErrorLogging.LogException(e);
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (DeviceErrorState)
            {
                return;
            }

            GameConfiguration.GameplayUI.Update(gameTime, TimeRemainingInSeconds, GameConfiguration.CurrentScore);

            if (!GameConfiguration.GameplayUI.Paused)
            {
                TimeRemainingInSeconds -= gameTime.ElapsedGameTime.TotalSeconds;

                if (TimeRemainingInSeconds <= -5)
                {
                    EndGame();
                    return;
                }
                else if (TimeRemainingInSeconds <= 0)
                {
                    game_level.SignalTimesUp();
                }
                else if (TimeRemainingInSeconds <= 45)
                {
                    game_level.SignalTimesAlmostUp();
                }
                else if (TimeRemainingInSeconds <= 60)
                {
                    game_level.SignalPreventJellyfish();
                }

                if (GameConfiguration.IsHardwareKeyboardAvailable(Game.Activity))
                {
                    if (gameTime.TotalGameTime.TotalSeconds >= 3.0)
                    {
                        //Get the state of the keyboard and determine which keys were released by the user
                        KeyboardState key_state = Keyboard.GetState();
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        var pressed_keys = key_state.GetPressedKeys().ToList();
                        var released_keys = previous_frame_pressed_keys.Where(x => !pressed_keys.Contains(x)).ToList();
                        previous_frame_pressed_keys = pressed_keys;

                        //Determine whether or not to use the shark zapper
                        bool zap = false;
                        if (released_keys.Contains(Keys.Enter) && shark_zapper.IsSharkZapperReady)
                        {
                            shark_zapper.SharkZapperValue = 0;
                            zap = true;
                        }

                        //Handle any touches on the touchscreen
                        var touch_collection = TouchPanel.GetState();
                        if (touch_collection.Count > 0)
                        {
                            var first_touch = touch_collection[0];
                            if (first_touch.State == TouchLocationState.Released)
                            {
                                //handle any touches here
                            }
                        }

                        //Update the state of the level
                        game_level.UpdateLevel(gameTime, released_keys, zap);

                        //Save the state of the level
                        TyperSharkSaveGameData.SaveCurrentGameData(game_save, game_level, released_keys);

                        //Exit the game if necessary
                        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                        {
                            EndGame();
                            return;
                        }
                    }
                }
                else
                {
                    DeviceErrorState = true;
                    NotifyDeviceCommunicationError();
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();
            
            game_level.DrawLevel(spriteBatch);
            GameConfiguration.GameplayUI.Render(spriteBatch);

            spriteBatch.End();

            //spriteBatch.Begin();
            //spriteBatch.End();

            base.Draw(gameTime);
        }

        public void ExitTyperShark ()
        {
            TxBDC_ErrorLogging.LogString("Exiting TyperShark");
            TyperSharkSaveGameData.CloseFile(game_save);
            Activity.SetResult(Android.App.Result.Ok);
            Activity.Finish();
            return;
        }
    }
}
