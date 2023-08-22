using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using RePlay_Activity_Common;
using RePlay_Activity_SpaceRunner.Main;
using RePlay_Activity_SpaceRunner.UI;
using RePlay_Exercises;
using RePlay_VNS_Triggering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;

namespace RePlay_Activity_SpaceRunner
{
    public class SpaceRunnerGame : RePlay_Game
    {
        #region Private Properties

        // Constants
        private const int DIFFICULTY_UPDATE_INTERVAL = 10;

        // Graphics
        private GraphicsDeviceManager GraphicsManager;
        private SpriteBatch Batch;
        public World Space;
        
        // Game stuff
        public Astronaut Player;
        public SpaceManager Road;
        private SpaceRunner_GameplayUI GameManager;
        public GameStage Stage;
        private ExerciseDeviceType Device;
        private ExerciseType Exercise;
        private string SubjectID;
        private string TabletID;
        public int Duration;
        private float RestartTimer = 0f;
        private bool StartAnimation = false;
        public List<int> Scores = new List<int>();

        private bool show_pcm_connection_status = false;
        private bool is_replay_debug_mode = false;
        private PCM_Manager PCM;

        #endregion

        #region Public Properties
        
        public InputManager GameInput;
        public int BestScore { get; set; } = 0;
        public static GraphicsDevice GameGraphicsDevice;
        public double SecondsLeft { get; set; }
        public bool Debug { get; } = false;

        public GameState State = GameState.STARTING;

        public static int VirtualScreenWidth = 2560;
        public static int VirtualScreenHeight = 1600;

        public static int ScreenWidth;
        public static int ScreenHeight;
        private BinaryWriter gamedata_save_file_handle;

        private double tempGain = 1.0;
        private bool from_prescription;

        private VNSAlgorithmParameters vns_algorithm_parameters = new VNSAlgorithmParameters();
        private bool show_stim_icon = false;

        #endregion

        #region Constructor

        public SpaceRunnerGame(GameLaunchParameters game_launch_parameters)
        {
            tempGain = game_launch_parameters.Gain;

            show_pcm_connection_status = game_launch_parameters.ShowPCMConnectionStatus;
            is_replay_debug_mode = game_launch_parameters.DebugMode;
            show_stim_icon = game_launch_parameters.ShowStimulationRequests;
            PCM = new PCM_Manager(Game.Activity);
            
            GraphicsManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = game_launch_parameters.ContentDirectory;
            Duration = Convert.ToInt32(game_launch_parameters.Duration);
            Device = game_launch_parameters.Device;
            TabletID = game_launch_parameters.TabletID;
            SubjectID = game_launch_parameters.SubjectID;
            Exercise = game_launch_parameters.Exercise;
            SecondsLeft = Duration * 60;
            from_prescription = game_launch_parameters.LaunchedFromPrescription;
            vns_algorithm_parameters = game_launch_parameters.VNS_AlgorithmParameters;
            vns_algorithm_parameters.NoiseFloor = (vns_algorithm_parameters.NoiseFloor * game_launch_parameters.Gain) / ExerciseBase.StandardExerciseSensitivity;

            if (is_replay_debug_mode)
            {
                var game_activity = Game.Activity as RePlay_Game_Activity;
                if (game_activity != null)
                {
                    game_activity.GameGraphingLayout.Visibility = Android.Views.ViewStates.Visible;
                    game_activity.game_signal_chart.SetYAxisLabel("Game signal");
                    game_activity.game_signal_chart.SetYAxisLimits(0, double.NaN);
                    game_activity.game_signal_chart.AddHorizontalLineAnnotation(0.05, OxyPlot.OxyColors.Red);

                    game_activity.vns_signal_chart.SetYAxisLabel("VNS signal");
                    game_activity.vns_signal_chart.SetNoiseThresholds(vns_algorithm_parameters.NoiseFloor,
                        -vns_algorithm_parameters.NoiseFloor);
                }
            }

            GraphicsManager.ToggleFullScreen();
        }

        #endregion

        #region RePlay_Game overrides

        public override bool ConnectToDevice()
        {
            if (GameInput != null)
            {
                return GameInput.ReconnectToDevice();
            }
            else
            {
                return false;
            }
        }

        public override void EndGame()
        {
            EndSpaceRunnerGame();
        }

        public override void ContinueGame()
        {
            if (State == GameState.ERROR_ENCOUNTERED)
            {
                State = GameState.ERROR_RECOVERED;
            }
        }

        #endregion

