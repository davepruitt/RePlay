using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using RePlay_Exercises;
using RePlay_VNS_Triggering;
using System.IO;
using Microsoft.AppCenter.Crashes;
using RePlay_Activity_Common;
using Microsoft.Xna.Framework.Input.Touch;

// NOTES: Want to be able to adjust Paddle width and sensetivity; Collect data (save exactly what the user is doing, and any events (like score, levels, bonuses etc.),
//        and output to a text file). Also want to keep track of how many days or time the user is playing the game.
//        Work on puck controls.

namespace RePlay_Activity_Breakout
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class BreakoutGame : RePlay_Game
    {
        public static int VirtualScreenWidth = 2560;
        public static int VirtualScreenHeight = 1600;

        #region Public variables

        public bool device_setup_success = false;
        public PCM_Manager PCM;
        public int ScreenWidth;
        public int ScreenHeight;
        public bool startOfLevel = true;
        public int Duration = 0;
        public double SecondsLeft = 0;
        public bool error_gate = false;

        #endregion

        #region Constants

        // Breakout's constant values
        public const int MAP_ROWS = 4;
        public const int MAP_COLS = 8;
        public const int BLOCK_WIDTH = 200;
        public const int BLOCK_HEIGHT = 80;
        public const double LAST_HIT_TIMEOUT = 20.0;
        public const double MAX_BALL_MULTIPLIER = 3.0;

        #endregion

        #region Private Properties

        private double tempGain = 1.0;
        private bool show_pcm_connection_status = false;
        public bool is_replay_debug_mode = false;
        private bool mute_game = true;

        private SpriteBatch SpriteBatch;
        public Paddle Paddle;
        private SoundEffect blockHitSFX,
                    paddleHitSFX,
                    wallHitSFX,
                    fireBallSFX,
                    powerUpSFX;

        private int ballWithPaddle;
        public int score = 0;
        public int level = 0;
        public int gameDifficulty;

        private float newLevelCounter = 0f; // controls will be disabled and level string displayed for a moment when a new level first loads
        private float powerUpChance; // % chance of dropping a powerup, set in CreateLevel
        private float powerUpTimer = 0; // used to prevent power-ups from dropping near-simultaneously

        public List<Block> blocks = new List<Block>();
        public List<Ball> balls = new List<Ball>();
        public List<PowerUp> powerUps = new List<PowerUp>();

        private GraphicsDeviceManager graphics;
        private Texture2D Background;
        private string BackgroundName = string.Empty;
        private Random rand = new Random();
        private SpriteFont font, countdown;
        private ExerciseBase Exercise;
        private VNSAlgorithm_Standard VNS;
        public RePlay_Game_GameplayUI GameManager;
        
        private GameState State = GameState.SETUP_FAILED;
        private DateTime StateStartTime = DateTime.MinValue;

        private ExerciseDeviceType Device;
        private ExerciseType ExerciseType;
        private string SubjectID;
        private string TabletID;
        private bool from_prescription;
        private BinaryWriter gamedata_save_file_handle;

        private BreakoutInstructions breakout_instructions;
        private Texture2D breakout_instructions_texture;

        // [background, ballSpeed, paddleWidth, availability of power-ups, blockLevel, durabilityOfBlocks, progressDifficulty 0 == false, 1 == true]
        private int[] levelParams = { 0, 1, 0, 50, 0, 1 }; // for now just setting the values here, but going to read them from a file or maybe command line

        private enum LevelParameter
        {
            Background = 0,    // 0 == Sky, 1 == underwater, 2 == space
            BallSpeed,         // 0 == slow, 1 == medium, 2 == fast
            PaddleWidth,       // 0 == largest Paddle, 1 == smaller Paddle, 2 == smallest Paddle
            PowerUpFrequency,  // power-up frequency is a percentage => 40 would be 40% chance of a block dropping a powerup
            BlockDurability,   //  0 == 1 hit to destroy blocks, 1 == 2 hits to destroy block, 2 == 3 hits to destroy block
            ProgressDifficulty // progressDifficulty => the game will slightly increase the speed of the ball, decrease paddlewidth, increase power-up frequency, 
                               // and make blocks more durable (up to two hits) if progressDifficulty is set to true
        }
        
        private enum GameState
        {
            START_SCREEN, SETUP_FAILED, ERROR_ENCOUNTERED, PAUSED, RUNNING, COMPLETED
        }

        VNSAlgorithmParameters vns_algorithm_parameters = new VNSAlgorithmParameters();
        private bool show_stim_icon = false;

        #endregion

        #region Constructor

        public BreakoutGame(GameLaunchParameters game_launch_parameters)
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.ToggleFullScreen();

            tempGain = game_launch_parameters.Gain;
            show_pcm_connection_status = game_launch_parameters.ShowPCMConnectionStatus;
            is_replay_debug_mode = game_launch_parameters.DebugMode;
            show_stim_icon = game_launch_parameters.ShowStimulationRequests;

            Content.RootDirectory = game_launch_parameters.ContentDirectory;
            Duration = Convert.ToInt32(game_launch_parameters.Duration);
            SecondsLeft = Duration * 60;
            gameDifficulty = game_launch_parameters.Difficulty - 1;
            Device = game_launch_parameters.Device;
            ExerciseType = game_launch_parameters.Exercise;
            SubjectID = game_launch_parameters.SubjectID;
            TabletID = game_launch_parameters.TabletID;
            from_prescription = game_launch_parameters.LaunchedFromPrescription;
            vns_algorithm_parameters = game_launch_parameters.VNS_AlgorithmParameters;
            vns_algorithm_parameters.NoiseFloor = (vns_algorithm_parameters.NoiseFloor * tempGain) / ExerciseBase.StandardExerciseSensitivity;

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

        #endregion

        #region RePlay_Game overrides

        public override bool ConnectToDevice()
        {
            bool success = Exercise.SetupDevice();
            return success;
        }

        public override void ContinueGame()
        {
            if (State == GameState.ERROR_ENCOUNTERED)
            {
                State = GameState.RUNNING;
            }
        }

        #endregion

        #region Monogame overrides

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            ResizeScreen();

            //Initially set the game state as "setup failed"
            State = GameState.SETUP_FAILED;
            StateStartTime = DateTime.Now;

            // Setup stuff
            device_setup_success = SetupExercise(Device, ExerciseType, SubjectID);
            if (device_setup_success)
            {
                try
                {
                    //Create a new sprite batch. This is used for all drawing.
                    SpriteBatch = new SpriteBatch(GraphicsDevice);

                    GameManager = new RePlay_Game_GameplayUI(this, GraphicsDevice.Viewport, 
                        PCM, show_pcm_connection_status, is_replay_debug_mode, true, 
                        show_stim_icon, vns_algorithm_parameters.Enabled);

                    Paddle = new Paddle(this, Exercise, VNS);
                    Paddle.SetPaddlePosition(VirtualScreenWidth / 2, VirtualScreenHeight - 160);

                    try
                    {
                        blockHitSFX = Content.Load<SoundEffect>("high_beep");
                        paddleHitSFX = Content.Load<SoundEffect>("low_beep");
                        wallHitSFX = Content.Load<SoundEffect>("mid_beep");
                        fireBallSFX = Content.Load<SoundEffect>("fireball_sound");
                        powerUpSFX = Content.Load<SoundEffect>("powerup");
                    }
                    catch (Exception load_sounds_exception)
                    {
                        Crashes.TrackError(load_sounds_exception, new Dictionary<string, string>() { { "Breakout", "Exception while loading audio content!" } });
                    }

                    font = Content.Load<SpriteFont>("Score");
                    countdown = Content.Load<SpriteFont>("countdown");

                    breakout_instructions = new BreakoutInstructions(GraphicsDevice, VirtualScreenWidth, VirtualScreenHeight);
                    breakout_instructions.LoadContent(Content);
                    breakout_instructions_texture = breakout_instructions.DrawBreakoutInstructions(GraphicsDevice, SpriteBatch, VirtualScreenWidth, VirtualScreenHeight);

                    SetDifficulty();
                    CreateLevel();
                    SpawnBall();
                    GameManager.LoadContent();

                    //Get build information
                    var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(Game.Activity);
                    var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
                    var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

                    //Initialize the save file
                    string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string subject_id = SubjectID;
                    string tablet_id = TabletID;
                    string game_name = "Breakout";
                    string exercise_name = ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType);
                    string file_name = subject_id + "_" + game_name + "_" + current_date_time_stamp + ".txt";

                    string game_file_name = subject_id + "_" + game_name + "_" + current_date_time_stamp + "_gamedata.txt";
                    gamedata_save_file_handle = Exercise_SaveData.OpenFileForSaving(
                        Game.Activity, 
                        game_file_name,
                        build_date,
                        version_name,
                        version_code,
                        tablet_id,
                        subject_id, 
                        game_name, 
                        exercise_name, 
                        ExerciseBase.StandardExerciseSensitivity, 
                        Exercise.Gain,
                        ExerciseBase.StandardExerciseSensitivity,
                        from_prescription,
                        vns_algorithm_parameters);

                    BreakoutSaveGameData.SaveMetaData(gamedata_save_file_handle, this);
                    BreakoutSaveGameData.SaveRebaselineEvent(gamedata_save_file_handle, this, Exercise.RetrieveBaselineData());
                    
                    State = GameState.START_SCREEN;
                    StateStartTime = DateTime.Now;
                }
                catch (Exception e)
                {
                    State = GameState.SETUP_FAILED;
                    StateStartTime = DateTime.Now;
                    Crashes.TrackError(e, new Dictionary<string, string>() { { "Breakout", "Exception while loading content!" } });
                }
            }

            NotifySetupCompleted(State == GameState.SETUP_FAILED ? false : true);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            SpriteBatch.Dispose();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            switch (State)
            {
                case GameState.START_SCREEN:

                    if (DateTime.Now >= StateStartTime + TimeSpan.FromSeconds(5.0))
                    {
                        State = GameState.RUNNING;
                        StateStartTime = DateTime.Now;
                    }

                    break;
                case GameState.PAUSED:
                case GameState.RUNNING:

                    //Update the game menu UI
                    bool center_game = GameManager.Update(gameTime, SecondsLeft, score);
                    if (center_game)
                    {
                        NotifyDeviceRebaseline();
                        Exercise.ResetExercise();
                        VNS.Flush_VNS_Buffers();
                        Paddle.SetPaddleHorizontalPosition(VirtualScreenWidth / 2);
                        BreakoutSaveGameData.SaveRebaselineEvent(gamedata_save_file_handle, this, Exercise.RetrieveBaselineData());
                    }

                    //Update whether the game is paused or running
                    State = (GameManager.Paused) ? GameState.PAUSED : GameState.RUNNING;

                    //If the game is running, then update the state of the game itself
                    if (State == GameState.RUNNING)
                    {
                        GameRunning(gameTime);
                        BreakoutSaveGameData.SaveCurrentGameData(gamedata_save_file_handle, this);
                    }

                    //If the game time has completed, update the state
                    if (SecondsLeft <= 0)
                    {
                        StateStartTime = DateTime.Now;
                        State = GameState.COMPLETED;
                    }

                    //If the user has requested to exit the game, do so.
                    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                    {
                        EndGame();
                        return;
                    }

                    break;
                case GameState.COMPLETED:

                    if (DateTime.Now >= (StateStartTime + TimeSpan.FromSeconds(3.0)))
                    {
                        EndGame();
                        return;
                    }

                    break;
                case GameState.SETUP_FAILED:
                    EndGame();
                    break;
                case GameState.ERROR_ENCOUNTERED:
                    break;
            }
        }

        private void GameRunning(GameTime gameTime)
        {
            powerUpTimer += 0.05f;

            SecondsLeft -= (double) gameTime.ElapsedGameTime.Milliseconds / 1000;

            if (Keyboard.GetState().IsKeyDown(Keys.Space) && !startOfLevel)
            {
                foreach (Ball b in balls)
                    b.IsPaddleBall = false;
            }

            // if it's been a while since the user hit a block, increase the ball size
            CheckForBallSizeIncrease();

            // keep track of how long the "Level X" string is on the screen, disable Paddle until it's gone
            newLevelCounter += 0.05f;

            if (newLevelCounter > 5f)
                startOfLevel = false;

            if (!startOfLevel)
            {
                try
                {
                    Paddle.Update((float)gameTime.ElapsedGameTime.TotalSeconds, this);
                }
                catch (Exception e)
                {
                    State = GameState.ERROR_ENCOUNTERED;
                    StateStartTime = DateTime.Now;
                    Crashes.TrackError(e, new Dictionary<string, string>() { { "Breakout", "Error attempting to update the paddle in Breakout!" } });
                    NotifyDeviceCommunicationError();
                }
            }
                
            // if ball is not launched, the position will be the same as the Paddle
            foreach (Ball b in balls)
            {
                if (b.IsActive && b.IsPaddleBall)
                {
                    var paddle_top_center = Paddle.PaddleTopCenterPosition;
                    b.position = new Vector2(paddle_top_center.X, paddle_top_center.Y - b.Radius * 2f);
                    b.Update((float)gameTime.ElapsedGameTime.TotalSeconds, this);
                }
                else
                    b.Update((float)gameTime.ElapsedGameTime.TotalSeconds, this);

                CheckCollisions(b);
            }

            // remove balls that have been lost
            for (int i = 0; i < balls.Count; i++)
            {
                if (!balls[i].IsActive)
                {
                    balls.RemoveAt(i);

                    if (score > 50)
                    {
                        score -= 5;
                    }

                    if (balls.Count == 0)
                        SpawnBall();
                }
            }

            // drop powerup and check if it collides with the player
            foreach (PowerUp p in powerUps)
            {
                Rectangle paddlePos = Paddle.BoundingRect;

                if (!p.shouldRemove)
                    p.Update((float)gameTime.ElapsedGameTime.TotalSeconds, this);

                if (paddlePos.Intersects(p.BoundingRect) && (!p.isActive))
                {
                    ActivatePowerUp(p);
                    score += 15;
                }
            }

            // remove powerups that have been collected or off the screen
            for (int i = powerUps.Count - 1; i >= 0; i--)
            {
                if (powerUps[i].shouldRemove)
                {
                    if (!powerUps[i].isActive)
                    {
                        string powerup_name = string.Empty;
                        if (powerUps[i].type == PowerUpType.FireBall)
                        {
                            powerup_name = "FireBall";
                        }
                        else if (powerUps[i].type == PowerUpType.MultiBall)
                        {
                            powerup_name = "MultiBall";
                        }
                        else
                        {
                            powerup_name = "WidenPaddle";
                        }

                        BreakoutSaveGameData.SavePowerUpMissed(gamedata_save_file_handle, powerup_name);
                    }
                    
                    powerUps.RemoveAt(i);
                }
            }

            if (blocks.Count == 0)
            {
                gameDifficulty++;
                if (gameDifficulty >= 10)
                {
                    gameDifficulty = 0;
                }

                ClearLevel();
                SetDifficulty();
                CreateLevel();
                SpawnBall();
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            //Clear the screen to be some background color
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            //If the game is running...
            if (State != GameState.SETUP_FAILED)
            {
                //Draw everything...
                var scaleX = (float)ScreenWidth / VirtualScreenWidth;
                var scaleY = (float)ScreenHeight / VirtualScreenHeight;
                var scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1.0f);

                if (State == GameState.START_SCREEN)
                {
                    var secondsRemaining = Convert.ToSingle(Math.Max(0, Math.Min(1, (StateStartTime + TimeSpan.FromSeconds(5.0) - DateTime.Now).TotalSeconds)));

                    SpriteBatch.Begin(transformMatrix: scaleMatrix);
                    SpriteBatch.Draw(breakout_instructions_texture, new Vector2(0, 0), Color.White * secondsRemaining);
                    SpriteBatch.End();
                }
                else
                {
                    SpriteBatch.Begin(transformMatrix: scaleMatrix);
                    SpriteBatch.Draw(Background, new Rectangle(0, 0, VirtualScreenWidth, VirtualScreenHeight), Color.White);

                    //Render all the blocks on the screen
                    foreach (Block b in blocks)
                    {
                        SpriteBatch.Draw(b.Texture, new Rectangle((int)b.position.X, (int)b.position.Y, (int)Block.BlockWidth, (int)Block.BlockHeight), Color.White);
                    }

                    if (State == GameState.PAUSED || State == GameState.RUNNING)
                    {
                        //If the game is either paused or running, also render the paddle and balls on the screen

                        Paddle.Draw(SpriteBatch);

                        foreach (Ball b in balls)
                        {
                            if (b.IsActive)
                                SpriteBatch.Draw(b.Texture, b.position, null, Color.White, 0f, Vector2.Zero, (float)Ball.Multiplier, SpriteEffects.None, 0f);
                        }

                        foreach (PowerUp p in powerUps)
                        {
                            if (!p.shouldRemove)
                                p.Draw(SpriteBatch);
                        }

                        if (Ball.isPaddleBall)
                        {
                            string countdown_string = String.Format("{0}", Paddle.LaunchTimer);
                            var countdown_string_size = countdown.MeasureString(countdown_string);
                            int countdown_xpos = Convert.ToInt32((VirtualScreenWidth / 2) - (countdown_string_size.X / 2));
                            int countdown_ypos = Convert.ToInt32((VirtualScreenHeight / 2) - (countdown_string_size.Y / 2));
                            SpriteBatch.DrawString(countdown, countdown_string, new Vector2(countdown_xpos, countdown_ypos), Color.White);
                        }
                    }
                    else if (State == GameState.COMPLETED)
                    {
                        string good_job_str = "Good job!";
                        Vector2 size_of_string = countdown.MeasureString(good_job_str);
                        SpriteBatch.DrawString(countdown, good_job_str, new Vector2((VirtualScreenWidth / 2) - (size_of_string.X / 2), (VirtualScreenHeight / 2) - (size_of_string.Y / 2)), Color.White);
                    }

                    SpriteBatch.End();

                    SpriteBatch.Begin();
                    GameManager.Render(SpriteBatch);
                    SpriteBatch.End();
                }

            }
        }

        public override void EndGame()
        {
            //Attempt to close down files and such gracefully
            try
            {
                BreakoutSaveGameData.CloseFile(gamedata_save_file_handle);
                Exercise.CloseFile();
                Exercise.Close();
                //VNS.CloseRecordingFile();
            }
            catch (Exception)
            {
                //empty
            }

            //Set the result of the activity, and close the game
            Activity.SetResult(Android.App.Result.Ok);
            Activity.Finish();
        }
        
        #endregion

        #region Private Methods

        // Instantiate correct exercise and open the device to receive exercise data
        private bool SetupExercise(ExerciseDeviceType device, ExerciseType type, string subject_id)
        {
            try
            {
                Exercise = ExerciseBase.InstantiateCorrectExerciseClass(type, Activity, tempGain);

                bool ready = Exercise.SetupDevice();
                if (ready)
                {
                    ready = false;
                    var start = DateTime.Now;
                    while (!ready || (DateTime.Now - start).TotalMilliseconds < 2000)
                    {
                        ready = Exercise.ResetExercise();
                    }

                    //Get build information
                    var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(Game.Activity);
                    var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
                    var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

                    Exercise.SetupFile(build_date,
                        version_name,
                        version_code,
                        "Breakout", 
                        ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(type), 
                        TabletID,
                        subject_id, 
                        from_prescription,
                        vns_algorithm_parameters);

                    PCM = new PCM_Manager(Activity);
                    VNS = new VNSAlgorithm_Standard();
                    VNS.Initialize_VNS_Algorithm(DateTime.Now, vns_algorithm_parameters);
                    PCM.PropertyChanged += (a, b) =>
                    {
                        //empty
                    };

                    PCM.PCM_Event += (a, b) =>
                    {
                        try
                        {
                            Exercise_SaveData.SaveMessageFromReStoreService(Exercise.DataSaver, b);
                        }
                        catch (Exception)
                        {
                            //empty
                        }
                    };

                    return true;
                }
                else
                {
                    Crashes.TrackError(new Exception("Device could not be setup!"), new Dictionary<string, string>() { { "Breakout", "Unable to set up device!" } });
                    return false;
                }
            }
            catch (Exception e)
            {
                Crashes.TrackError(e, new Dictionary<string, string>() { { "Breakout", "Exception encountered while attempting to set up device!" } });
                return false;
            }
        }

        private void PCM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        // Called to check for collision between the ball and blocks 
        private bool Intersects(double circle_x, double circle_y, double circle_r, double rect_x, double rect_y, double rect_width, double rect_height)
        {
            double circleDistance_x = Math.Abs(circle_x - rect_x);
            double circleDistance_y = Math.Abs(circle_y - rect_y);

            if (circleDistance_x > (rect_width/2 + circle_r)) { return false; }
            if (circleDistance_y > (rect_height/2 + circle_r)) { return false; }

            if (circleDistance_x <= (rect_width/2)) { return true; } 
            if (circleDistance_y <= (rect_height/2)) { return true; }

            double cornerDistance_sq = Math.Pow((circleDistance_x - rect_width/2), 2) + Math.Pow((circleDistance_y - rect_height/2), 2);

            return (cornerDistance_sq <= Math.Pow(circle_r, 2));
        }

        // Check if the ball has hit anything
        private void CheckCollisions(Ball ball)
        {
            var ball_rect = new Rectangle(Convert.ToInt32(ball.position.X - ball.Radius),
                Convert.ToInt32(ball.position.Y - ball.Radius),
                Convert.ToInt32(ball.Radius * 2),
                Convert.ToInt32(ball.Radius * 2));
            var paddle_rect = Paddle.PaddleRectangle;

            // Check for collision with the Paddle
            if (ballWithPaddle == 0 && ball_rect.Intersects(Paddle.PaddleRectangle) &&
                ball.direction.Y > 0)
            {
                if (!ball.IsPaddleBall)
                {   
                    if (paddleHitSFX != null && !mute_game)
                    {
                        paddleHitSFX.Play();
                    }
                    
                    score += 3;
                }

                // Reflect based on which part of the Paddle is hit

                // By default, set the normal to up
                    Vector2 normal = -1.0f * Vector2.UnitY;
                // ball.direction = -1.0f * Vector2.UnitY;       // Changing direction explicitly makes the ball more predictable

                // Distance from the leftmost to rightmost part of the Paddle
                float dist = Paddle.Width + ball.Radius * 2;

                // Where within this distance the ball is at
                float ballLocation = ball.position.X -
                    (Paddle.position.X - ball.Radius - Paddle.Width / 2);

                // Percent between leftmost and rightmost part of Paddle
                float pct = ballLocation / dist;

                if (pct <= 0.20f)                               // far left
                    normal = new Vector2(-0.196f, -0.981f);     
                else if (pct > 0.20f && pct <= 0.40f)           // left
                    normal = new Vector2(-0.096f, -0.981f);
                //   ball.direction = new Vector2(-1, -0.981f); 
                else if (pct > 0.40f && pct <= .60f)            // middle
                    normal = new Vector2(0, -0.981f);
                // ball.direction = new Vector2(1, -0.981f);
                else if (pct > .60f && pct <= .80)              // right
                    normal = new Vector2(0.096f, -0.981f);
                else                                            // far right
                    normal = new Vector2(0.196f, -0.981f);      

                    
                ball.direction = Vector2.Reflect(ball.direction, normal);

                // Fix the direction if it's too steep
                AngleCorrection(ball);

                // No collisions between ball and Paddle for 20 frames
                ballWithPaddle = 20;
            }
            else if (ballWithPaddle > 0)
            {
                ballWithPaddle--;
            }
            
            // Check for block collisions
            Block collidedBlock = null;
            
            foreach (Block b in blocks)
            {
                if (Intersects(ball.position.X, ball.position.Y, ball.Radius, b.position.X + Block.BlockWidth / 3, b.position.Y, Block.BlockWidth, Block.BlockHeight))
                {
                    collidedBlock = b;
                    break;
                }
            }

            if (collidedBlock != null)
            {
                Ball.LastBlockHit = DateTime.Now;

                if (!ball.IsFireBall)
                {
                    if (blockHitSFX != null && !mute_game)
                    {
                        blockHitSFX.Play();
                    }
                }
                else
                {
                    if (fireBallSFX != null && !mute_game)
                    {
                        fireBallSFX.Play();
                    }
                }

                int randNum = rand.Next(0, 100);

                if (randNum <= powerUpChance && (powerUps.Count <= 3) && powerUpTimer >= 3) //&& collidedBlock.durability < 1)  // max of 4 powerups dropped at a time
                {
                    DropPowerUp(collidedBlock.position);
                    powerUpTimer = 0;
                }
                      
                // Assume that if our Y is close to the top or bottom of the block,
                // we're colliding with the top or bottom
                if (!ball.IsFireBall)
                {
                    if ((ball.position.Y <
                        (collidedBlock.position.Y - Block.BlockHeight / 2)) ||
                        (ball.position.Y >
                        (collidedBlock.position.Y + Block.BlockHeight / 2)))
                    {
                        ball.direction.Y = -1.0f * ball.direction.Y;
                    }
                    else // otherwise, we have to be colliding from the sides
                    {
                        ball.direction.X = -1.0f * ball.direction.X;
                    }
                }

                // Now remove this block from the list, or damage block if durability >= 1
                if (collidedBlock.Durability < 1)
                {
                    blocks.Remove(collidedBlock);
                    score += 20;
                }
                else
                {
                    collidedBlock.Texture = new Block(++collidedBlock.type, this).Texture;
                    collidedBlock.Durability--;
                }

                //Save information about this ball-block collission
                BreakoutSaveGameData.SaveCollisionData(gamedata_save_file_handle, collidedBlock);
            }

            // Check walls
            if ((Math.Abs(ball.position.X) < ball.Radius) && (ball.direction.X < 0))
            {
                if (wallHitSFX != null && !mute_game)
                {
                    wallHitSFX.Play();
                }
                

                ball.direction.X = -1.0f * ball.direction.X;
            }
            else if (
                ((ball.position.X + ball.Radius) > VirtualScreenWidth) &&
                (ball.direction.X > 0)
                )
            {
                if (wallHitSFX != null && !mute_game)
                {
                    wallHitSFX.Play();
                }

                ball.direction.X = -1.0f * ball.direction.X;
            }
            else if (((Math.Abs(ball.position.Y) < ball.Radius) && (ball.direction.Y < 0)))
            {
                if (wallHitSFX != null && !mute_game)
                {
                    wallHitSFX.Play();
                }

                ball.direction.Y = -1.0f * ball.direction.Y;
            }
            else if (ball.position.Y > (VirtualScreenHeight + ball.Radius))
                ball.IsActive = false;

            // prevent low direction values that would slow the ball down too much
            if (!ball.IsPaddleBall)
            {
                if (ball.direction.Y <= 0.35f && ball.direction.Y >= 0)
                    ball.direction.Y += 0.2f;
                else if (ball.direction.Y >= -0.35f && ball.direction.Y <= 0)
                    ball.direction.Y -= 0.2f;
                if (ball.direction.X <= 0.35f && ball.direction.X >= 0)
                    ball.direction.X += 0.2f;
                else if (ball.direction.X >= -0.35f && ball.direction.X <= 0)
                    ball.direction.X -= 0.2f;
            }
        }

        // Adjust angle of ball if it is too steep
        private void AngleCorrection(Ball ball)
        {
            // Fix the direction if it's too steep
            float dotResult = Vector2.Dot(ball.direction, Vector2.UnitX);
            if (dotResult > 0.9f)
            {
                ball.direction = new Vector2(0.906f, -0.423f);
            }
            dotResult = Vector2.Dot(ball.direction, -Vector2.UnitX);
            if (dotResult > 0.9f)
            {
                ball.direction = new Vector2(-0.906f, -0.423f);
            }
            dotResult = Vector2.Dot(ball.direction, -Vector2.UnitY);
            if (dotResult > 0.9f)
            {
                // check if clockwise or counter-clockwise
                Vector3 crossResult = Vector3.Cross(new Vector3(ball.direction, 0),
                    -Vector3.UnitY);
                if (crossResult.Z < 0)
                {
                    ball.direction = new Vector2(0.423f, -0.906f);
                }
                else
                {
                    ball.direction = new Vector2(-0.423f, -0.906f);
                }
            }
        }

        // Recenter the paddle, and clear lists of all powerups, balls, and blocks
        private void ClearLevel()
        {
            for (int i = powerUps.Count - 1; i >= 0; i--)
                powerUps.RemoveAt(i);

            for (int i = balls.Count - 1; i >= 0; i--)
                balls.RemoveAt(i);

            for (int i = blocks.Count - 1; i >= 0; i--) // not necessary, but added this to maybe add the option of skipping levels
                blocks.RemoveAt(i);
            
            Paddle.SetPaddlePosition(VirtualScreenWidth / 2, VirtualScreenHeight - 160);

            BreakoutSaveGameData.SaveLevelFinish(gamedata_save_file_handle);
        }

        // Start a new level
        private void CreateLevel()
        {
            startOfLevel = true;
            newLevelCounter = 0;
            level++;
            int blockDurability = 0;
            bool progressDifficulty = false;

            // Set background
            switch (levelParams[(int)LevelParameter.Background])
            {
                case 0:
                    BackgroundName = "sky_background";
                    Background = Content.Load<Texture2D>("sky_background");
                    break;
                case 1:
                    BackgroundName = "underwater_background";
                    Background = Content.Load<Texture2D>("underwater_background");
                    break;
                case 2:
                    BackgroundName = "space_background";
                    Background = Content.Load<Texture2D>("space_background");
                    break;
            }

            // Set ball speed
            switch (levelParams[(int)LevelParameter.BallSpeed])
            {
                case 0:
                    Ball.DefaultSpeed = Ball.SLOW;
                    break;
                case 1:
                    Ball.DefaultSpeed = Ball.MED;
                    break;
                case 2:
                    Ball.DefaultSpeed = Ball.FAST;
                    break;
            }

            //Set other ball parameters
            Ball.LastBlockHit = DateTime.MinValue;
            Ball.LastMultiplierIncrease = DateTime.MinValue;
            Ball.Multiplier = 1.0;
            
            // Set other level parameters
            blockDurability = levelParams[(int)LevelParameter.BlockDurability];
            powerUpChance = levelParams[(int)LevelParameter.PowerUpFrequency];
            progressDifficulty = (levelParams[(int)LevelParameter.ProgressDifficulty] == 1) ? true : false;

            // Calculate starting position of the blocks
            int blockTypeInt = 0;
            int blockStartPosX = (VirtualScreenWidth - (MAP_COLS * BLOCK_WIDTH)) / 2;
            int blockStartPosY = VirtualScreenHeight / 8;

            // Create block map
            for (int i=0; i < MAP_ROWS; i++)
            {
                for (int j=0; j < MAP_COLS; j++)
                {
                    Block b = new Block((BlockType)blockTypeInt, this);
                    b.position = new Vector2(j * Block.BlockWidth + blockStartPosX, i * Block.BlockHeight + blockStartPosY);
                    b.Durability = blockDurability;
                    blocks.Add(b);
                }
                blockTypeInt += 3;
            }

            if (progressDifficulty)
            {
                // increase ball speed
                if (level != 1)
                    Ball.DefaultSpeed += level * 10;
                // shrink Paddle
                if (levelParams[(int)LevelParameter.PaddleWidth] != 2)
                    levelParams[(int)LevelParameter.PaddleWidth]++;
            }

            // rotate map backgrounds
            if (levelParams[(int)LevelParameter.Background] == 3)
                levelParams[(int)LevelParameter.Background] = 0;
            else
                levelParams[(int)LevelParameter.Background]++;

            //Save information about the start of a new level
            BreakoutSaveGameData.SaveLevelStart(gamedata_save_file_handle, this);
        }

        // Spawn balls
        private void SpawnBall()
        {
            Ball b = new Ball(this);

            //If the current level uses the "space background", then use the light colored ball for better contrast
            if (BackgroundName.Equals("space_background"))
            {
                b.TextureName = "ball_light";
            }

            b.LoadContent();
            b.Radius = (float)((b.Texture.Width / 2) * Ball.Multiplier);

            // level = 10; adjust to test ball speed at different levels

            if (balls.Count == 0)
            {
                var paddle_center_top = Paddle.PaddleTopCenterPosition;

                b.IsPaddleBall = true;
                b.position = new Vector2(paddle_center_top.X, paddle_center_top.Y - b.Radius * 2f);
            }
            else
            {
                b.IsPaddleBall = false;
                b.IsMultiBall = true;
                b.position = new Vector2(balls[0].position.X, balls[0].position.Y);

                // slightly change the directions that the multiballs are going to separate from the original ball
                if (balls.Count < 2)
                {
                    b.direction = new Vector2(balls[0].direction.X + .15f, balls[0].direction.Y);
                    b.Speed -= 25f; // temporarily slow down multiballs to prevent balls hitting the Paddle at the same time and phasing through (speed is corrected in Ball class)
                }

                else
                {
                    b.direction = new Vector2(balls[0].direction.X - .15f, balls[0].direction.Y);
                    b.Speed -= 40f;
                }
            }  
              
            balls.Add(b);
        }
        
        // Spawn a power up
        private void DropPowerUp(Vector2 blockPos)
        {
            int randNum = rand.Next(0, 100);
            PowerUpType pType = new PowerUpType();
            
            if (randNum <= 40)
                pType = PowerUpType.MultiBall;

            else if (randNum > 40 && randNum < 85)
                pType = PowerUpType.PaddleSizeIncrease;

            else if (randNum >= 85)
                pType = PowerUpType.FireBall;

            PowerUp p = new PowerUp(pType, this);
            p.position = blockPos;
            p.LoadContent();
            powerUps.Add(p);

            //Save information to the data file about this new power-up appearing.
            string powerup_name = string.Empty;
            if (p.type == PowerUpType.FireBall)
            {
                powerup_name = "FireBall";
            }
            else if (p.type == PowerUpType.MultiBall)
            {
                powerup_name = "MultiBall";
            }
            else
            {
                powerup_name = "WidenPaddle";
            }

            BreakoutSaveGameData.SavePowerUpAppeared(gamedata_save_file_handle, powerup_name);
        }
        
        // Determine the power up that should be activated
        private void ActivatePowerUp(PowerUp p, params Ball[] bList)
        {
            p.shouldRemove = true;
            p.isActive = true;

            if (powerUpSFX != null && !mute_game)
            {
                powerUpSFX.Play();
            }
            
            switch (p.type)
            {
                case PowerUpType.MultiBall:

                    BreakoutSaveGameData.SavePowerUpCapture(gamedata_save_file_handle, "MultiBall");

                    if (balls.Count <= 1)   // max of 3 balls
                    {
                        SpawnBall();
                        SpawnBall();
                    }
                    else if (balls.Count == 2)
                        SpawnBall();
                     break;
                case PowerUpType.PaddleSizeIncrease:

                    BreakoutSaveGameData.SavePowerUpCapture(gamedata_save_file_handle, "WidenPaddle");

                    Paddle.WidenPaddle();
                    break;
                case PowerUpType.FireBall:

                    BreakoutSaveGameData.SavePowerUpCapture(gamedata_save_file_handle, "FireBall");

                    balls[0].FireBallTimer = 0f;  

                    if (!balls[0].IsFireBall)
                    {
                        balls[0].Texture = Content.Load<Texture2D>("fireball");
                        balls[0].IsFireBall = true;
                    }
                    break;
            }
        }

        // Set the game difficulty according to levelParameters
        private void SetDifficulty()
        {
            Block.BlockWidth = BLOCK_WIDTH;
            Block.BlockHeight = BLOCK_HEIGHT;

            if (gameDifficulty == 0)
            {
                Paddle.MinPaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_00;
                Paddle.PaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_00;
                levelParams[(int)LevelParameter.Background] = 0;
                levelParams[(int)LevelParameter.BallSpeed] = 0;
                levelParams[(int)LevelParameter.PaddleWidth] = 0;
                levelParams[(int)LevelParameter.PowerUpFrequency] = 60;
                levelParams[(int)LevelParameter.BlockDurability] = 0;
                levelParams[(int)LevelParameter.ProgressDifficulty] = 0;
            }
            else if (gameDifficulty == 1)
            {
                Paddle.MinPaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_01;
                Paddle.PaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_01;
                levelParams[(int)LevelParameter.Background] = 0;
                levelParams[(int)LevelParameter.BallSpeed] = 1;
                levelParams[(int)LevelParameter.PaddleWidth] = 0;
                levelParams[(int)LevelParameter.PowerUpFrequency] = 55;
                levelParams[(int)LevelParameter.BlockDurability] = 0;
                levelParams[(int)LevelParameter.ProgressDifficulty] = 0;
            }
            else if (gameDifficulty == 2)
            {
                Paddle.MinPaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_02;
                Paddle.PaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_02;
                levelParams[(int)LevelParameter.Background] = 1;
                levelParams[(int)LevelParameter.BallSpeed] = 0;
                levelParams[(int)LevelParameter.PaddleWidth] = 0;
                levelParams[(int)LevelParameter.PowerUpFrequency] = 50;
                levelParams[(int)LevelParameter.BlockDurability] = 1;
                levelParams[(int)LevelParameter.ProgressDifficulty] = 0;
            }
            else if (gameDifficulty == 3)
            {
                Paddle.MinPaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_03;
                Paddle.PaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_03;
                levelParams[(int)LevelParameter.Background] = 1;
                levelParams[(int)LevelParameter.BallSpeed] = 1;
                levelParams[(int)LevelParameter.PaddleWidth] = 1;
                levelParams[(int)LevelParameter.PowerUpFrequency] = 45;
                levelParams[(int)LevelParameter.BlockDurability] = 1;
                levelParams[(int)LevelParameter.ProgressDifficulty] = 0;
            }
            else if (gameDifficulty == 4)
            {
                Paddle.MinPaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_04;
                Paddle.PaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_04;
                levelParams[(int)LevelParameter.Background] = 1;
                levelParams[(int)LevelParameter.BallSpeed] = 1;
                levelParams[(int)LevelParameter.PaddleWidth] = 1;
                levelParams[(int)LevelParameter.PowerUpFrequency] = 40;
                levelParams[(int)LevelParameter.BlockDurability] = 2;
                levelParams[(int)LevelParameter.ProgressDifficulty] = 0;
            }
            else if (gameDifficulty == 5)
            {
                Paddle.MinPaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_05;
                Paddle.PaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_05;
                levelParams[(int)LevelParameter.Background] = 2;
                levelParams[(int)LevelParameter.BallSpeed] = 2;
                levelParams[(int)LevelParameter.PaddleWidth] = 1;
                levelParams[(int)LevelParameter.PowerUpFrequency] = 35;
                levelParams[(int)LevelParameter.BlockDurability] = 2;
                levelParams[(int)LevelParameter.ProgressDifficulty] = 0;
            }
            else if (gameDifficulty == 6)
            {
                Paddle.MinPaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_06;
                Paddle.PaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_06;
                levelParams[(int)LevelParameter.Background] = 2;
                levelParams[(int)LevelParameter.BallSpeed] = 2;
                levelParams[(int)LevelParameter.PaddleWidth] = 1;
                levelParams[(int)LevelParameter.PowerUpFrequency] = 30;
                levelParams[(int)LevelParameter.BlockDurability] = 2;
                levelParams[(int)LevelParameter.ProgressDifficulty] = 0;
            }
            else if (gameDifficulty == 7)
            {
                Paddle.MinPaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_07;
                Paddle.PaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_07;
                levelParams[(int)LevelParameter.Background] = 0;
                levelParams[(int)LevelParameter.BallSpeed] = 2;
                levelParams[(int)LevelParameter.PaddleWidth] = 2;
                levelParams[(int)LevelParameter.PowerUpFrequency] = 25;
                levelParams[(int)LevelParameter.BlockDurability] = 2;
                levelParams[(int)LevelParameter.ProgressDifficulty] = 0;
            }
            else if (gameDifficulty == 8)
            {
                Paddle.MinPaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_08;
                Paddle.PaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_08;
                levelParams[(int)LevelParameter.Background] = 1;
                levelParams[(int)LevelParameter.BallSpeed] = 2;
                levelParams[(int)LevelParameter.PaddleWidth] = 2;
                levelParams[(int)LevelParameter.PowerUpFrequency] = 25;
                levelParams[(int)LevelParameter.BlockDurability] = 2;
                levelParams[(int)LevelParameter.ProgressDifficulty] = 0;
            }
            else if (gameDifficulty == 9)
            {
                Paddle.MinPaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_09;
                Paddle.PaddleWidth = (int)Paddle.PADDLE_WIDTHS.SIZE_09;
                levelParams[(int)LevelParameter.Background] = 2;
                levelParams[(int)LevelParameter.BallSpeed] = 2;
                levelParams[(int)LevelParameter.PaddleWidth] = 2;
                levelParams[(int)LevelParameter.PowerUpFrequency] = 25;
                levelParams[(int)LevelParameter.BlockDurability] = 2;
                levelParams[(int)LevelParameter.ProgressDifficulty] = 0;
            }
        }

        // Calculate proper screen size for sprite rendering
        private void ResizeScreen()
        {
            //GraphicsManager.ToggleFullScreen();
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            ScreenWidth = graphics.PreferredBackBufferWidth;
            ScreenHeight = graphics.PreferredBackBufferHeight;

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

            //GraphicsDevice.Viewport = new Viewport(x, y, width, height);
            GraphicsDevice.Viewport = new Viewport(x, y, ScreenWidth, ScreenHeight);
        }

        // Increase the size of the ball if it's been a while since a block was hit
        private void CheckForBallSizeIncrease()
        {
            if (Ball.LastBlockHit != DateTime.MinValue)
            {
                double timeDiff = (DateTime.Now - Ball.LastBlockHit).TotalSeconds;
                double ballDiff = Ball.LastMultiplierIncrease != DateTime.MinValue ? (DateTime.Now - Ball.LastMultiplierIncrease).TotalSeconds : 11.0;
                if (timeDiff >= LAST_HIT_TIMEOUT && Ball.Multiplier < MAX_BALL_MULTIPLIER && ballDiff >= 10.0)
                {
                    Ball.Multiplier += .25;
                    Ball.LastMultiplierIncrease = DateTime.Now;
                }
            }
        }

        #endregion

    }
}