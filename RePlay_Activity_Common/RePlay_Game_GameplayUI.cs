using System;
using System.Collections.Generic;
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
using Microsoft.Xna.Framework.Input.Touch;
using RePlay_VNS_Triggering;

namespace RePlay_Activity_Common
{
    public class RePlay_Game_GameplayUI
    {
        #region Private constants

        private const int MarginLeft = 30;
        private const int MarginTop = 10;
        private const int MarginRight = 30;

        private const string Stimulation_Icon_Asset = "stim_symbol";
        private const string Stimulation_Icon_NoStim_Asset = "stim_symbol_nostim";
        private const string PCM_Connected_Icon_Asset = "pcm_connected";
        private const string PCM_Connected_Icon_NoStim_Asset = "pcm_connected_nostim";
        private const string PCM_Disconnected_Icon_Asset = "pcm_disconnected";
        private const string PausedButton = "pause_button";
        private const string PlayButton = "play_button";
        private const string FontName = "GameFont";
        private const string DiagnosticsFontName = "DiagnosticsFont";
        private const int HeightToDraw = 80;
        private const int PauseWidth = 800;
        private const int PauseHeight = 500;

        private const string MenuAsset_State1 = "menu1";
        private const string MenuAsset_State2 = "menu2";
        private const string MenuAsset_State3 = "menu3";

        #endregion

        #region Private data members
        
        private RePlay_Activity_Common.PrivateClasses.MessageQueue score_messages = new RePlay_Activity_Common.PrivateClasses.MessageQueue();
        private List<PCM_DebugModeEvent_EventArgs> pcm_event_messages = new List<PCM_DebugModeEvent_EventArgs>();

        protected RePlay_Game Game;
        protected Viewport GameplayViewport;

        private Texture2D Stimulation_Icon_NoStim_Texture;
        private Texture2D Stimulation_Icon_Texture;
        private Texture2D PCM_Connected_NoStim_Texture;
        private Texture2D PCM_Connected_Texture;
        private Texture2D PCM_Disconnected_Texture;
        private Texture2D PauseButtonTexture;
        private Texture2D PlayButtonTexture;
        private Rectangle PlayAndPauseButtonRectangle;
        private Rectangle PCM_ConnectionStatus_Rectangle;

        protected Vector2 scorePosition;
        protected Vector2 timerPosition;
        protected Vector2 buttonPosition;
        protected Vector2 pcmConnectedIconPosition;
        protected Vector2 stimIconPosition;

        protected Vector2 scoreDimensions;
        protected Vector2 timerDimensions;

        protected int TimerMinutes;
        protected double TimerSeconds;
        protected int Score;
        private bool IsScoreVisible;
        private bool IsPCMConnectionStatusVisible;
        private bool IsDebugMode;
        protected SpriteFont Font;
        protected SpriteFont DiagnosticsFont;

        private Vector2 layered_text_offset = new Vector2(-3, 3);

        private Texture2D MenuTexture_State1;
        private Texture2D MenuTexture_State2;
        private Texture2D MenuTexture_State3;
        private int menu_state = 1;
        private Rectangle MenuRectangle;
        private Rectangle Menu_ReturnToGame_Rect;
        private Rectangle Menu_GoToNextGame_Rect;

        private PCM_Manager pcm_manager;

        private int DiagnosticsStartX;
        private int DiagnosticsStartY;
        private TimeSpan DiagnosticsRetainerTimeSpan = TimeSpan.FromSeconds(15.0);

        private bool IsStimulationEnabled = false;
        private bool IsStimulationReal = false;
        private bool IsStimulationIconEnabled = false;
        private bool IsStimulationIconVisible = false;
        private TimeSpan StimulationIconStartTime = TimeSpan.Zero;
        private TimeSpan StimulationIconDuration = TimeSpan.FromSeconds(1.0);
        private TimeSpan CurrentTotalElapsedGameTime = TimeSpan.Zero;

        #endregion

        #region Public members

