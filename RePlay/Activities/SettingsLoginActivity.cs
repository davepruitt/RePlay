using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Graphics;
using Android.Text;
using Android.Widget;
using RePlay.Manager;
using RePlay.Fragments;

// SettingsLoginActivity: Authenticate user to get to settings page
namespace RePlay.Activities
{
    // Login screen to access the settings/prescriptions page
    [Activity(Label = "SettingsLoginActivity", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class SettingsLoginActivity : Activity
    {
        #region Properties
        // Display strings used on prompt
        const string PASSWORD = "replay";
        const string PASSWORD_ERR = "Incorrect password";
        const string PASSWORD_EMPTY = "Please enter your password";
        const string PASSWORD_CLEAN = "Enter your password";

        // Colors
        const string GREEN = "#69BE28";
        const string RED = "#D61926";

        Button NextButton;
        Button BackButton;
        EditText PasswordText;
        TextView PasswordPrompt;

        public static bool SettingsActivityLaunched { get; set; } = false;

        private GoogleConnectionManager google_connection_manager = null;

        #endregion

        #region OnCreate

        // Inflates the settings login layout and add the
        // event hanlders to the page elements (e.g. the
        // next button, password field, back button)
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SettingsLogin);

            //Grab the input parameters for this activity
            google_connection_manager = StupidityManager.GiveMeThat("google") as GoogleConnectionManager;
            StupidityManager.CleanTheSlatePlease();

            // Add click handlers
            NextButton = FindViewById<Button>(Resource.Id.settings_next);
            NextButton.Click += Settings_Next_Click;

            BackButton = FindViewById<Button>(Resource.Id.settings_back);
            BackButton.Click += Settings_Back_Click;

            // Add input changed handler
            PasswordText = FindViewById<EditText>(Resource.Id.password);
            PasswordText.TextChanged += User_Input_Changed;
            PasswordText.EditorAction += PasswordText_EditorAction;
            //PasswordText.KeyPress += PasswordText_KeyPress;

            PasswordPrompt = FindViewById<TextView>(Resource.Id.enter_password);
            
            // Add the build information to the view
            var build_information_view = FindViewById<TextView>(Resource.Id.build_information_text_view_settings_login_page);
            build_information_view.Text = "RePlay Version: " + BuildInformationManager.GetVersionName(this).ToString();
        }

        #endregion

        #region Methods
        // Handle the back image button click
        void Settings_Back_Click(object sender, EventArgs e)
        {
            StupidityManager.CleanTheSlatePlease();
            StupidityManager.HoldThisForMe("google", google_connection_manager);

            Intent intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }

        // Handle the text change in edit text
        void User_Input_Changed(object sender, TextChangedEventArgs e)
        {
            if (PasswordPrompt.Text.Equals(PASSWORD_ERR))
            {
                PasswordText.SetBackgroundResource(Resource.Drawable.EditTextBorder);
                PasswordPrompt.Text = PASSWORD_CLEAN;
                PasswordPrompt.SetTextColor(Color.ParseColor(GREEN));
            }
        }
        
        // Intercept done or return key
        private void PasswordText_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if ((e.ActionId == Android.Views.InputMethods.ImeAction.Done) ||
                (e.Event.Action == Android.Views.KeyEventActions.Down &&
                e.Event.KeyCode == Android.Views.Keycode.Enter))
            {
                ValidatePassword();
            }
        }

        // Handle the next button click
        private void Settings_Next_Click(object sender, EventArgs e)
        {
            ValidatePassword();
        }

        // Check password
        private void ValidatePassword()
        {
            string EnteredString = PasswordText.Text;

            // Check if the password is correct and handle appropriately
            if (string.IsNullOrEmpty(EnteredString))
            {
                PasswordPrompt.Text = PASSWORD_EMPTY;
            }
            else if (EnteredString.Equals(PASSWORD) && !SettingsActivityLaunched)
            {
                StupidityManager.CleanTheSlatePlease();
                StupidityManager.HoldThisForMe("google", google_connection_manager);

                // Password is correct; launch the settings activity
                SettingsActivityLaunched = true;
                Intent intent = new Intent(this, typeof(SettingsMenuPageActivity));
                StartActivity(intent);
            }
            else
            {
                // Incorrect password; change edit text UI to reflect incorrect password
                PasswordPrompt.Text = PASSWORD_ERR;
                PasswordText.SetBackgroundResource(Resource.Drawable.EditTextBorderRed);
                PasswordPrompt.SetTextColor(Color.ParseColor(RED));
            }
        }

        // Reset the password field so that the previous password wasn't saved
        protected override void OnResume()
        {
            base.OnResume();
            PasswordText.Text = "";

            //Pass the google connection manager to the navigation fragment as well
            var navigation_fragment = FragmentManager.FindFragmentById<NavigationFragment>(Resource.Id.navigation_fragment);
            navigation_fragment.google_connection_manager = google_connection_manager;
        }
        #endregion
    }
}