        #region Monogame overrides

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            bool setup_successful = true;

            // Create a new SpriteBatch, which can be used to draw textures.
            Batch = new SpriteBatch(GraphicsDevice);
            SpaceRunnerGame.GameGraphicsDevice = this.GraphicsDevice;
            ResizeScreen();

            //Set up the device and exercise
            try
            {
                GameInput = new InputManager(Game.Activity, PCM, Device, Exercise, 
                    TabletID, SubjectID, tempGain, from_prescription,
                    vns_algorithm_parameters, is_replay_debug_mode);

                //Get build information
                var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(Game.Activity);
                var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
                var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

                string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string game_name = "SpaceRunner";
                string exercise_name = ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(Exercise);
                string file_name = SubjectID + "_" + game_name + "_" + current_date_time_stamp + ".txt";

                string game_file_name = SubjectID + "_" + game_name + "_" + current_date_time_stamp + "_gamedata.txt";
                gamedata_save_file_handle = Exercise_SaveData.OpenFileForSaving(Game.Activity, 
                    game_file_name, 
                    build_date,
                    version_name,
                    version_code,
                    TabletID,
                    SubjectID, 
                    game_name, 
                    exercise_name, 
                    ExerciseBase.StandardExerciseSensitivity, 
                    GameInput.Exercise.Gain,
                    ExerciseBase.StandardExerciseSensitivity,
                    from_prescription,
                    vns_algorithm_parameters);

                SpaceRunnerSaveGameData.SaveRebaselineEvent(gamedata_save_file_handle, 
                    this, 
                    GameInput.Exercise.RetrieveBaselineData());
            }
            catch (Exception)
            {
                setup_successful = false;
            }

            Space = new World();
            Player = new Astronaut(this);
            Road = new SpaceManager();
            GameManager = new SpaceRunner_GameplayUI(this, 
                GraphicsDevice.Viewport, PCM, show_pcm_connection_status, 
                is_replay_debug_mode, show_stim_icon, vns_algorithm_parameters.Enabled);
            Stage = new GameStage(Player, this);

            // Load content
            GameManager.LoadContent();
            Stage.LoadContent(Content);
            Player.LoadContent(Content);
            Road.LoadContent(Content);
            Road.PropertyChanged += DisplayNotification;

            SpaceRunnerSaveGameData.SaveMetaData(gamedata_save_file_handle, this);
            
