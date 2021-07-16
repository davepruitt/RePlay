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
using RePlay.Entity;
using RePlay.Fragments;
using RePlay.Manager;

namespace RePlay.Activities
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Activity(Label = "SettingsMenuPageActivity", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class SettingsMenuPageActivity : Activity
    {
        private Participant patient = null;
        private bool PatientFragmentLaunched = false;
        private GoogleConnectionManager google_connection_manager;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.SettingsMenuPage);

            //Grab the input parameters for this activity
            google_connection_manager = StupidityManager.GiveMeThat("google") as GoogleConnectionManager;
            StupidityManager.CleanTheSlatePlease();

            SettingsLoginActivity.SettingsActivityLaunched = false;

            //Define button-click handlers
            Button set_participant_id_button = FindViewById<Button>(Resource.Id.settings_set_participant_id_button);
            Button edit_settings_button = FindViewById<Button>(Resource.Id.settings_edit_button);
            Button edit_assignment_button = FindViewById<Button>(Resource.Id.settings_edit_assignment_button);
            Button access_manual_stim_button = FindViewById<Button>(Resource.Id.access_manual_stim_button);

            set_participant_id_button.Click += SetParticipantID_ButtonClick;
            edit_settings_button.Click += EditSettings_ButtonClick;
            edit_assignment_button.Click += EditAssignment_ButtonClick;
            access_manual_stim_button.Click += AccessManualStim_ButtonClick;
        }

        protected override void OnResume()
        {
            base.OnResume();

            //Pass the google connection manager to the navigation fragment as well
            var navigation_fragment = FragmentManager.FindFragmentById<NavigationFragment>(Resource.Id.navigation_fragment);
            navigation_fragment.google_connection_manager = google_connection_manager;
        }

        private void SetParticipantID_ButtonClick(object sender, EventArgs e)
        {
            if (!PatientFragmentLaunched)
            {
                patient = PatientLoader.Load(Assets);
                PatientFragmentLaunched = true;
                Activity settings = this;
                FragmentTransaction fm = settings.FragmentManager.BeginTransaction();
                PatientFragment dialog = new PatientFragment(patient, google_connection_manager);
                PatientFragment.DialogClosed += PatientFragment_DialogClosed;
                dialog.Show(fm, "dialog fragment");
            }
        }

        private void PatientFragment_DialogClosed(object sender, Participant p)
        {
            PatientFragmentLaunched = false;
            var frag = sender as PatientFragment;
            if (frag.SaveInfo)
            {
                patient = p;
                PatientLoader.Save(patient);

                //Save the data for the purposes of Google communication
                try
                {
                    google_connection_manager.AddNewParticipant(p.SubjectID);
                }
                catch (Exception)
                {
                    //Unable to add participant to the google drive folder
                }
            }
        }

        private void EditSettings_ButtonClick(object sender, EventArgs e)
        {
            StupidityManager.CleanTheSlatePlease();
            StupidityManager.HoldThisForMe("google", google_connection_manager);

            Intent intent = new Intent(this, typeof(SettingsEditPageActivity));
            StartActivity(intent);
        }

        private void EditAssignment_ButtonClick(object sender, EventArgs e)
        {
            StupidityManager.CleanTheSlatePlease();
            StupidityManager.HoldThisForMe("google", google_connection_manager);

            Intent intent = new Intent(this, typeof(SettingsAssignmentPageActivity));
            StartActivity(intent);
        }

        private void AccessManualStim_ButtonClick(object sender, EventArgs e)
        {
            StupidityManager.CleanTheSlatePlease();
            StupidityManager.HoldThisForMe("google", google_connection_manager);

            var patient = PatientLoader.Load(Assets);
            Type t = Type.GetType("RePlay_Activity_TherapistManualMode.MainActivity, RePlay_Activity_TherapistManualMode");

            Intent intent = new Intent(this, t);
            intent.PutExtra("tabletID", PreferencesManager.GetTabletID(this));
            intent.PutExtra("subjectID", patient.SubjectID);
            intent.PutExtra("projectID", PreferencesManager.ProjectName);
            intent.PutExtra("siteID", PreferencesManager.SiteName);
            intent.PutExtra("showPCMConnectionStatus", PreferencesManager.ShowPCMConnectionInGames);
            intent.PutExtra("debugMode", PreferencesManager.DebugMode);
            StartActivity(intent);
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}