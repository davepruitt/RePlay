using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using RePlay_Activity_Common;
using RePlay_Activity_FruitNinja.Main;
using RePlay_Activity_FruitNinja.UI;
using RePlay_Exercises;
using RePlay_VNS_Triggering;

namespace RePlay_Activity_FruitNinja
{
    public class FruitNinjaGame : RePlay_Game
    {
        #region Private Properties

        // Constants
        private const int DIFFICULTY_UPDATE_INTERVAL = 20;

        // Graphics
        private Texture2D Background;
        private GraphicsDeviceManager GraphicsManager;
        private SpriteBatch Batch;
        public static int VirtualScreenWidth = 2560;
        public static int VirtualScreenHeight = 1600;
        public float scaleX, scaleY;
        public Matrix scaleMatrix;


        // Game
        private BinaryWriter gamedata_save_file_handle;
        private BinaryWriter controller_save_file_handle;
        private Knife Blade;
        public int Duration;
        public Gameplay GameManager;
        private Random Randomizer;
        private List<string> HitStrings = new List<string>(new string[] { "Nice Job!", "Great Hit!", "Sliced!", "Good Cut!" });
        private double LastDifficultyUpdate;

        private bool show_pcm_connection_status = false;
        public bool is_replay_debug_mode = false;
        private PCM_Manager PCM;
        private string subject_id_temp = string.Empty;
        private bool show_stim_icon = false;

        private VNSAlgorithmParameters vns_algorithm_parameters = new VNSAlgorithmParameters();

        #endregion

        #region Public Properties

        public ProjectileManager Dojo;
        public int ScreenWidth;
        public int ScreenHeight;
        public double SecondsLeft { get; set; }
        public bool Debug { get; } = false;
        public enum GameState
        {
            PAUSED, RUNNING, GAMEOVER
        }
        public GameState State = GameState.RUNNING;
        
        #endregion

        #region Constructor

        public FruitNinjaGame(GameLaunchParameters game_launch_parameters)
        {
            GraphicsManager = new GraphicsDeviceManager(this);
            GraphicsManager.ToggleFullScreen();
            Content.RootDirectory = game_launch_parameters.ContentDirectory;
            Duration = Convert.ToInt32(game_launch_parameters.Duration);
            subject_id_temp = game_launch_parameters.SubjectID;
            show_stim_icon = game_launch_parameters.ShowStimulationRequests;

            show_pcm_connection_status = game_launch_parameters.ShowPCMConnectionStatus;
            is_replay_debug_mode = game_launch_parameters.DebugMode;
            vns_algorithm_parameters = game_launch_parameters.VNS_AlgorithmParameters;

            //Get build information
            var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(Game.Activity);
            var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
            var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

            string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string game_name = "FruitNinja";
            string exercise_name = "Touch";
            string file_name = game_launch_parameters.SubjectID + "_" + game_name + "_" + current_date_time_stamp + ".txt";
            controller_save_file_handle = Exercise_SaveData.OpenFileForSaving(Game.Activity, 
                file_name, 
                build_date,
                version_name,
                version_code,
                game_launch_parameters.TabletID,
                game_launch_parameters.SubjectID, 
                game_name, 
                exercise_name, 
                double.NaN, 
                double.NaN, 
                double.NaN,
                game_launch_parameters.LaunchedFromPrescription,
                game_launch_parameters.VNS_AlgorithmParameters);

            string game_file_name = game_launch_parameters.SubjectID + "_" + game_name + "_" + current_date_time_stamp + "_gamedata.txt";
            gamedata_save_file_handle = Exercise_SaveData.OpenFileForSaving(Game.Activity, 
                game_file_name, 
                build_date,
                version_name,
                version_code,
                game_launch_parameters.TabletID,
                game_launch_parameters.SubjectID, 
                game_name, 
                exercise_name, 
                double.NaN, 
                double.NaN, 
                double.NaN,
                game_launch_parameters.LaunchedFromPrescription,
                game_launch_parameters.VNS_AlgorithmParameters);

            PCM = new PCM_Manager(Game.Activity);
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

            if (is_replay_debug_mode)
            {
                var game_activity = Game.Activity as RePlay_Game_Activity;
                if (game_activity != null)
                {
                    game_activity.GameGraphingLayout.Visibility = Android.Views.ViewStates.Visible;
                    game_activity.game_signal_chart.SetYAxisLabel("Velocity (px/sec)");
                    game_activity.game_signal_chart.SetYAxisLimits(0, double.NaN);

                    game_activity.vns_signal_chart.SetYAxisLabel("VNS signal");
                    game_activity.vns_signal_chart.SetNoiseThresholds(vns_algorithm_parameters.NoiseFloor,
                        -vns_algorithm_parameters.NoiseFloor);
                }
            }
            
        }

        #endregion

        #region RePlay_Game overrides

        public override void EndGame()
        {
            EndFruitNinja();
        }

        public override void ContinueGame()
        {
            base.ContinueGame();
        }

        public override bool ConnectToDevice()
        {
            return base.ConnectToDevice();
        }

        #endregion

        #region Monogame Overrides

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            ResizeScreen();
            StartLevel(subject_id_temp);

            // Load in a bunch of content
            Batch = new SpriteBatch(GraphicsDevice);
            Background = Content.Load<Texture2D>("background");
            GameManager.LoadContent();
            Blade.LoadContent(Batch);
            Dojo.LoadContent();

            NotifySetupCompleted(true);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            //Get the current state of the touch screen
            TouchCollection touches = TouchPanel.GetState();
            
            //Check to see if the user has pressed the back button. If so, end the game.
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                EndGame();
            }

