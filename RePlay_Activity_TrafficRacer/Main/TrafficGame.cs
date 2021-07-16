using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Diagnostics;
using RePlay_Activity_TrafficRacer.Vehicle;
using RePlay_Activity_TrafficRacer.Environment;
using RePlay_Activity_TrafficRacer.Gui;
using RePlay_Activity_TrafficRacer.Input;
using RePlay_Activity_TrafficRacer.Graphics;
using RePlay_Exercises;
using RePlay_Activity_TrafficRacer.Main;
using System.IO;
using Microsoft.AppCenter.Analytics;
using RePlay_Activity_Common;
using RePlay_VNS_Triggering;
using Android.Media.Audiofx;

namespace RePlay_Activity_TrafficRacer
{

    public class TrafficGame : RePlay_Game
    {
        Texture2D puck_texture;
        Vector2 puck_texture_position = Vector2.Zero;
        Vector2 puck_texture_origin = Vector2.Zero;
        float puck_texture_rotation = 0.0f;
        float puck_texture_scale = 0.5f;
        float puck_margin_x = 10.0f;
        float puck_margin_y = 10.0f;
        SpriteFont puck_font;

        //Parameters
        const int coinPoints = 1000;
        const float playerSpeed = 20;

        //Things
        World world;
        Player player;
        EnvironmentManager environment;
        TrafficManager trafficManager;
        
        //Graphics
        public static GraphicsDevice Graphics;
        PostProcessor postProcessor;
        DebugView debugView;
        Effect effect;
        GraphicsDeviceManager graphicsManager;
        SpriteBatch spriteBatch;
        Viewport ScaledView;
        int ScreenWidth;
        int ScreenHeight;

        //GUI
        GameOverUI gameOverUI;
        FPSUI fpsUI;
        CountdownUI countdownUI;
        CountdownUI restartCountdownUI;
        TitleUI titleUI;
        RePlay_Game_GameplayUI GameManager;

        //Exercise
        private ExerciseDeviceType Device;
        private ExerciseType Exercise;
        private string SubjectID;
        private string TabletID;

        private BinaryWriter gamedata_save_file_handle;

        //State
        public int score;
        GameState state;
        double stateChangeTime;
        int startTime = -1;
        double restartCountdown = 3.0;
        bool inTargetLane = false;
        public static bool DEBUG;
        public int Duration;
        public static float Difficulty;
        float movement;

        private bool show_pcm_connection_status = false;
        private bool is_replay_debug_mode = false;
        private PCM_Manager PCM;

        public float adjustedSpeed
        {
            get
            {
                return playerSpeed * Math.Max(0.5f, TrafficGame.Difficulty);
            }
        }
        
        public double SecondsLeft { get; private set; } = 0;
        private bool restartCount = false;
        private double tempGain = 1.0;
        private bool from_prescription;

        private VNSAlgorithmParameters vns_algorithm_parameters;
        private bool show_stim_icon = false;