        public bool Paused = false;

        public event EventHandler<RePlayGamePauseMenuItemPressedEventArgs> PauseMenuItemPressed;
        public event EventHandler<EventArgs> PauseButtonPressed;
        public event EventHandler<EventArgs> ScreenPressed;

        #endregion

        #region Constructor

        public RePlay_Game_GameplayUI (RePlay_Game game, Viewport v, 
            PCM_Manager pcmManager, bool show_pcm_connection_status, bool debug_mode, 
            bool show_score, bool is_stimulation_icon_enabled, bool is_stimulation_enabled)
        {
            Game = game;
            GameplayViewport = v;
            IsScoreVisible = show_score;
            IsPCMConnectionStatusVisible = show_pcm_connection_status;
            IsStimulationIconEnabled = is_stimulation_icon_enabled;
            IsStimulationEnabled = is_stimulation_enabled;
            IsDebugMode = debug_mode;

            DiagnosticsStartX = GameplayViewport.Width - 500;
            DiagnosticsStartY = 200;

            pcm_manager = pcmManager;
            //pcm_manager.PCM_Event += HandlePCMEvent;
        }

        #endregion

        #region Methods

        private void HandlePCMEvent(object sender, PCM_DebugModeEvent_EventArgs e)
        {
            pcm_event_messages.Add(e);
        }

        public void Handle_MenuItem_Pressed (TouchCollection touch)
        {
            if (touch.Count > 0 && Paused)
            {
                Rectangle touchRect = new Rectangle((int)touch[0].Position.X, (int)touch[0].Position.Y, 1, 1);

                if (touch[0].State == TouchLocationState.Pressed)
                {
                    if (touchRect.Intersects(Menu_ReturnToGame_Rect))
                    {
                        menu_state = 2;
                    }
                    else if (touchRect.Intersects(Menu_GoToNextGame_Rect))
                    {
                        menu_state = 3;
                    }
                    else
                    {
                        menu_state = 1;
                    }
                }
                else if (touch[0].State == TouchLocationState.Released)
                {
                    menu_state = 1;

                    if (touchRect.Intersects(Menu_ReturnToGame_Rect))
                    {
                        //If anyone has subscribed for notifications, fire one off
                        RePlayGamePauseMenuItemPressedEventArgs eventArgs = new RePlayGamePauseMenuItemPressedEventArgs()
                        {
                            MenuItemIndex = 1,
                            MenuItemName = "Return to game"
                        };

                        PauseMenuItemPressed?.Invoke(this, eventArgs);

                        Paused = false;
                    }
                    else if (touchRect.Intersects(Menu_GoToNextGame_Rect))
                    {
                        //If anyone has subscribed for notifications, fire one off
                        RePlayGamePauseMenuItemPressedEventArgs eventArgs = new RePlayGamePauseMenuItemPressedEventArgs()
                        {
                            MenuItemIndex = 1,
                            MenuItemName = "Return to game"
                        };

                        PauseMenuItemPressed?.Invoke(this, eventArgs);

                        Game.EndGame();
                    }
                }
            }
        }

        public bool Handle_PauseButton_Pressed (TouchCollection touch)
        {
            if (touch.Count > 0 && !Paused)
            {
                Rectangle touchRect = new Rectangle((int)touch[0].Position.X, (int)touch[0].Position.Y, 1, 1);

                if (touchRect.Intersects(PlayAndPauseButtonRectangle) && touch[0].State == TouchLocationState.Pressed)
                {
                    //If anyone has subscribed for notifications, fire one off
                    PauseButtonPressed?.Invoke(this, new EventArgs());

                    //Set "Paused" to true
                    Paused = true;

                    //Invoke the "screen pressed" event
                    ScreenPressed?.Invoke(this, new EventArgs());

                    //Return true, indicating to the caller that the pause button was pressed
                    return true;
                }
            }

            //Return false, indicating that no button press was detected
            return false;
        }

        #endregion

        #region Corresponding MonoGame methods