            //Update the current game state
            if (SecondsLeft <= 0)
            {
                State = GameState.GAMEOVER;
            }
            else if (GameManager.Paused)
            {
                State = GameState.PAUSED;
            }
            else
            {
                State = GameState.RUNNING;
            }

            GameManager.Update(gameTime, SecondsLeft, Blade.Score, touches);

            if (State == GameState.RUNNING)
            {
                SecondsLeft -= (double)gameTime.ElapsedGameTime.Milliseconds / 1000;

                var adapt = LastDifficultyUpdate - SecondsLeft >= DIFFICULTY_UPDATE_INTERVAL;

                TouchCollection transformed_touch_collection = new TouchCollection();
                if (touches.Count > 0)
                {
                    List<TouchLocation> list_of_transformed_touch_locations = new List<TouchLocation>();
                    var viewport = GraphicsDevice.Viewport;
                    var matrix = Matrix.Invert(scaleMatrix);
                    foreach (var old_touch in touches)
                    {
                        var oldPosition = new Vector2(
                            old_touch.Position.X - viewport.X, 
                            old_touch.Position.Y - viewport.Y);
                        var newPosition = Vector2.Transform(oldPosition, matrix);

                        TouchLocation new_touch = new TouchLocation(old_touch.Id, old_touch.State, newPosition);
                        list_of_transformed_touch_locations.Add(new_touch);
                    }

                    transformed_touch_collection = new TouchCollection(list_of_transformed_touch_locations.ToArray());
                }

                if (transformed_touch_collection.Count > 0)
                {
                    //Save touch screen data
                    Exercise_SaveData.SaveCurrentTouchData(
                        controller_save_file_handle,
                        transformed_touch_collection[0].Position.X,
                        transformed_touch_collection[0].Position.Y);
                }

                // Check if we should adjust difficulty
                if (adapt)
                {
                    AdaptDifficulty();
                    LastDifficultyUpdate = SecondsLeft;
                }

                Blade.Update(gameTime, transformed_touch_collection, controller_save_file_handle);
                Dojo.Update(gameTime, Blade);

                // Save game data
                FruitNinjaSaveGameData.SaveCurrentGameData(gamedata_save_file_handle, this, Dojo, Blade, adapt, transformed_touch_collection);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Batch.Draw(Background, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
            Batch.End();

            Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, transformMatrix: scaleMatrix);

            Dojo.Draw(Batch);
            Blade.Draw(Batch);

            Batch.End();

            Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            GameManager.Render(Batch);

            Batch.End();

            base.Draw(gameTime);
        }

        #endregion

        #region Private Methods

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
            scaleX = (float)ScreenWidth / VirtualScreenWidth;
            scaleY = (float)ScreenHeight / VirtualScreenHeight;
            scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1.0f);
        }

        // Initialize Fruit Ninja objects
        private void StartLevel(string subject)
        {
            SecondsLeft = Duration * 60;
            LastDifficultyUpdate = SecondsLeft;
            GameManager = new Gameplay(this, 
                GraphicsDevice.Viewport, PCM, show_pcm_connection_status, 
                is_replay_debug_mode, show_stim_icon, vns_algorithm_parameters.Enabled);
            Randomizer = new Random();
            Blade = new Knife(this, PCM, vns_algorithm_parameters);
            Dojo = new ProjectileManager(this, VirtualScreenWidth, VirtualScreenWidth);
            Dojo.PropertyChanged += DisplayNotification;

            FruitNinjaSaveGameData.SaveMetaData(gamedata_save_file_handle, this);
        }

        // Event Handler for Notifications
        private void DisplayNotification(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Score":
                    GameManager.AddNotificationString(GetRandomHitString() + " +25");
                    break;
                case "MissedFruit":
                    GameManager.AddNotificationString("Missed!");
                    break;
                case "Bomb":
                    GameManager.AddNotificationString("Kaboom!");
                    break;
                case "Combo2":
                    GameManager.DisplayComboImage(2);
                    break;
                case "Combo3":
                    GameManager.DisplayComboImage(3);
                    break;
                case "Combo4":
                    GameManager.DisplayComboImage(4);
                    break;
                case "DifficultyI":
                    if (Debug) GameManager.AddNotificationString("Level Up!");
                    break;
                case "DifficultyD":
                    if (Debug) GameManager.AddNotificationString("Level Down");
                    break;
            }
        }

        // Finish game activity
        public void EndFruitNinja()
        {
            State = GameState.GAMEOVER;

            FruitNinjaSaveGameData.SaveFinalData(gamedata_save_file_handle, Dojo, Blade);
            FruitNinjaSaveGameData.CloseFile(gamedata_save_file_handle);
            Exercise_SaveData.CloseFile(controller_save_file_handle);

            Activity.SetResult(Android.App.Result.Ok);
            Activity.Finish();
        }

        // Get random string for a fruit hit
        private string GetRandomHitString()
        {
            var idx = Randomizer.Next(0, HitStrings.Count);

            return HitStrings[idx];
        }

        // Dynamically change the difficulty
        private void AdaptDifficulty()
        {
            if (Dojo.BombsHit >= 3 || Dojo.FruitAccuracy <= 0.3)
            {
                // step down
                Blade.DecreaseDifficulty();
                Dojo.DecreaseDifficulty();
            }
            else if (Dojo.BombsHit < 3 && Dojo.FruitAccuracy >= 0.8)
            {
                // step up
                Blade.IncreaseDifficulty();
                Dojo.IncreaseDifficulty();
            }
        }

        #endregion
        
    }
}