        public TrafficGame(GameLaunchParameters game_launch_parameters)
        {
            tempGain = game_launch_parameters.Gain;
            show_pcm_connection_status = game_launch_parameters.ShowPCMConnectionStatus;
            is_replay_debug_mode = game_launch_parameters.DebugMode;
            show_stim_icon = game_launch_parameters.ShowStimulationRequests;
            PCM = new PCM_Manager(Game.Activity);

            graphicsManager = new GraphicsDeviceManager(this);
            ResizeScreen();

            if (string.IsNullOrEmpty(game_launch_parameters.ContentDirectory))
            {
                Content.RootDirectory = "Content";
            } else
            {
                Content.RootDirectory = game_launch_parameters.ContentDirectory;
            }
            
            Difficulty = Convert.ToSingle(game_launch_parameters.Difficulty) * 0.1f;
            Device = game_launch_parameters.Device;
            Exercise = game_launch_parameters.Exercise;
            TabletID = game_launch_parameters.TabletID;
            SubjectID = game_launch_parameters.SubjectID;
            SecondsLeft = game_launch_parameters.Duration * 60;
            Duration = Convert.ToInt32(game_launch_parameters.Duration) * 60;
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
                    game_activity.game_signal_chart.SetYAxisLimits(double.NaN, double.NaN);

                    game_activity.vns_signal_chart.SetYAxisLabel("VNS signal");
                    game_activity.vns_signal_chart.SetNoiseThresholds(vns_algorithm_parameters.NoiseFloor,
                        -vns_algorithm_parameters.NoiseFloor);
                }
            }
        }

        #region RePlay_Game overrides

        public override bool ConnectToDevice()
        {
            return InputManager.ReconnectDevice();
        }

        public override void ContinueGame()
        {
            if (state == GameState.ERROR_ENCOUNTERED)
            {
                state = GameState.ERROR_RECOVERED;
            }
        }

        public override void EndGame()
        {
            EndTrafficRacerGame();
        }

        #endregion

        protected override void Initialize()
        {
            base.Initialize();
            Graphics = GraphicsDevice;
            
            //Initialize physics
            tainicom.Aether.Physics2D.Settings.MaxPolygonVertices = 16;
            world = new World(Vector2.Zero);
            debugView = new DebugView(world);
            debugView.AppendFlags(DebugViewFlags.DebugPanel | DebugViewFlags.PolygonPoints);
            debugView.LoadContent(GraphicsDevice, Content);

            //Create player
            player = new Player(Content, CarType.SPORT, world, adjustedSpeed);
            player.DodgeCompleteCallback = DodgeCompleted;
            player.CoinGetCallback = CoinGet;

            //Create objects
            environment = new EnvironmentManager(Content, world);
            trafficManager = new TrafficManager(Content, world);

            //Setup graphics
            Lighting.Initialize();
            effect = Content.Load<Effect>("effect");
            postProcessor = new PostProcessor(spriteBatch, Content.Load<Effect>("desaturate"));

            //Setup GUI
            gameOverUI = new GameOverUI();
            fpsUI = new FPSUI();
            countdownUI = new CountdownUI();
            restartCountdownUI = new CountdownUI(3, "RESTARTING");
            titleUI = new TitleUI();
            state = GameState.STARTING;

            //Setup input
            try
            {
                InputManager.Initialize(PCM, Device, Exercise, TabletID, 
                    SubjectID, tempGain, from_prescription, vns_algorithm_parameters, is_replay_debug_mode);

                //Get build information
                var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(Game.Activity);
                var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
                var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

                //Initialize the save file
                string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string subject_id = SubjectID;
                string game_name = "TrafficRacer";
                string exercise_name = ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(Exercise);
                string file_name = subject_id + "_" + game_name + "_" + current_date_time_stamp + ".txt";

                string game_file_name = subject_id + "_" + game_name + "_" + current_date_time_stamp + "_gamedata.txt";
                gamedata_save_file_handle = Exercise_SaveData.OpenFileForSaving(Game.Activity, 
                    game_file_name, 
                    build_date,
                    version_name,
                    version_code,
                    TabletID,
                    subject_id, 
                    game_name, 
                    exercise_name,
                    ExerciseBase.StandardExerciseSensitivity, 
                    InputManager.Exercise.Gain,
                    ExerciseBase.StandardExerciseSensitivity,
                    from_prescription,
                    vns_algorithm_parameters);

                TrafficRacerSaveGameData.SaveMetaData(gamedata_save_file_handle, environment, this);
                TrafficRacerSaveGameData.SaveRebaselineEvent(gamedata_save_file_handle, this, InputManager.Exercise.RetrieveBaselineData());
                NotifySetupCompleted(true);
            }
            catch (Exception)
            {
                NotifySetupCompleted(false);
            }
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Car.LoadContent(Content);
            GameOverUI.LoadContent(Content);
            FPSUI.LoadContent(Content);
            CountdownUI.LoadContent(Content);
            TitleUI.LoadContent(Content);
            GameManager = new RePlay_Game_GameplayUI(this, ScaledView, PCM, show_pcm_connection_status, 
                is_replay_debug_mode, true, show_stim_icon, vns_algorithm_parameters.Enabled);
            GameManager.LoadContent();
            //TODO: probably should load ALL content here. Some content (e.g. sign models) still loaded at runtime

            puck_texture = Content.Load<Texture2D>("blue_puck");
            puck_texture_origin = new Vector2(puck_texture.Width / 2, puck_texture.Height / 2);
            puck_texture_position = new Vector2(puck_margin_x + (0 + (puck_texture.Width / 2)) * puck_texture_scale, (graphicsManager.PreferredBackBufferHeight - puck_texture_scale * (puck_texture.Height / 2)) - puck_margin_y);

            puck_font = Content.Load<SpriteFont>("GameFont");
        }

        public void EndTrafficRacerGame()
        {
            Analytics.TrackEvent("TRAFFIC RACER EndGame");

            TrafficRacerSaveGameData.CloseFile(gamedata_save_file_handle);
            InputManager.Close();
            Activity.SetResult(Android.App.Result.Ok);
            Activity.Finish();
            return;
        }

        private void RestartGame(GameTime time)
        {
            restartCount = false;
            restartCountdown = 3.0;
            state = GameState.RESTARTING;
            stateChangeTime = time.TotalGameTime.TotalSeconds;
            player.Reset();
        }

        private void HandleInput(GameTime gameTime)
        {
            InputManager.Update(GameManager);

            if (InputManager.Quit)
            {
                EndGame();
            }

            if (InputManager.ToggleDebug)
            {
                TrafficGame.DEBUG = !TrafficGame.DEBUG;
                Camera.main.Revolution = Vector2.Zero;
                if (Camera.main.Mode == CameraMode.FLAT)
                {
                    Camera.main.Mode = CameraMode.PERSPECTIVE;
                }
                else
                {
                    Camera.main.Mode = CameraMode.FLAT;
                }
            }

            if (InputManager.ZoomOut)
            {
                Camera.main.Scale /= 1.5f;
            }

            if (InputManager.ZoomIn)
            {
                Camera.main.Scale *= 1.5f;
            }

            if (InputManager.Restart)
            {
                state = GameState.RESTARTING;
                stateChangeTime = gameTime.TotalGameTime.TotalSeconds;
            }

            Camera.main.Revolution += InputManager.MoveCameraAmount / 30;
        }

        //Fired when we dodge a car
        private void DodgeCompleted(Body b)
        {
            //scoreUI.ShowPoints(dodgePoints);
            //score += dodgePoints;
        }

        //Fired when we hit a coin
        private void CoinGet(Body b)
        {
            //Save information about the coin capture to the file
            Guid coin_guid = Guid.Empty;
            Coin c = environment.GetCoinFromBody(b);
            if (c != null)
            {
                coin_guid = c.UniqueID;
            }

            TrafficRacerSaveGameData.SaveCoinCapture(gamedata_save_file_handle, coin_guid);

            //Now update the score and the environment
            GameManager.AddScoreMessage(coinPoints);
            score += coinPoints;
            environment.DestroyCoin(b);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (state == GameState.ERROR_ENCOUNTERED)
            {
                return;
            }
            else if (state == GameState.ERROR_RECOVERED)
            {
                player.crashed = true;
                state = GameState.RUNNING;
            }
            else if (state == GameState.PAUSED)
            {
                state = (GameManager.Paused) ? GameState.PAUSED : GameState.RUNNING;
            }
            else
            {
                movement = InputManager.LateralMovement;
                TrafficRacerSaveGameData.SaveCurrentGameData(gamedata_save_file_handle, player, trafficManager, environment, this, environment.road, movement);

                if (state == GameState.RUNNING)
                {
                    if (!player.crashed)
                    {
                        //Minimum of 1 point per frame.
                        //Score based on velocity, double points if in correct lane
                        //TODO: For low velocities, we may want less than 1 point per frame.
                        int deltaScore = (int)Math.Max(1, (player.Velocity.Y * gameTime.ElapsedGameTime.TotalSeconds));
                        if (inTargetLane)
                        {
                            deltaScore *= 2;
                        }
                        score += deltaScore;
                    }

                    state = (GameManager.Paused) ? GameState.PAUSED : GameState.RUNNING;
                    SecondsLeft = Math.Max(0, SecondsLeft - gameTime.ElapsedGameTime.TotalSeconds);
                }
                else if (state == GameState.STARTING)
                {
                    Camera.main.Zoom = Math.Min(1, Camera.main.Zoom + 0.05f); //Zoom out camera to normal position
                    if (gameTime.TotalGameTime.TotalSeconds - stateChangeTime > 3) //Start game after 3 seconds
                    {
                        state = GameState.RUNNING;
                        stateChangeTime = gameTime.TotalGameTime.TotalSeconds;
                        player.Velocity = new Vector2(player.Velocity.X, adjustedSpeed);
                    }
                }
                else if (state == GameState.RESTARTING)
                {
                    Camera.main.Zoom = Math.Max(0, Camera.main.Zoom - 0.05f); //Zoom camera in as a transition
                    if (gameTime.TotalGameTime.TotalSeconds - stateChangeTime > 1) //Reset world after 1 second
                    {
                        state = GameState.STARTING;
                        stateChangeTime = gameTime.TotalGameTime.TotalSeconds;
                        environment.Reset();
                        trafficManager.Reset();
                        player.Reset();
                        score = 0;

                        TrafficRacerSaveGameData.SaveReStartEvent(gamedata_save_file_handle);
                    }
                }

                //Get input
                try
                {
                    HandleInput(gameTime);
                }
                catch (Exception)
                {
                    state = GameState.ERROR_ENCOUNTERED;
                    NotifyDeviceCommunicationError();
                    return;
                }

                //Get time of first frame
                if (startTime == -1)
                {
                    startTime = (int)gameTime.TotalGameTime.TotalSeconds;
                }

                if (player.crashed)
                {
                    //Save the crash event immediately after it happens
                    if (!restartCount)
                    {
                        TrafficRacerSaveGameData.SaveCrashEvent(gamedata_save_file_handle);
                    }
                    
                    restartCount = true;
                }

                // Restart Countdown
                if (restartCount)
                {
                    restartCountdown -= gameTime.ElapsedGameTime.TotalMilliseconds / 1000;
                    restartCountdownUI.Update((int)Math.Round(restartCountdown));
                    if (restartCountdown <= -1)
                    {
                        RestartGame(gameTime);
                    }
                }

                //Step forward time (slo-mo if player crashed or countdown finished)
                float timeScale = 1.0f;
                if (player.crashed || SecondsLeft <= 0)
                {
                    timeScale = 0.25f;
                }
                world.Step((float)gameTime.ElapsedGameTime.TotalSeconds * timeScale);

                //Detect if in target lane
                int lane = Road.GetLane(player.Position.X, 2.0f);
                inTargetLane = lane == environment.road.GetHighlightAtPlayerPos();
                environment.road.SetHighlightStatus(lane);

                //Update game stuff
                trafficManager.Update(gameTime, player, state);
                environment.Update(gameTime, player);
                player.Update(gameTime, InputManager.LateralMovement);
                
                //Countdown
                countdownUI.Update(Convert.ToInt32(SecondsLeft));
                if (SecondsLeft <= 0)
                {
                    EndGame();
                }
            }
            
            //Light & camera follow player
            Camera.main.Target = new Vector2(player.Position.X, player.Position.Y);
            Lighting.Position = new Vector3(Lighting.Position.X, player.Position.Y + 15, Lighting.Position.Z);

            //Update GUI
            bool center = GameManager.Update(gameTime, SecondsLeft, score);
            if (center)
            {
                NotifyDeviceRebaseline();
                InputManager.Exercise.ResetExercise();
                InputManager.VNS?.Flush_VNS_Buffers();
                player.ResetToCenter();
                TrafficRacerSaveGameData.SaveRebaselineEvent(gamedata_save_file_handle, this, InputManager.Exercise.RetrieveBaselineData());
            }
            
            fpsUI.Update(gameTime);
            titleUI.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            GraphicsDevice.Clear(Color.Black);

            //Update lights and camera
            Camera.main.Update();
            Lighting.Update();

            //Update graphics state
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = new RasterizerState() { CullMode = CullMode.CullClockwiseFace };

            //Render shadow map
            GraphicsDevice.SetRenderTarget(Lighting.ShadowMap);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1.0f, 0);
            effect.CurrentTechnique = effect.Techniques["ShadowMap"];

            environment.Render(gameTime, GraphicsDevice, effect);
            player.Render(gameTime, effect);
            trafficManager.RenderTraffic(gameTime, effect);
            
            //Render scene
            GraphicsDevice.RasterizerState = new RasterizerState() { CullMode = CullMode.CullCounterClockwiseFace };
            GraphicsDevice.SetRenderTarget(null);

            postProcessor.Begin();

            effect.CurrentTechnique = effect.Techniques["ShadowedScene"];
            environment.Render(gameTime, GraphicsDevice, effect);

            player.Render(gameTime, effect);
            trafficManager.RenderTraffic(gameTime, effect);

            postProcessor.End(player.crashed);
            
            //Render GUI
            spriteBatch.Begin();
            
            countdownUI.Render(spriteBatch);
            titleUI.Render(spriteBatch, gameTime);
            GameManager.Render(spriteBatch, inTargetLane);

            if (player.crashed && state != GameState.RESTARTING)
            {
                gameOverUI.Render(spriteBatch, gameTime);
            }

            if (restartCount) restartCountdownUI.Render(spriteBatch);

            /*
            spriteBatch.Draw(puck_texture, puck_texture_position, null, Color.White, puck_texture_rotation, puck_texture_origin, puck_texture_scale, SpriteEffects.None, 0);

            string angle_string = Convert.ToInt32(MathHelper.ToDegrees(puck_texture_rotation)).ToString();
            Vector2 angle_string_size = puck_font.MeasureString(angle_string);
            Vector2 angle_string_origin = angle_string_size / 2;
            spriteBatch.DrawString(puck_font, angle_string, puck_texture_position, Color.White, 0, angle_string_origin, 1.0f, SpriteEffects.None, 0);
            */

            spriteBatch.End();
            
            //Render debug
            if (TrafficGame.DEBUG)
            {
                trafficManager.RenderDebug(debugView, Camera.main.View, Camera.main.Projection);
                debugView.RenderDebugData(Camera.main.Projection, Camera.main.View, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, 0.8f);
                spriteBatch.Begin();
                fpsUI.Render(spriteBatch);
                spriteBatch.End();

                //DEBUG: show shadow map
                spriteBatch.Begin(0, BlendState.Opaque, SamplerState.AnisotropicClamp);
                spriteBatch.Draw(Lighting.ShadowMap, new Rectangle(0, GraphicsDevice.Viewport.Height - 256, 256, 256), Color.White);
                spriteBatch.End();
            }
        }

        // Calculate proper screen size for sprite rendering
        private void ResizeScreen()
        {
            graphicsManager.ToggleFullScreen();
            ScreenWidth = graphicsManager.PreferredBackBufferWidth;
            ScreenHeight = graphicsManager.PreferredBackBufferHeight;
            ScaledView = new Viewport();
            ScaledView.X = 0;
            ScaledView.Y = 0;
            ScaledView.Width = ScreenWidth;
            ScaledView.Height = ScreenHeight;
            graphicsManager.GraphicsProfile = GraphicsProfile.HiDef;
        }
    }

    public enum GameState
    {
        STARTING, RUNNING, RESTARTING, PAUSED, ERROR_ENCOUNTERED, ERROR_RECOVERED
    }
}