        /// <summary>
        /// Loads the textures for the menu UI
        /// </summary>
        public virtual void LoadContent()
        {
            MenuTexture_State1 = Game.Content.Load<Texture2D>(MenuAsset_State1);
            MenuTexture_State2 = Game.Content.Load<Texture2D>(MenuAsset_State2);
            MenuTexture_State3 = Game.Content.Load<Texture2D>(MenuAsset_State3);

            Vector2 MenuRectanglePosition = new Vector2(GameplayViewport.Width / 2 - MenuTexture_State1.Width / 2, GameplayViewport.Height / 2 - MenuTexture_State1.Height / 2);
            MenuRectangle = new Rectangle((int)MenuRectanglePosition.X, (int)MenuRectanglePosition.Y, MenuTexture_State1.Width, MenuTexture_State1.Height);
            Menu_ReturnToGame_Rect = new Rectangle(MenuRectangle.X, MenuRectangle.Y + 170, 802, 200);
            Menu_GoToNextGame_Rect = new Rectangle(MenuRectangle.X, MenuRectangle.Y + 371, 802, 230);

            Font = Game.Content.Load<SpriteFont>(FontName);
            DiagnosticsFont = Game.Content.Load<SpriteFont>(DiagnosticsFontName);
            PauseButtonTexture = Game.Content.Load<Texture2D>(PausedButton);
            PlayButtonTexture = Game.Content.Load<Texture2D>(PlayButton);
            CreatePauseButton();

            PCM_Connected_Texture = Game.Content.Load<Texture2D>(PCM_Connected_Icon_Asset);
            PCM_Connected_NoStim_Texture = Game.Content.Load<Texture2D>(PCM_Connected_Icon_NoStim_Asset);
            PCM_Disconnected_Texture = Game.Content.Load<Texture2D>(PCM_Disconnected_Icon_Asset);
            Stimulation_Icon_Texture = Game.Content.Load<Texture2D>(Stimulation_Icon_Asset);
            Stimulation_Icon_NoStim_Texture = Game.Content.Load<Texture2D>(Stimulation_Icon_NoStim_Asset);
        }

        public void AddMessage (string msg)
        {
            RePlay_Activity_Common.PrivateClasses.Message m = 
                new RePlay_Activity_Common.PrivateClasses.Message(msg);
            m.Offset = new Vector2(100, 150);

            score_messages.Enqueue(m);
        }

        public void AddScoreMessage (int amount)
        {
            RePlay_Activity_Common.PrivateClasses.Message m = new RePlay_Activity_Common.PrivateClasses.Message(String.Format("+{0:n0}", amount));
            m.Offset = new Vector2(100, 150);

            score_messages.Enqueue(m);
        }

        public void DisplayStimulationIcon (bool real_stim)
        {
            DisplayStimulationIcon(real_stim, TimeSpan.FromSeconds(1.0));
        }

        public void DisplayStimulationIcon (bool real_stim, TimeSpan duration)
        {
            if (IsStimulationIconEnabled)
            {
                IsStimulationReal = real_stim;
                IsStimulationIconVisible = true;
                StimulationIconStartTime = CurrentTotalElapsedGameTime;
                StimulationIconDuration = duration;
            }
        }

