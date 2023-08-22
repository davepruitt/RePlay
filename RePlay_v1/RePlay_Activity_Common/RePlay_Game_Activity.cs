using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using OxyPlot.Xamarin.Android;
using RePlay_Exercises;
using RePlay_VNS_Triggering;

namespace RePlay_Activity_Common
{
    public class RePlay_Game_Activity : AndroidGameActivity
    {
        #region Protected data members

        protected FrameLayout MainGameLayout;
        public LinearLayout GameGraphingLayout;
        public RePlay_Game_Chart game_signal_chart;
        public RePlay_Game_VNS_Chart vns_signal_chart;

        #endregion

        #region Private data members

        private ImageView SplashScreenView;
        private LinearLayout ErrorMenuLayout;
        private LinearLayout CenteringLayout;
        private bool is_centering_layout_visible = false;

        #endregion

        #region Overriden methods

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            MainGameLayout = new FrameLayout(ApplicationContext);

            /* CREATE THE CENTERING LAYOUT */

            //Create a linear layout and place the text view inside of it
            CenteringLayout = new LinearLayout(ApplicationContext);
            CenteringLayout.SetBackgroundColor(Android.Graphics.Color.Argb(200, 40, 40, 40));
            CenteringLayout.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
            CenteringLayout.SetPadding(100, 100, 100, 100);
            CenteringLayout.SetGravity(GravityFlags.Center);
            CenteringLayout.SetForegroundGravity(GravityFlags.Center);
            CenteringLayout.Orientation = Orientation.Vertical;

            //Create a text view to indicate to the user that there is an error
            TextView rebaselineText = new TextView(ApplicationContext);
            rebaselineText.Text = "CENTERING";
            rebaselineText.TextSize = 72;
            rebaselineText.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
            rebaselineText.SetTextColor(Android.Graphics.Color.White);
            rebaselineText.SetForegroundGravity(GravityFlags.Center);
            rebaselineText.Gravity = GravityFlags.Center;
            rebaselineText.TextAlignment = TextAlignment.Center;

            CenteringLayout.AddView(rebaselineText);

            /* END OF CREATING THE CENTERING LAYOUT */

            /* CREATE THE GRAPHING LAYOUT */

            int scr_width = 2560;
            int scr_height = 1600;
            int width = 900;
            int height = 500;
            int padding = 100;

            GameGraphingLayout = new LinearLayout(ApplicationContext);
            GameGraphingLayout.SetBackgroundColor(Android.Graphics.Color.Argb(0, 0, 0, 0));
            GameGraphingLayout.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
            GameGraphingLayout.SetPadding(scr_width - width - padding, scr_height - height - padding, padding, padding);
            GameGraphingLayout.Orientation = Orientation.Horizontal;
            GameGraphingLayout.Visibility = ViewStates.Invisible;

            PlotView game_plot_view = new PlotView(ApplicationContext);
            game_plot_view.LayoutParameters = new ViewGroup.LayoutParams(width / 2, height);
            game_plot_view.SetBackgroundColor(Android.Graphics.Color.Argb(128, 255, 255, 255));
            game_signal_chart = new RePlay_Game_Chart(game_plot_view);

            PlotView vns_plot_view = new PlotView(ApplicationContext);
            vns_plot_view.LayoutParameters = new ViewGroup.LayoutParams(width / 2, height);
            vns_plot_view.SetBackgroundColor(Android.Graphics.Color.Argb(128, 255, 255, 255));
            vns_signal_chart = new RePlay_Game_VNS_Chart(vns_plot_view);

            GameGraphingLayout.AddView(game_plot_view);
            GameGraphingLayout.AddView(vns_plot_view);

            /* END OF GRAPHING LAYOUT */
        }

        #endregion

        #region Methods

        protected GameLaunchParameters ParseIntentData ()
        {
            string game_launch_params_json = Intent.GetStringExtra("game_launch_parameters_json");

            GameLaunchParameters result = new GameLaunchParameters();
            try
            {
                result = JsonConvert.DeserializeObject<GameLaunchParameters>(game_launch_params_json);
            }
            catch (Exception ex)
            {
                //empty
            }

            return result;
        }

