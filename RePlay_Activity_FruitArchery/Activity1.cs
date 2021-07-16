using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Content.Res;
using Android.Widget;
using RePlay_Exercises;
using RePlay_Activity_Common;

namespace RePlay_Activity_FruitArchery
{
    [Activity(Label = "RePlay_Activity_FruitArchery"
        , AlwaysRetainTaskState = true
        , LaunchMode = Android.Content.PM.LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.UserLandscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout)]
    public class Activity1 : RePlay_Game_Activity
    {
        #region OnCreate/OnResume

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            //Go full screen
            StartImmersiveMode();

            //Parse the intent data
            var game_launch_parameters = ParseIntentData();

            //Instantiate the game
            var g = new FruitArcheryGame(game_launch_parameters);

            //Display and run the game
            SubscribeToGameEvents(g);
            AddGameToView(g);
            ShowSplashScreen();
            SetContentView(MainGameLayout);

            g.Run();
        }

        // Restart immersive mode
        protected override void OnResume()
        {
            base.OnResume();

            StartImmersiveMode();
        }

        #endregion
    }
}

