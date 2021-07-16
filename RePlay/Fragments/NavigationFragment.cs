using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using RePlay.Activities;
using RePlay.Entity;
using RePlay.Manager;

namespace RePlay.Fragments
{
    // Top navigation bar for going to the differnt activities
    // of the RePlay app (i.e. games, settings, home screen)
#pragma warning disable CS0618 // Type or member is obsolete
    public class NavigationFragment : Fragment
    {
        #region Properties

        private ImageButton home, games, settings, connection, tabletSettings;
        public const int BUTTON_DURATION = 60;
        public const float BUTTON_SCALE = .6f;
        private bool IsConnected { get; set; }
        private PCMConnectionManager PCMConnection { get; set; }
        private bool ConnectionIconClicked { get; set; } = false;

        #endregion

        #region Public data members

        public GoogleConnectionManager google_connection_manager = null;

        #endregion

        #region EventHandlers

        private void HomeClicked(object sender, EventArgs e)
        {
            ImageButton button = (ImageButton)sender;
            if (Activity is SettingsAssignmentPageActivity) Activity.Finish();
            if (!button.Context.GetType().Equals(typeof(MainActivity)))
            {
                StupidityManager.CleanTheSlatePlease();
                StupidityManager.HoldThisForMe("google", google_connection_manager);

                Intent intent = new Intent(button.Context, typeof(MainActivity));
                button.Context.StartActivity(intent);
            }
        }

        private void GamesClicked(object sender, EventArgs e)
        {
            ImageButton button = (ImageButton)sender;
            if (Activity is SettingsAssignmentPageActivity)
            {
                Activity.Finish();
            }

            if (!button.Context.GetType().Equals(typeof(GamesListActivity))) 
            {
                var current_participant = PatientLoader.Load(this.Activity.Assets);
                var is_name_okay = Participant.ParticipantID_IsOK(current_participant.SubjectID);
                if (!is_name_okay)
                {
                    AlertDialog.Builder alert = new AlertDialog.Builder(this.Activity);
                    alert.SetTitle("Invalid Participant ID");
                    alert.SetMessage("The participant ID is invalid. Please enter a valid participant ID to continue.");
                    alert.SetNeutralButton("OK", (senderAlert, args) =>
                    {
                        //empty
                    });

                    Dialog dialog = alert.Create();
                    dialog.Show();
                }
                else
                {
                    var current_prescription = PrescriptionManager.Instance.CurrentPrescription;
                    AlertDialog.Builder alert = new AlertDialog.Builder(this.Activity);
                    alert.SetTitle("Verify action");

                    string msg = "Please verify that the participant ID is correct:\n\n" + current_participant.SubjectID;
                    int start = msg.Length - current_participant.SubjectID.Length;
                    int end = msg.Length;
                    SpannableString s = new SpannableString(msg);
                    s.SetSpan(new AlignmentSpanStandard(Layout.Alignment.AlignCenter), start, end, SpanTypes.ExclusiveInclusive);
                    s.SetSpan(new RelativeSizeSpan(2.0f), start, end, SpanTypes.ExclusiveInclusive);
                    alert.SetMessage(s);
                    alert.SetPositiveButton("PROCEED", (senderAlert, args) =>
                    {
                        StartGamesPage();
                    });
                    alert.SetNegativeButton("GO BACK", (senderAlert, args) =>
                    {
                        //empty
                    });

                    Dialog dialog = alert.Create();
                    dialog.Show();
                }
            }
        }

        private void StartGamesPage ()
        {
            StupidityManager.CleanTheSlatePlease();
            StupidityManager.HoldThisForMe("google", google_connection_manager);
            Intent intent = new Intent(this.Activity, typeof(GamesListActivity));
            StartActivity(intent);
        }

        private void ConnectionClicked(object sender, EventArgs e)
        {
            ConnectionIconClicked = true;
            Toast.MakeText(Context, "Refreshing PCM Connection...", ToastLength.Short).Show();
            PCMConnection.RunConnectionCheck();
        }

        private void SettingsClicked(object sender, EventArgs e)
        {
            ImageButton button = (ImageButton)sender;
            if (!button.Context.GetType().Equals(typeof(SettingsLoginActivity)) && !button.Context.GetType().Equals(typeof(SettingsAssignmentPageActivity)))
            {
                StupidityManager.CleanTheSlatePlease();
                StupidityManager.HoldThisForMe("google", google_connection_manager);

                Intent intent = new Intent(button.Context, typeof(SettingsLoginActivity));
                StartActivity(intent);
            }
        }

