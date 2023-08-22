using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using RePlay.Fragments;
using RePlay.Manager;

namespace RePlay.Activities
{
    // basic class for 1) notifying patients when they have completed their assigned prescriptions, and
    //                 2) prompting patients to check out the games list page
    [Activity(Label = "PrescriptionDoneActivity", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class PrescriptionDoneActivity : Activity
    {
        private GoogleConnectionManager google_connection_manager;

        #region OnCreate

        // sets up the views and onClick delegate allowing patients to navigate pages
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the layout resource
            SetContentView(Resource.Layout.PrescriptionDone);

            //Grab the input parameters for this activity
            google_connection_manager = StupidityManager.GiveMeThat("google") as GoogleConnectionManager;
            StupidityManager.CleanTheSlatePlease();

            Button button = FindViewById<Button>(Resource.Id.games_next);

            button.Click += delegate
            {
                StupidityManager.CleanTheSlatePlease();
                StupidityManager.HoldThisForMe("google", google_connection_manager);

                Intent intent = new Intent(this, typeof(MainActivity));
                StartActivity(intent);
            };
        }

        public override void OnBackPressed()
        {
            StupidityManager.CleanTheSlatePlease();
            StupidityManager.HoldThisForMe("google", google_connection_manager);

            Intent intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
            Finish();
        }

        protected override void OnResume()
        {
            base.OnResume();

            //Pass the google connection manager to the navigation fragment as well
            var navigation_fragment = FragmentManager.FindFragmentById<NavigationFragment>(Resource.Id.navigation_fragment);
            navigation_fragment.google_connection_manager = google_connection_manager;
        }

        #endregion
    }
}