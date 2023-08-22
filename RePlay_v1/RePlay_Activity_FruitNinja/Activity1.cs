using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using RePlay_Activity_Common;
using RePlay_Exercises;

namespace RePlay_Activity_FruitNinja
{
    [Activity(Label = "RePlay_Activity_FruitNinja"
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
            var g = new FruitNinjaGame(game_launch_parameters);

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

