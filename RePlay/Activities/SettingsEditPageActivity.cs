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
using RePlay.Fragments;
using RePlay.Manager;

namespace RePlay.Activities
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Activity(Label = "SettingsEditPageActivity", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class SettingsEditPageActivity : Activity
    {
        private GoogleConnectionManager google_connection_manager;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Set the UI for this activity
            SetContentView(Resource.Layout.SettingsSelectionPage);

            //Grab the input parameters for this activity
            google_connection_manager = StupidityManager.GiveMeThat("google") as GoogleConnectionManager;
            StupidityManager.CleanTheSlatePlease();

            //Load the project names
            var projects = ProjectListManager.GetProjectNames(this);

            //Set the project names in the drop-down box
            Spinner project_names_dropdown = FindViewById<Spinner>(Resource.Id.project_id_dropdown_box);
            ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, projects);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            project_names_dropdown.Adapter = adapter;

            //Set the default selected item in the drop-down box
            var selection_index = projects.IndexOf(PreferencesManager.ProjectName);
            if (selection_index >= 0)
            {
                project_names_dropdown.SetSelection(selection_index);
            }
            
            //Define a function to handle the project name selection being changed
            project_names_dropdown.ItemSelected += ProjectName_ItemSelected;

            //Define a function handle what happens when the user has completed typing in the Site ID text box
            EditText site_id_textbox = FindViewById<EditText>(Resource.Id.site_id_text_box);
            site_id_textbox.Text = PreferencesManager.SiteName;
            site_id_textbox.EditorAction += SideName_HandleCompletion;

            //Set the default values for the "show pcm switch" and the "debug mode switch"
            Switch show_pcm_switch = FindViewById<Switch>(Resource.Id.show_pcm_status_switch);
            Switch debug_mode_switch = FindViewById<Switch>(Resource.Id.debug_mode_switch);
            Switch show_stim_requests_switch = FindViewById<Switch>(Resource.Id.show_stimulation_requests_switch);
            show_pcm_switch.CheckedChange += Show_pcm_switch_CheckedChange;
            debug_mode_switch.CheckedChange += Debug_mode_switch_CheckedChange;
            show_stim_requests_switch.CheckedChange += Show_stim_requests_switch_CheckedChange;
            show_pcm_switch.Checked = PreferencesManager.ShowPCMConnectionInGames;
            debug_mode_switch.Checked = PreferencesManager.DebugMode;
            show_stim_requests_switch.Checked = PreferencesManager.ShowStimulationRequestsInGames;

            //Define a function to handle a button click when the "Done" button is pressed by the user
            Button done_button = FindViewById<Button>(Resource.Id.settings_selection_page_done_button);
            done_button.Click += HandleDoneButtonClick;
        }

        private void Show_stim_requests_switch_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Switch show_stim_requests_switch = FindViewById<Switch>(Resource.Id.show_stimulation_requests_switch);
            PreferencesManager.ShowStimulationRequestsInGames = show_stim_requests_switch.Checked;
        }

        private void Debug_mode_switch_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Switch debug_mode_switch = FindViewById<Switch>(Resource.Id.debug_mode_switch);
            PreferencesManager.DebugMode = debug_mode_switch.Checked;
        }

        private void Show_pcm_switch_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Switch show_pcm_switch = FindViewById<Switch>(Resource.Id.show_pcm_status_switch);
            PreferencesManager.ShowPCMConnectionInGames = show_pcm_switch.Checked;
        }

        private void HandleDoneButtonClick(object sender, EventArgs e)
        {
            //Close this activity
            this.Finish();
        }

        protected override void OnResume()
        {
            base.OnResume();

            //Pass the google connection manager to the navigation fragment as well
            var navigation_fragment = FragmentManager.FindFragmentById<NavigationFragment>(Resource.Id.navigation_fragment);
            navigation_fragment.google_connection_manager = google_connection_manager;
        }

        private void SideName_HandleCompletion(object sender, TextView.EditorActionEventArgs e)
        {
            EditText site_id_textbox = FindViewById<EditText>(Resource.Id.site_id_text_box);

            switch (e.ActionId)
            {
                case Android.Views.InputMethods.ImeAction.Done:
                case Android.Views.InputMethods.ImeAction.Go:
                case Android.Views.InputMethods.ImeAction.Next:
                    PreferencesManager.SiteName = site_id_textbox.Text;
                    if (google_connection_manager != null)
                    {
                        google_connection_manager.SetSiteForGoogleCommunication(PreferencesManager.SiteName);
                    }

                    break;
            }
        }

        private void ProjectName_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner project_names_dropdown = FindViewById<Spinner>(Resource.Id.project_id_dropdown_box);
            string k = (string)project_names_dropdown.SelectedItem;

            PreferencesManager.ProjectName = k;
            if (google_connection_manager != null)
            {
                google_connection_manager.SetProjectForGoogleCommunication(PreferencesManager.ProjectName);
            }
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}