        private void TabletSettingsClicked(object sender, EventArgs e)
        {
            AlertDialog.Builder dialog = new AlertDialog.Builder(Activity);
            AlertDialog alert = dialog.Create();
            alert.SetTitle("Warning");
            alert.SetMessage("This action will transfer you out of RePlay to a separate application that allows you to change tablet-wide settings.\n\n " +
                "Are you sure you want to navigate away from RePlay?");
            alert.SetButton("YES", (c, ev) =>
            {
                StartActivity(new Intent(Android.Provider.Settings.ActionSettings));
                Activity.Finish();
            });
            alert.SetButton2("NO", (c, ev) => {
                alert.Dismiss();
            });
            alert.Show();
        }
        
        #endregion

        #region OnCreate

        // Default OnCreate
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            PCMConnection = PCMConnectionManager.Instance;
            IsConnected = PCMConnection.IsConnectedToPCM;
            PCMConnection.RunConnectionCheck();
            PCMConnection.PropertyChanged += PCMConnection_PropertyChanged;
        }

        // Inflates the Navigation view
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.Navigation, container, false);
            
            // ImageButton objects
            home = view.FindViewById<ImageButton>(Resource.Id.homeButton);
            games = view.FindViewById<ImageButton>(Resource.Id.gamesButton);
            settings = view.FindViewById<ImageButton>(Resource.Id.settingsButton);
            connection = view.FindViewById<ImageButton>(Resource.Id.connectionButton);
            tabletSettings = view.FindViewById<ImageButton>(Resource.Id.tabletsettingsButton);

            if (Activity.GetType() != typeof(SettingsAssignmentPageActivity))
            {
                tabletSettings.Visibility = ViewStates.Gone;
            }
            else
            {
                tabletSettings.Touch += (s, e) => ButtonTouched(s, e, e.Event.Action);
            }

            UpdateConnectionIcon();

            // Add touch delegates
            home.Touch += (s, e) => ButtonTouched(s, e, e.Event.Action);
            games.Touch += (s, e) => ButtonTouched(s, e, e.Event.Action);
            settings.Touch += (s, e) => ButtonTouched(s, e, e.Event.Action);
            connection.Touch += (s, e) => ButtonTouched(s, e, e.Event.Action);

            return view;
        }

        // Produce bounce effect when a button is pressed
        private void ButtonTouched(object sender, EventArgs e, MotionEventActions action)
        {
            ImageButton button = (ImageButton)sender;
            switch (action)
            {
                case MotionEventActions.Down:
                    button.Animate().ScaleX(BUTTON_SCALE).SetDuration(BUTTON_DURATION).Start();
                    button.Animate().ScaleY(BUTTON_SCALE).SetDuration(BUTTON_DURATION).Start();
                    break;
                case MotionEventActions.Up:
                    button.Animate().Cancel();
                    button.Animate().ScaleX(1f).SetDuration(BUTTON_DURATION).Start();
                    button.Animate().ScaleY(1f).SetDuration(BUTTON_DURATION).Start();

                    if (button.Id.Equals(Resource.Id.homeButton))
                    {
                        HomeClicked(sender, e);
                    }
                    else if (button.Id.Equals(Resource.Id.gamesButton))
                    {
                        GamesClicked(sender, e);
                    }
                    else if (button.Id.Equals(Resource.Id.settingsButton))
                    {
                        SettingsClicked(sender, e);
                    }
                    else if (button.Id.Equals(Resource.Id.tabletsettingsButton))
                    {
                        TabletSettingsClicked(sender, e);
                    }
                    else
                    {
                        ConnectionClicked(sender, e);
                    }

                    break;
            }
        }

        // Refresh connection icon
        private void UpdateConnectionIcon()
        {
            if (IsConnected)
            {
                bool? prescription_vns_enabled = PrescriptionManager.Instance?.CurrentPrescription?.VNS?.Enabled;
                bool vns_enabled = (prescription_vns_enabled.HasValue) ? prescription_vns_enabled.Value : false;
                if (vns_enabled)
                {
                    connection.SetImageResource(Resource.Drawable.pcmConnected);
                }
                else
                {
                    connection.SetImageResource(Resource.Drawable.pcmConnected_nostim);
                }
            }
            else
            {
                connection.SetImageResource(Resource.Drawable.pcmDisconnected);
            }
        }

        private void PCMConnection_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsConnected = PCMConnection.IsConnectedToPCM && PCMConnection.IsConnectedToRestore;
            if (!PCMConnection.IsConnectedToRestore && Context != null && ConnectionIconClicked)
            {
                Toast.MakeText(Context, "Please re-launch the ReStore app to reconnect your PCM", ToastLength.Short).Show();
            }
            else if (!PCMConnection.IsConnectedToPCM && Context != null && ConnectionIconClicked)
            {
                Toast.MakeText(Context, "Please make sure your PCM is turned on", ToastLength.Short).Show();
            }

            ConnectionIconClicked = false;
            UpdateConnectionIcon();
        }

        #endregion
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