        public virtual bool Update(GameTime time, double secondsRemaining, int score, TouchCollection touchCollection)
        {
            CurrentTotalElapsedGameTime = time.TotalGameTime;
            if (IsStimulationIconVisible)
            {
                if (CurrentTotalElapsedGameTime >= (StimulationIconStartTime + StimulationIconDuration))
                {
                    IsStimulationIconVisible = false;
                }
            }

            //If the game is paused, then check to see if a menu item has been pressed.
            if (Paused)
            {
                Handle_MenuItem_Pressed(touchCollection);
            }
            else
            {
                //Eliminate old diagnostics messages
                pcm_event_messages = pcm_event_messages.Where(x => 
                    DateTime.Now < (x.MessageTimestamp + DiagnosticsRetainerTimeSpan)).ToList();
                
                //If the game is not currently paused...
                score_messages.Update(time);

                //Check to see if the pause button was pressed.
                TimeSpan ts = TimeSpan.FromSeconds(secondsRemaining);
                TimerMinutes = ts.Minutes;
                TimerSeconds = ts.Seconds;
                Score = score;

                bool pause_button_touched = Handle_PauseButton_Pressed(touchCollection);
                if (!pause_button_touched)
                {
                    if (touchCollection.Count > 0)
                    {
                        if (touchCollection[0].State == TouchLocationState.Pressed)
                        {
                            //If the pause button was pressed, return "true" to the caller
                            return true;
                        }
                    }
                }
            }

            //Return false to the caller if the "pause" button was not pressed
            return false;
        }