        // Create splash screen
        protected void ShowSplashScreen ()
        {
            if (MainGameLayout != null)
            {
                // Use the splash screen image to cover up the black/blank screen while Android/MonoGame finish initializing.
                // "splash" should be set to the name of your splash screen image
                int id = Resources.GetIdentifier("splash", "drawable", PackageName);
                SplashScreenView = new ImageView(ApplicationContext);
                SplashScreenView.SetBackgroundResource(id);
                MainGameLayout.AddView(SplashScreenView);
            }
        }

        protected void AddGameToView (RePlay_Game g)
        {
            if (MainGameLayout != null)
            {
                MainGameLayout.AddView((View)g.Services.GetService(typeof(View)));
                MainGameLayout.AddView(GameGraphingLayout);
            }
        }

        protected void SubscribeToGameEvents (RePlay_Game g)
        {
            if (g != null)
            {
                g.SetupCompleted += Handle_RePlayGame_SetupCompletedEvent;
                g.DeviceCommunicationError += Handle_RePlayGame_DeviceCommunicationErrorEvent;
                g.DeviceRebaseline += Handle_Device_Rebaseline;
            }
        }

        private async void Handle_Device_Rebaseline(object sender, EventArgs e)
        {
            if (!is_centering_layout_visible)
            {
                is_centering_layout_visible = true;

                await Task.Run(async () =>
                {
                    this.RunOnUiThread(() =>
                    {
                        MainGameLayout.AddView(CenteringLayout);
                    });
                    await Task.Delay(500);
                    this.RunOnUiThread(() =>
                    {
                        MainGameLayout.RemoveView(CenteringLayout);
                    });
                });

                is_centering_layout_visible = false;
            }
        }

        protected void Handle_RePlayGame_DeviceCommunicationErrorEvent(object sender, EventArgs e)
        {
            if (MainGameLayout != null)
            {
                int txbdc_button_foreground_rid = Resources.GetIdentifier("TxBDC_Button_Foreground_Selector", "drawable", PackageName);
                int txbdc_button_background_rid = Resources.GetIdentifier("TxBDC_Button_Background_Selector", "drawable", PackageName);

                //Create a text view to indicate to the user that there is an error
                TextView errorText = new TextView(ApplicationContext);
                errorText.Text = "We are sorry, but we have lost communication with the game controller device. If you unplugged it, please plug it back in. Press continue below to reconnect to the device and continue your exercise.";
                errorText.TextSize = 24;
                errorText.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
                errorText.SetTextColor(Android.Graphics.Color.White);
                errorText.SetForegroundGravity(GravityFlags.Center);
                errorText.Gravity = GravityFlags.Center;
                errorText.TextAlignment = TextAlignment.Center;

                //Create 2 buttons: one to attempt to continue the game, and the other to quit the game
                Button continueButton = new Button(ApplicationContext);
                continueButton.Text = "Continue exercise";
                continueButton.TextAlignment = TextAlignment.Center;
                LinearLayout.LayoutParams continueButtonLayoutParams = new LinearLayout.LayoutParams(700, 300);
                continueButtonLayoutParams.SetMargins(100, 100, 100, 100);
                continueButton.LayoutParameters = continueButtonLayoutParams;
                continueButton.Enabled = true;

                continueButton.Click += (s, evArgs) =>
                {
                    RePlay_Game senderGame = sender as RePlay_Game;
                    if (senderGame != null)
                    {
                        bool connectionSuccess = senderGame.ConnectToDevice();
                        if (connectionSuccess)
                        {
                            senderGame.ContinueGame();
                            if (ErrorMenuLayout != null)
                            {
                                MainGameLayout.RemoveView(ErrorMenuLayout);
                            }
                        }
                    }
                };

                if (txbdc_button_foreground_rid != 0 && txbdc_button_background_rid != 0)
                {
                    continueButton.SetBackgroundResource(txbdc_button_foreground_rid);
                    continueButton.SetTextAppearance(txbdc_button_background_rid);
                }
                
                Button quitButton = new Button(ApplicationContext);
                quitButton.Text = "Exit this exercise";
                quitButton.TextAlignment = TextAlignment.Center;
                LinearLayout.LayoutParams quitButtonLayoutParams = new LinearLayout.LayoutParams(700, 300);
                quitButtonLayoutParams.SetMargins(100, 100, 100, 100);
                quitButton.LayoutParameters = quitButtonLayoutParams;
                quitButton.Enabled = true;

                quitButton.Click += (s, evArgs) =>
                {
                    RePlay_Game senderGame = sender as RePlay_Game;
                    if (senderGame != null)
                    {
                        senderGame.EndGame();
                    }
                };

                if (txbdc_button_foreground_rid != 0 && txbdc_button_background_rid != 0)
                {
                    quitButton.SetBackgroundResource(txbdc_button_foreground_rid);
                    quitButton.SetTextAppearance(txbdc_button_background_rid);
                }

                //Create a horizontal linear layout to hold the buttons
                LinearLayout buttonLayout = new LinearLayout(ApplicationContext);
                buttonLayout.LayoutParameters = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
                buttonLayout.Orientation = Orientation.Horizontal;
                buttonLayout.AddView(continueButton);
                buttonLayout.AddView(quitButton);

                //Create a linear layout and place the text view inside of it
                ErrorMenuLayout = new LinearLayout(ApplicationContext);
                ErrorMenuLayout.SetBackgroundColor(Android.Graphics.Color.Argb(200, 40, 40, 40));
                ErrorMenuLayout.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
                ErrorMenuLayout.SetPadding(100, 100, 100, 100);
                ErrorMenuLayout.SetGravity(GravityFlags.Center);
                ErrorMenuLayout.SetForegroundGravity(GravityFlags.Center);
                ErrorMenuLayout.Orientation = Orientation.Vertical;

                ErrorMenuLayout.AddView(errorText);
                ErrorMenuLayout.AddView(buttonLayout);

                //Place the linear layout in our main frame layout
                MainGameLayout.SetForegroundGravity(GravityFlags.Center);
                MainGameLayout.AddView(ErrorMenuLayout);
            }
        }
        