            NotifySetupCompleted(setup_successful);
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            if (State != GameState.ERROR_ENCOUNTERED)
            {
                try
                {
                    // Update touch data
                    var touchData = TouchPanel.GetState();

                    GameInput.Update(touchData, GameManager);

                    bool center = GameManager.Update(gameTime, Convert.ToInt32(SecondsLeft), Player.Score, GameInput.TouchData);
                    if (center)
                    {
                        NotifyDeviceRebaseline();
                        GameInput.Exercise.ResetExercise();
                        GameInput.VNS?.Flush_VNS_Buffers();
                        SpaceRunnerSaveGameData.SaveRebaselineEvent(gamedata_save_file_handle, this, GameInput.Exercise.RetrieveBaselineData());
                    }

                    // Update game state
                    UpdateGameState(gameTime);
                }
                catch (Exception)
                {
                    State = GameState.ERROR_ENCOUNTERED;
                    NotifyDeviceCommunicationError();
                }
                
                switch (State)
                {
                    case GameState.GAMEOVER:
                        BestScore = (Scores.Count > 0) ? Scores.Max() : Player.Score;
                        break;

                    case GameState.ERROR_ENCOUNTERED:
                        break;
                    case GameState.ERROR_RECOVERED:
                        HandleRestart(gameTime);
                        break;
                    case GameState.PAUSED:
                        break;

                    case GameState.STARTING:
                        if (!StartAnimation)
                        {
                            Stage.BeginAnimation();
                            StartAnimation = true;
                        }
                        Stage.Update(gameTime, State, Player.Crashed);
                        Player.Update(gameTime, GameInput.BinaryExerciseData, State, gamedata_save_file_handle);
                        break;

                    case GameState.RUNNING:
                        Space.Step((float)gameTime.ElapsedGameTime.TotalSeconds);
                        SecondsLeft -= gameTime.ElapsedGameTime.TotalSeconds;
                        Stage.Update(gameTime, State, Player.Crashed);
                        Player.Update(gameTime, GameInput.BinaryExerciseData, State, gamedata_save_file_handle);
                        Road.Update(gameTime, Player, State, gamedata_save_file_handle);

                        if (Player.Crashed)
                        {
                            HandleRestart(gameTime);
                        }

                        // Check if we should adjust difficulty
                        //if (LastDifficultyUpdate - SecondsLeft >= DIFFICULTY_UPDATE_INTERVAL)
                        //{
                        //    AdaptDifficulty();
                        //    LastDifficultyUpdate = SecondsLeft;
                        //}
                        
                        SpaceRunnerSaveGameData.SaveCurrentGameData(gamedata_save_file_handle, this);

                        break;
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                {
                    EndGame();
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            var scaleX = (float)ScreenWidth / VirtualScreenWidth;
            var scaleY = (float)ScreenHeight / VirtualScreenHeight;
            var scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1.0f);
            Batch.Begin(transformMatrix: scaleMatrix);

            Stage.Draw(Batch);
            Road.Draw(Batch);
            Player.Draw(Batch);

            Batch.End();

            Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            GameManager.Render(Batch);
            Batch.End();

            base.Draw(gameTime);
        }

        private void UpdateGameState(GameTime gameTime)
        {
            // No time left
            if (SecondsLeft <= 0)
                State = GameState.GAMEOVER;
            // Pause has been pressed
            else if (GameManager.Paused)
                State = GameState.PAUSED;
            // Player crashed
            else if (StartAnimation)
                State = GameState.STARTING;
            else if (Player.Flying)
                State = GameState.RUNNING;
        }

        #endregion

        #region Public Methods

        // Finish game activity
        public void EndSpaceRunnerGame()
        {
            State = GameState.GAMEOVER;
            GameInput.CloseInput();
            Activity.SetResult(Android.App.Result.Ok);
            Activity.Finish();
        }

        public void FinishedStarting()
        {
            StartAnimation = false;
            GameManager.DisplayInstructions = true;
        }

        public void RemoveInstructions()
        {
            GameManager.DisplayInstructions = false;
        }

        #endregion

        #region Private Methods

        // Adjust difficulty
        private void AdaptDifficulty()
        {
            // Step up difficulty
            Road.IncreaseDifficulty();
            Player.IncreaseDifficulty();
        }

        // Display notifs
        private void DisplayNotification(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "HitLaser":
                    GameManager.AddNotificationString("Zapped! Restarting...");
                    break;
                case "AvoidedLaser":
                    GameManager.AddNotificationString("Nice flying!");
                    break;
                case "HitFloor":
                    GameManager.AddNotificationString("Ouch! Restarting...");
                    break;
                case "GrabCoin":
                    GameManager.AddNotificationString("+100");
                    break;
            }
        }

        // Handle restarting of game
        private void HandleRestart(GameTime time)
        {
            RestartTimer += (float)time.ElapsedGameTime.TotalSeconds;
            if (RestartTimer >= 3.0)
            {
                RestartTimer = 0f;
                RestartGame();
            }
        }

        // Reset games
        private void RestartGame()
        {
            State = GameState.STARTING;
            StartAnimation = false;
            Scores.Add(Player.Score);
            Player = new Astronaut(this);
            Road = new SpaceManager();
            Stage = new GameStage(Player, this);
            Stage.LoadContent(Content);
            Player.LoadContent(Content);
            Road.LoadContent(Content);
            Road.PropertyChanged += DisplayNotification;

            SpaceRunnerSaveGameData.SaveEndofAttemptData(gamedata_save_file_handle, this);

            //Player.Reset();
            //Road.Reset();
        }

        // Calculate proper screen size for sprite rendering
        private void ResizeScreen()
        {
            GraphicsManager.GraphicsProfile = GraphicsProfile.HiDef;
            ScreenWidth = GraphicsManager.PreferredBackBufferWidth;
            ScreenHeight = GraphicsManager.PreferredBackBufferHeight;
            
            var viewport = GraphicsDevice.Viewport;
            var aspectRatio = (float)VirtualScreenWidth / VirtualScreenHeight;
            var width = viewport.Width;
            var height = (int)(width / aspectRatio + 0.5f);

            if (height > viewport.Height)
            {
                height = viewport.Height;
                width = (int)(height * aspectRatio + 0.5f);
            }

            var x = (viewport.Width / 2) - (width / 2);
            var y = (viewport.Height / 2) - (height / 2);

            GraphicsDevice.Viewport = new Viewport(x, y, width, height);
            //GraphicsDevice.Viewport = new Viewport(x, y, ScreenWidth, ScreenHeight);
        }

        #endregion
        
    }
}
