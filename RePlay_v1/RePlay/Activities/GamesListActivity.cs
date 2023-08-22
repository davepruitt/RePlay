using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using RePlay.CustomViews;
using RePlay.Entity;
using RePlay.Fragments;
using RePlay.Manager;
using RePlay_VNS_Triggering;

// GamesListActivity: Select a game from a grid of all available games
namespace RePlay.Activities
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Activity(Label = "GamesListActivity", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class GamesListActivity : Activity
    {
        #region Properties

        private const int GAMES_PER_PAGE = 6;
        private GoogleConnectionManager google_connection_manager = null;
        private List<RePlayGame> replay_games = GameManager.Instance.Games;
        private Paginator<RePlayGame> game_paginator = null;

        private int CurrentPage = 0;
        private bool IsCurrentlyInGame = false;
        private bool currently_configuring_game = false;

        #endregion

        #region UI data members

        private GridView GameGridView;
        private ImageButton LeftButton;
        private ImageButton RightButton;
        
        #endregion

        #region Activity overrides

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Grab the input parameters for this activity
            google_connection_manager = StupidityManager.GiveMeThat("google") as GoogleConnectionManager;
            StupidityManager.CleanTheSlatePlease();

            //Create the game paginator
            game_paginator = new Paginator<RePlayGame>(GAMES_PER_PAGE, replay_games);

            //Set the UI layout for this activity
            SetContentView(Resource.Layout.GameList);

            //Initialize the UI
            InitializeViews();
        }

        protected override void OnResume()
        {
            base.OnResume();

            //Pass the google connection manager to the navigation fragment as well
            var navigation_fragment = FragmentManager.FindFragmentById<NavigationFragment>(Resource.Id.navigation_fragment);
            navigation_fragment.google_connection_manager = google_connection_manager;
        }

        #endregion

        #region Private Methods that handle UI state

        // Instantiate the left and right buttons
        private void InitializeViews()
        {
            GameGridView = FindViewById<GridView>(Resource.Id.gameslist_grid);
            LeftButton = FindViewById<ImageButton>(Resource.Id.gameslist_left);
            RightButton = FindViewById<ImageButton>(Resource.Id.gameslist_right);
            LeftButton.Enabled = false;
            LeftButton.SetImageResource(Resource.Drawable.keyboard_arrow_left_disabled);

            LeftButton.Click += LeftButton_Click;
            RightButton.Click += RightButton_Click;
            LeftButton.Touch += (s, e) => ButtonTouched(s, e, e.Event.Action);
            RightButton.Touch += (s, e) => ButtonTouched(s, e, e.Event.Action);

            HandleGenerationOfNewPage();
        }

        /// <summary>
        /// This method enables or disables buttons based on the current page number
        /// </summary>
        void ToggleButtons()
        {
            // Disable right button on last page
            if (CurrentPage == game_paginator.LastPage)
            {
                LeftButton.Enabled = true;
                RightButton.Enabled = false;
                LeftButton.SetImageResource(Resource.Drawable.keyboard_arrow_left);
                RightButton.SetImageResource(Resource.Drawable.keyboard_arrow_right_disabled);
            }

            // Disable left button on first page
            else if (CurrentPage == 0)
            {
                LeftButton.Enabled = false;
                RightButton.Enabled = true;
                LeftButton.SetImageResource(Resource.Drawable.keyboard_arrow_left_disabled);
                RightButton.SetImageResource(Resource.Drawable.keyboard_arrow_right);
            }
            else
            {
                LeftButton.Enabled = true;
                RightButton.Enabled = true;
                LeftButton.SetImageResource(Resource.Drawable.keyboard_arrow_left);
                RightButton.SetImageResource(Resource.Drawable.keyboard_arrow_right);
            }
        }

        private void HandleGenerationOfNewPage ()
        {
            //Create a view for the new games page
            CustomGameCardView new_gamegrid_page = new CustomGameCardView(this, game_paginator.GeneratePage(CurrentPage));

            //Subscribe to events from the page
            new_gamegrid_page.RequestConfigureGame += HandleGameConfigurationRequest;

            //Assign the new view to the game grid
            GameGridView.Adapter = new_gamegrid_page;

            //Toggle the state of the buttons
            ToggleButtons();
        }

        #endregion

        #region Button click event handlers

        /// <summary>
        /// This method handles animation of buttons that are touched
        /// </summary>
        private void ButtonTouched(object sender, View.TouchEventArgs e, MotionEventActions action)
        {
            ImageButton button = (ImageButton)sender;
            switch (action)
            {
                case MotionEventActions.Down:
                    button.Animate().ScaleX(NavigationFragment.BUTTON_SCALE).SetDuration(NavigationFragment.BUTTON_DURATION).Start();
                    button.Animate().ScaleY(NavigationFragment.BUTTON_SCALE).SetDuration(NavigationFragment.BUTTON_DURATION).Start();
                    break;
                case MotionEventActions.Up:
                    button.Animate().Cancel();
                    button.Animate().ScaleX(1f).SetDuration(NavigationFragment.BUTTON_DURATION).Start();
                    button.Animate().ScaleY(1f).SetDuration(NavigationFragment.BUTTON_DURATION).Start();
                    
                    if (button.Id.Equals(Resource.Id.gameslist_left))
                    {
                        LeftButton_Click(sender, e);
                    }
                    else
                    {
                        RightButton_Click(sender, e);
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles a click to the "left" button, goes back a page
        /// </summary>
        void LeftButton_Click(object sender, EventArgs e)
        {
            CurrentPage -= 1;
            HandleGenerationOfNewPage();
        }

        /// <summary>
        /// Handles a click on the "right" button, goes forward a page
        /// </summary>
        void RightButton_Click(object sender, EventArgs e)
        {
            CurrentPage += 1;
            HandleGenerationOfNewPage();
        }

        #endregion

        #region Methods to handle starting new games

        private void HandleGameConfigurationRequest(object sender, EventArgs e)
        {
            CustomGameCardView.RequestConfigureGameEventArgs event_args = e as CustomGameCardView.RequestConfigureGameEventArgs;
            if (event_args != null)
            {
                if (!currently_configuring_game)
                {
                    currently_configuring_game = true;

                    //Begin a fragment transaction
                    FragmentTransaction fm = FragmentManager.BeginTransaction();

                    //Create an instance of the dialog that will allow the user to configure the game to launch
                    AddNewPrescriptionItemFragment dialog = new AddNewPrescriptionItemFragment(this,
                        AddNewPrescriptionItemFragment.AddPrescriptionItemMode.CreateItemForImmediateGameplay,
                        event_args.GameInternalName);

                    dialog.NewImmediateGameplayItemConfirmed += HandleGameLaunchRequest;
                    dialog.DialogPausedOrCancelled += HandleGameConfigurationCancelled;
                    dialog.Show(fm, "dialog fragment");
                }
            }
        }

        private void HandleGameConfigurationCancelled(object sender, EventArgs e)
        {
            currently_configuring_game = false;
        }

        public void HandleGameLaunchRequest(object sender, EventArgs e)
        {
            currently_configuring_game = false;

            var new_game_event_args = e as AddNewPrescriptionItemFragment.PrescriptionItemEditEventArgs;
            if (new_game_event_args != null)
            {
                var assignment_item = new_game_event_args.NewOrEdited_PrescriptionItem;

                if (DeviceManager.Instance.CheckDeviceAttached(this, assignment_item.Device))
                {
                    if (DeviceManager.Instance.CheckDeviceAttachedAndPermissions(this, assignment_item.Device))
                    {
                        bool launch_game = false;
                        if (assignment_item.Device == RePlay_Exercises.ExerciseDeviceType.ReCheck)
                        {
                            launch_game = DeviceManager.Instance.CheckCorrectReCheckModuleAttached(this, assignment_item.Exercise);
                            if (!launch_game)
                            {
                                AlertDialog.Builder dialog = new AlertDialog.Builder(this);
                                AlertDialog alert = dialog.Create();
                                alert.SetTitle("Confirm");
                                alert.SetMessage("Please ensure that the correct ReCheck module is plugged in.");
                                alert.SetButton("OK", (c, ev) =>
                                {
                                    alert.Dismiss();
                                });
                                alert.Show();
                            }
                        }
                        else
                        {
                            launch_game = true;
                        }

                        //Launch game here
                        if (launch_game)
                        {
                            //See if there are VNS parameters we should be using
                            //Even though this game is not part of a prescription, we will draw the VNS
                            //algorithm parameters from the current prescription.
                            VNSAlgorithmParameters vns_algorithm_parameters = null;
                            if (PrescriptionManager.Instance != null &&
                                PrescriptionManager.Instance.CurrentPrescription != null)
                            {
                                vns_algorithm_parameters = PrescriptionManager.Instance.CurrentPrescription.VNS;
                            }

                            bool launch_success = GameLaunchManager.LaunchGame(this, assignment_item, vns_algorithm_parameters, false);
                        }
                    }
                }
                else
                {
                    AlertDialog.Builder dialog = new AlertDialog.Builder(this);
                    AlertDialog alert = dialog.Create();
                    alert.SetTitle("Confirm");
                    string deviceMsg = DeviceManager.Instance.GetDeviceMessage(assignment_item.Device.ToString());
                    alert.SetMessage(deviceMsg);
                    alert.SetButton("OK", (c, ev) =>
                    {
                        alert.Dismiss();
                    });
                    alert.Show();
                }
            }
        }

        #endregion
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
