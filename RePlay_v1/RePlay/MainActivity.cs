using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using RePlay.Manager;
using RePlay.Activities;
using Android.Views;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Android.Content.PM;
using Android.Runtime;
using Android;
using RePlay.Fragments;
using RePlay.Entity;
using System;
using Android.Text;
using Android.Text.Style;

namespace RePlay
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Activity(Label = "RePlay", Icon = "@mipmap/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape, LaunchMode = Android.Content.PM.LaunchMode.SingleTask)]
    public class MainActivity : Activity
    {
        private long LastPlayClickTime = 0;

        #region Public data members

        public GoogleConnectionManager google_connection_manager = null;

        #endregion

        #region OnCreate

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Grab anything passed to us from the StupidityManager
            google_connection_manager = StupidityManager.GiveMeThat("google") as GoogleConnectionManager;
            StupidityManager.CleanTheSlatePlease();

            //Load games from Assets/games.txt and load prescriptions from internal storage
            GameManager.Instance.LoadGames(Assets);
            GameManager.Instance.SetParentActivity(this);
            ExerciseManager.Instance.LoadExercises(Assets);
            PrescriptionManager.Instance.LoadPrescription();
            SavedPrescriptionManager.Instance.LoadPrescription();
            PCMConnectionManager.CreateInstance(this);
            DeviceManager.Instance.Initialize(this);

            //Create the noise floor preferences file if it doesn't already exist
            PreferencesManager.CreateNoiseFloorPreferencesFile();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            ImageButton button = FindViewById<ImageButton>(Resource.Id.playButton);

            //Define what happens when the "play button" is clicked
            button.Click += HandlePrimaryButtonClick;
            
            //Define what happens when the "play button" has a press/release motion event
            button.Touch += (s, e) =>
            {
                switch (e.Event.Action)
                {
                    case MotionEventActions.Down:
                        button.SetImageResource(Resource.Drawable.play_green_inverted);
                        break;
                    case MotionEventActions.Up:
                        button.SetImageResource(Resource.Drawable.play_green);
                        break;
                }

                e.Handled = false;
            };
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnRestart()
        {
            base.OnRestart();
        }

        protected override void OnResume()
        {
            base.OnResume();

            //Pass the google connection manager to the navigation fragment as well
            var navigation_fragment = FragmentManager.FindFragmentById<NavigationFragment>(Resource.Id.navigation_fragment);
            navigation_fragment.google_connection_manager = google_connection_manager;

            var participant_obj = PatientLoader.Load(this.Assets);
            string subject_id = participant_obj.SubjectID;

            var patient_id_textview = FindViewById<TextView>(Resource.Id.participantidtextview);
            patient_id_textview.Text = subject_id;

            var replay_version_textview = FindViewById<TextView>(Resource.Id.replay_version_textview);
            var tablet_id_textview = FindViewById<TextView>(Resource.Id.replay_tablet_textview);
            var project_name_textview = FindViewById<TextView>(Resource.Id.project_name_textview);
            var site_id_textview = FindViewById<TextView>(Resource.Id.site_name_textview);
            var assignment_name_textview = FindViewById<TextView>(Resource.Id.assignmentnametextview);

            replay_version_textview.Text = BuildInformationManager.GetVersionName(this).ToString();
            tablet_id_textview.Text = PreferencesManager.GetTabletID(this);
            project_name_textview.Text = PreferencesManager.ProjectName;
            site_id_textview.Text = PreferencesManager.SiteName;
            if (PrescriptionManager.Instance != null)
            {
                if (PrescriptionManager.Instance.CurrentPrescription != null)
                {
                    assignment_name_textview.Text = PrescriptionManager.Instance.CurrentPrescription.Name;
                }
            }
            
            AppCenter.SetUserId(subject_id);
        }

        #endregion

        #region Event handlers

        private void HandlePrimaryButtonClick(object sender, System.EventArgs e)
        {
            // Multiple click launch prevention
            if (SystemClock.ElapsedRealtime() - LastPlayClickTime < 500)
            {
                return;
            }
            else
            {
                var current_participant = PatientLoader.Load(this.Assets);
                var is_name_okay = Participant.ParticipantID_IsOK(current_participant.SubjectID);
                if (!is_name_okay)
                {
                    AlertDialog.Builder alert = new AlertDialog.Builder(this);
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
                    //if (current_prescription.IsSupervised)
                    //{
                        AlertDialog.Builder alert = new AlertDialog.Builder(this);
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
                            LaunchAssignment();
                        });
                        alert.SetNegativeButton("GO BACK", (senderAlert, args) =>
                        {
                            //empty
                        });

                        Dialog dialog = alert.Create();
                        dialog.Show();
                    //}
                    //else
                    //{
                        //TO DO
                    //}
                }
            }
        }

        #endregion

        #region Private methods

        private void LaunchAssignment ()
        {
            //Check to see if this is a "new" participant
            //In this context, a "new" participant means that this is the first time 
            //that this participant has run the most recently prescribed assignment.
            var current_participant = PatientLoader.Load(this.Assets);
            if (current_participant.IsNewParticipant)
            {
                //Set the flag to false
                current_participant.IsNewParticipant = false;

                //Save the participant data
                PatientLoader.Save(current_participant);
            }
            
            LastPlayClickTime = SystemClock.ElapsedRealtime();

            //Pass some variables into the stupidity manager that PromptActivity can then pick up.
            StupidityManager.CleanTheSlatePlease();
            StupidityManager.HoldThisForMe("google", google_connection_manager);

            //Start the "PromptActivity" activity.
            Intent intent = new Intent(this, typeof(PromptActivity));
            StartActivity(intent);
        }

        #endregion
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