        public virtual bool Update(GameTime time, double secondsRemaining, int score)
        {
            try
            {
                TouchCollection touchCollection = TouchPanel.GetState();
                return Update(time, secondsRemaining, score, touchCollection);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public virtual void Render(SpriteBatch spriteBatch, bool highlight_score = false)
        {
            int viewportWidth = GameplayViewport.Width;
            int viewportHeight = GameplayViewport.Height;

            //Define and measure the score string
            string scoreString = String.Format("Score: {0:#,###0}", Score);
            scoreDimensions = Font.MeasureString(scoreString);
            var half_score_height = scoreDimensions.Y / 2;

            //Define and measure the timer string
            string timeString = (TimerMinutes < 0) ? 
                "Game Over" : 
                "Time: " + String.Format(
                    "{0}:{1}", 
                    (int)Math.Max(0, TimerMinutes),
                    Math.Max(0, TimerSeconds).ToString("00"));
            timerDimensions = Font.MeasureString(timeString);
            var half_timer_height = timerDimensions.Y / 2;

            //Get half the pause button height
            var half_pause_button_height = PauseButtonTexture.Height / 2;

            //Let's figure out the height of the tallest item that we are rendering
            var tallest_height = Math.Max(Math.Max(scoreDimensions.Y, timerDimensions.Y), PauseButtonTexture.Height);
            var half_tallest_height = tallest_height / 2;
            
            //Now determine the position of each item
            scorePosition = new Vector2(MarginLeft, MarginTop + half_tallest_height - half_score_height);
            timerPosition = new Vector2((viewportWidth - MarginRight) - PauseButtonTexture.Width - MarginRight - timerDimensions.X, MarginTop + half_tallest_height - half_timer_height);
            buttonPosition = new Vector2((viewportWidth - MarginRight) - PauseButtonTexture.Width, MarginTop + half_tallest_height - half_pause_button_height);
            pcmConnectedIconPosition = new Vector2(timerPosition.X - (PCM_Connected_Texture.Width / 2) - MarginRight, 
                MarginTop + half_tallest_height - (PCM_Connected_Texture.Height / 4));
            stimIconPosition = new Vector2(pcmConnectedIconPosition.X - (Stimulation_Icon_Texture.Width / 2) - MarginRight, MarginTop + half_tallest_height - (Stimulation_Icon_Texture.Height / 4));

            // Draw Score
            if (IsScoreVisible)
            {
                Color scoreColor = Color.White;
                if (highlight_score)
                {
                    scoreColor = Color.Gold;
                }

                //Draw the score text slightly offset - this creates a layered effect, then draw the actual text on top of that
                spriteBatch.DrawString(Font, scoreString, scorePosition - layered_text_offset, Color.Black);
                spriteBatch.DrawString(Font, scoreString, scorePosition, scoreColor);

                foreach (RePlay_Activity_Common.PrivateClasses.Message m in score_messages)
                {
                    spriteBatch.DrawString(Font, m.Text, scorePosition + m.Offset + layered_text_offset, Color.Black);
                    spriteBatch.DrawString(Font, m.Text, scorePosition + m.Offset, Color.Gold);
                }
            }

            // Draw Timer
            spriteBatch.DrawString(Font, timeString, timerPosition - layered_text_offset, Color.Black);
            spriteBatch.DrawString(Font, timeString, timerPosition, Color.White);

            // Determine the dimensions for the play/pause button
            PlayAndPauseButtonRectangle = new Rectangle(Convert.ToInt32(buttonPosition.X), Convert.ToInt32(buttonPosition.Y), PauseButtonTexture.Width, PauseButtonTexture.Height);

            // Determine the dimensions and position for the PCM connection status icon
            PCM_ConnectionStatus_Rectangle = new Rectangle(Convert.ToInt32(pcmConnectedIconPosition.X), Convert.ToInt32(pcmConnectedIconPosition.Y), PCM_Connected_Texture.Width, PCM_Connected_Texture.Height);
            if (IsPCMConnectionStatusVisible)
            {
                if (pcm_manager.IsConnectedToPCM)
                {
                    if (IsStimulationEnabled)
                    {
                        spriteBatch.Draw(PCM_Connected_Texture, pcmConnectedIconPosition, null, Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
                    }
                    else
                    {
                        spriteBatch.Draw(PCM_Connected_NoStim_Texture, pcmConnectedIconPosition, null, Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
                    }   
                }
                else
                {
                    spriteBatch.Draw(PCM_Disconnected_Texture, pcmConnectedIconPosition, null, Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
                }
            }

            //Draw the stimulation icon
            if (IsStimulationIconVisible)
            {
                if (IsStimulationReal)
                {
                    spriteBatch.Draw(Stimulation_Icon_Texture, stimIconPosition, null, Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
                }
                else
                {
                    spriteBatch.Draw(Stimulation_Icon_NoStim_Texture, stimIconPosition, null, Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
                }
            }

            //Draw the diagnostics stuff if necessary
            /*if (IsDebugMode)
            {
                var currentyval = DiagnosticsStartY;
                foreach (var msg in pcm_event_messages)
                {
                    spriteBatch.DrawString(DiagnosticsFont, msg.PrimaryMessage, new Vector2(DiagnosticsStartX, currentyval), 
                        Color.White);
                    
                    foreach (var kvp in msg.SecondaryMessages)
                    {
                        currentyval += 20;
                        spriteBatch.DrawString(DiagnosticsFont, kvp.Key, new Vector2(DiagnosticsStartX, currentyval),
                            Color.White);
                        currentyval += 20;
                        spriteBatch.DrawString(DiagnosticsFont, kvp.Value, new Vector2(DiagnosticsStartX + 50, currentyval),
                            Color.White);
                    }

                    currentyval += 50;
                }
            }*/
            
            //If the game has been paused....
            if (Paused)
            {
                //Render the pause menu
                RenderPauseMenu(spriteBatch);

                //Render the play button
                spriteBatch.Draw(PlayButtonTexture, PlayAndPauseButtonRectangle, Color.White);
            }
            else
            {
                //Render the pause button
                spriteBatch.Draw(PauseButtonTexture, PlayAndPauseButtonRectangle, Color.White);
            }
        }

        #endregion

        #region Private Methods

        private void CreatePauseButton()
        {
            var buttonPosition = new Vector2(GameplayViewport.Width - 200, HeightToDraw);
            PlayAndPauseButtonRectangle = new Rectangle((int)buttonPosition.X, (int)buttonPosition.Y, PauseButtonTexture.Width, PauseButtonTexture.Height);
        }

        private void RenderPauseMenu(SpriteBatch spriteBatch)
        {
            Texture2D menu_texture_to_draw = MenuTexture_State1;
            if (menu_state == 2)
            {
                menu_texture_to_draw = MenuTexture_State2;
            }
            else if (menu_state == 3)
            {
                menu_texture_to_draw = MenuTexture_State3;
            }

            spriteBatch.Draw(menu_texture_to_draw, MenuRectangle, Color.White);
        }

        #endregion
    }
}