        protected void Handle_RePlayGame_SetupCompletedEvent(object sender, RePlayGameSetupCompletedEventArgs e)
        {
            if (MainGameLayout != null && SplashScreenView != null)
            {
                if (e.Successful)
                {
                    MainGameLayout.RemoveView(SplashScreenView);
                }
                else
                {
                    LinearLayout error_layout = new LinearLayout(ApplicationContext);
                    error_layout.SetBackgroundColor(Android.Graphics.Color.Argb(255, 0, 0, 0));
                    error_layout.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
                    error_layout.SetGravity(GravityFlags.Center);
                    error_layout.SetForegroundGravity(GravityFlags.Center);
                    error_layout.Orientation = Orientation.Vertical;

                    TextView error_text = new TextView(ApplicationContext);
                    error_text.Text = "There was an error setting up the game. Please restart the game.";
                    error_text.TextSize = 36;
                    error_text.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
                    error_text.SetTextColor(Android.Graphics.Color.White);
                    error_text.SetForegroundGravity(GravityFlags.Center);
                    error_text.Gravity = GravityFlags.Center;
                    error_text.TextAlignment = TextAlignment.Center;

                    error_layout.AddView(error_text);

                    Button quitButton = new Button(ApplicationContext);
                    quitButton.Text = "Close game";
                    quitButton.TextAlignment = TextAlignment.Center;
                    LinearLayout.LayoutParams quitButtonLayoutParams = new LinearLayout.LayoutParams(700, 300);
                    quitButtonLayoutParams.SetMargins(100, 100, 100, 100);
                    quitButton.LayoutParameters = quitButtonLayoutParams;
                    quitButton.Enabled = true;

                    quitButton.Click += (s, evArgs) =>
                    {
                        RePlay_Game senderGame = sender as RePlay_Game;
                        if (senderGame != null)
                        {
                            senderGame.EndGame();
                        }
                    };

                    error_layout.AddView(quitButton);

                    MainGameLayout.RemoveView(SplashScreenView);
                    MainGameLayout.AddView(error_layout);
                }
            }
        }

        protected void StartImmersiveMode()
        {
            View decorView = Window.DecorView;
            Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            var uiOptions = (int)decorView.SystemUiVisibility;
            var newUiOptions = (int)uiOptions;

            newUiOptions |= (int)SystemUiFlags.Fullscreen;
            newUiOptions |= (int)SystemUiFlags.HideNavigation;
            newUiOptions |= (int)SystemUiFlags.Immersive;
            newUiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
        }

        #endregion
    }
}