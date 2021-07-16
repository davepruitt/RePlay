using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using RePlay.Entity;
using RePlay.Fragments;
using RePlay.Manager;
using RePlay_Exercises;

namespace RePlay.Activities
{
#pragma warning disable CS0618 // Type or member is obsolete
    // an activity designed to describe the current exercise and game a patient needs to complete for his/her prescription
    [Activity(Label = "PromptActivity", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class PromptActivity : Activity
    {
        #region Private data members

        private GoogleConnectionManager google_connection_manager = null;
        private DateTime prescription_item_start_time = DateTime.MinValue;
        private int current_prescription_item_index = 0;
        private List<PrescriptionItem> prescription = new List<PrescriptionItem>();
        private ExerciseManager exercises = ExerciseManager.Instance;
        private bool IsCurrentlyInGame = false;
        private bool ExternalApplicationHasBeenLaunched = false;
        public bool VideoTutorialLaunched { get; set; } = false;

        #endregion

        #region UI pieces of the activity

        //UI pieces of the activity
        private Button StartGameButton;
        private Button GoToPreviousGameButton;
        private Button SkipGameButton;
        private Button Video;
        private ImageView exercisePic;
        private ImageView gameImage; 
        private ImageView DeviceImage;
        private TextView exerciseText;
        private TextView exerciseSubtext;
        private TextView gameText;
        private TextView DurationText;
        private TextView DeviceText;
        private TextView DeviceInstructionText;
        private TextView activityIndexText;

        #endregion

        #region Activity overrides

        /// <summary>
        /// This sets up the UI, gets the current prescription and the state of the participant's
        /// progress in the prescription from the relevant manager classes
        /// </summary>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Set the UI for this activity
            SetContentView(Resource.Layout.Prompt);

            //Grab the input parameters for this activity
            google_connection_manager = StupidityManager.GiveMeThat("google") as GoogleConnectionManager;
            StupidityManager.CleanTheSlatePlease();

            //Set all of the UI elements
            gameText = FindViewById<TextView>(Resource.Id.prompt_game_text);
            gameImage = FindViewById<ImageView>(Resource.Id.prompt_game_image);
            exercisePic = FindViewById<ImageView>(Resource.Id.prompt_exercise_image);
            exerciseText = FindViewById<TextView>(Resource.Id.prompt_exercise_text);
            exerciseSubtext = FindViewById<TextView>(Resource.Id.prompt_exercise_span);
            DurationText = FindViewById<TextView>(Resource.Id.prompt_duration_text);
            DeviceText = FindViewById<TextView>(Resource.Id.prompt_device_text);
            DeviceInstructionText = FindViewById<TextView>(Resource.Id.prompt_device_instruction);
            activityIndexText = FindViewById<TextView>(Resource.Id.activityindextextview);
            StartGameButton = FindViewById<Button>(Resource.Id.next);
            DeviceImage = FindViewById<ImageView>(Resource.Id.prompt_device_image);
            GoToPreviousGameButton = FindViewById<Button>(Resource.Id.prescription_rewind);
            SkipGameButton = FindViewById<Button>(Resource.Id.prescription_forward);
            Video = FindViewById<Button>(Resource.Id.exercise_tutorial);

            //Grab some manager classes
            if (PrescriptionManager.Instance != null)
            {
                if (PrescriptionManager.Instance.NewlySwappedPrescription)
                {
                    PrescriptionManager.Instance.NewlySwappedPrescription = false;
                    StateManager.Instance.UpdateState(DateTimeOffset.Now.ToUnixTimeMilliseconds(), 0);
                    current_prescription_item_index = 0;
                }
            }

            prescription = PrescriptionManager.Instance.CurrentPrescription.PrescriptionItems;
            current_prescription_item_index = StateManager.Instance.Index;
            exercises = ExerciseManager.Instance;

            //Declare handlers for button clicks
            StartGameButton.Click += HandleStartGameButtonClick;
            GoToPreviousGameButton.Click += Handle_GoBackToPreviousGame_ButtonClick;
            SkipGameButton.Click += Handle_SkipGame_ButtonClick;
            Video.Click += Handle_WatchTutorialVideo_ButtonClick;

            //Now update the state of this activity
            UpdateState();
        }

        /// <summary>
        /// This activity is called any time this activity is "resumed". This MAY be after completion of a
        /// game, but that is not the only scenario in which it could be called. Specifically, it is always
        /// called after completion of an EXTERNAL game (such as ReTrieve), and so this is our chance to handle
        /// game-completion events after games like ReTrieve return control to RePlay.
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();

            //Pass the google connection manager to the navigation fragment as well
            var navigation_fragment = FragmentManager.FindFragmentById<NavigationFragment>(Resource.Id.navigation_fragment);
            navigation_fragment.google_connection_manager = google_connection_manager;

            //Grab the participant information
            var participant_obj = PatientLoader.Load(this.Assets);
            var participant_id_textview = FindViewById<TextView>(Resource.Id.participantidtextview);
            participant_id_textview.Text = "Participant ID: " + participant_obj.SubjectID;

            //Check to see if this activity is resuming after the closure of an external game
            if (ExternalApplicationHasBeenLaunched)
            {
                HandleCompletionOfPrescriptionItem();
            }

            //Check to see if there is a fresh assignment that has been assigned
            if (PrescriptionManager.Instance != null)
            {
                if (PrescriptionManager.Instance.NewlySwappedPrescription)
                {
                    PrescriptionManager.Instance.NewlySwappedPrescription = false;
                    StateManager.Instance.UpdateState(DateTimeOffset.Now.ToUnixTimeMilliseconds(), 0);
                    current_prescription_item_index = 0;
                }
            }
        }

        /// <summary>
        /// This method is called when control is returned to this activity, after returning from a game
        /// that was launched using the StartActivityForResult method.
        /// </summary>
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            //if(requestCode == REQUEST_CODE && resultCode == Result.Ok)
            if (requestCode == GameLaunchManager.REQUEST_CODE) //temporary change, for some reason the resultCode is returning "CANCELED" from RepetitionsMode
            {
                HandleCompletionOfPrescriptionItem();
            }
        }


        #endregion

        #region Private methods

        /// <summary>
        /// This method is called any time we return to this activity after having completed
        /// a prescription item.
        /// </summary>
        private void HandleCompletionOfPrescriptionItem ()
        {
            //Get the current participant
            var participant_obj = PatientLoader.Load(this.Assets);

            //Set these flags to now be false
            ExternalApplicationHasBeenLaunched = false;
            IsCurrentlyInGame = false;

            //Save information about this completed game to Google (if possible)
            try
            {
                TimeSpan duration = DateTime.Now - prescription_item_start_time;

                google_connection_manager.AddRowToParticipantSheet(
                    participant_obj.SubjectID,
                    DateTime.Now,
                    PreferencesManager.GetTabletID(this),
                    prescription[current_prescription_item_index].Game.ExternalName,
                    ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(prescription[current_prescription_item_index].Exercise),
                    prescription[current_prescription_item_index].Difficulty.ToString(),
                    duration,
                    "NA");
            }
            catch (Exception e)
            {
                //empty
            }

            //Update the index of the current prescription item
            current_prescription_item_index++;

            //Update the state of the activity
            UpdateState();
        }

        /// <summary>
        /// This method updates the current state of this activity.
        /// </summary>
        private void UpdateState()
        {
            if (current_prescription_item_index < prescription.Count)
            {
                UpdateView();
                StateManager.Instance.UpdateState(DateTimeOffset.Now.ToUnixTimeMilliseconds(), current_prescription_item_index);
            }
            else
            {
                //Set index to 0 so patient can run through prescription again
                current_prescription_item_index = 0;
                StateManager.Instance.UpdateState(DateTimeOffset.Now.ToUnixTimeMilliseconds(), current_prescription_item_index);

                //Go to prescriptions finished page
                StupidityManager.CleanTheSlatePlease();
                StupidityManager.HoldThisForMe("google", google_connection_manager);
                Intent intent = new Intent(this, typeof(PrescriptionDoneActivity));
                StartActivity(intent);
            }
        }

        /// <summary>
        /// This method updates the view/layout of this activity based on the current state
        /// of the prescription
        /// </summary>
        private void UpdateView()
        {
            if (prescription == null ||
                current_prescription_item_index >= prescription.Count ||
                prescription[current_prescription_item_index].Game == null)
            {
                return;
            }

            activityIndexText.Text = string.Empty;

            gameText.Text = prescription[current_prescription_item_index].Game.ExternalName;
            gameImage.SetImageResource(prescription[current_prescription_item_index].Game.GetAssetResource(this));

            //Set the text of the exercise
            string exercise_text = string.Empty;
            if (prescription[current_prescription_item_index].Exercise != ExerciseType.Unknown)
            {
                exercise_text = ExerciseTypeConverter.ConvertExerciseTypeToDescription(prescription[current_prescription_item_index].Exercise);
            }
            else if (prescription[current_prescription_item_index].Game.HasDefinedGameSpecificExercise())
            {
                exercise_text = prescription[current_prescription_item_index].Game.GetGameSpecificExercise();
            }

            exerciseText.Text = exercise_text;
            exercisePic.SetImageResource(prescription[current_prescription_item_index].GetExerciseImageResourceID(this));
            if (GameManager.Instance.IsRetrieve(prescription[current_prescription_item_index].Game))
            {
                //If the game is ReTrieve, we need to do some extra work...
                //First, we need to tell the user which sets they will need to use.
                var this_item_sets = GameManager.Instance.RetrieveSetIDstoSet(prescription[current_prescription_item_index].RetrieveSetIDs);

                if (this_item_sets.Count > 0)
                {
                    string result_text = "You will use the following sets: ";
                    for (int i = 0; i < this_item_sets.Count; i++)
                    {
                        if (i > 0)
                        {
                            result_text += ", ";
                        }

                        result_text += this_item_sets[i];
                    }

                    exerciseSubtext.Text = result_text;
                }
            }
            else
            {
                exerciseSubtext.Text = string.Empty;
            }

            //Set the name and image for the device that will be used for this assignment
            DeviceText.Text = prescription[current_prescription_item_index].Device.ToString();
            DeviceImage.SetImageResource(DeviceManager.Instance.GetImage(this, prescription[current_prescription_item_index].Device));
            DeviceInstructionText.Text = DeviceManager.Instance.GetDeviceInstruction(prescription[current_prescription_item_index].Device, prescription[current_prescription_item_index].Exercise);

            //If the game is Repetitions Mode we should show the user how many reps they will be asked to do
            if (GameManager.Instance.IsRepetitionsMode(prescription[current_prescription_item_index].Game))
            {
                DurationText.Text = prescription[current_prescription_item_index].Duration + " reps";
            }
            else
            {
                //Otherwise, we should show the user how much time they will be asked to spend playing the game
                string difficulty_result = "Time: " + prescription[current_prescription_item_index].Duration.ToString() + " min";
                if (prescription[current_prescription_item_index].Device == ExerciseDeviceType.Box)
                {
                    difficulty_result += ", Difficulty: " + prescription[current_prescription_item_index].Difficulty.ToString();
                }

                DurationText.Text = difficulty_result;
            }

            //Now update the buttons as needed
            UpdateButtonStates();
        }

        /// <summary>
        /// This method handles updating the enabled/disabled or visible/invisible state
        /// of the buttons, based upon where we are currently in the prescription
        /// </summary>
        private void UpdateButtonStates()
        {
            if (current_prescription_item_index == 0)
            {
                GoToPreviousGameButton.Visibility = Android.Views.ViewStates.Invisible;
                SkipGameButton.Enabled = true;
            }
            else
            {
                GoToPreviousGameButton.Visibility = Android.Views.ViewStates.Visible;
                SkipGameButton.Enabled = true;
            }

            if (prescription != null && current_prescription_item_index <= prescription.Count)
            {
                var exercise_type = prescription[current_prescription_item_index].Exercise;
                if (ExerciseManager.Instance.MapNameToVideo(exercise_type, this) != 0)
                {
                    Video.Visibility = Android.Views.ViewStates.Visible;
                }
                else
                {
                    Video.Visibility = Android.Views.ViewStates.Invisible;
                }
            }
        }

        #endregion

        #region Button click handlers

        /// <summary>
        /// Skips to the next prescription item
        /// </summary>
        private void Handle_SkipGame_ButtonClick(object sender, EventArgs e)
        {
            current_prescription_item_index++;
            UpdateState();
        }

        /// <summary>
        /// Goes back to the previous prescription item
        /// </summary>
        private void Handle_GoBackToPreviousGame_ButtonClick(object sender, EventArgs e)
        {
            if (current_prescription_item_index > 0)
            {
                current_prescription_item_index--;
            }

            UpdateState();
        }

        /// <summary>
        /// Handles the display of a video tutorial for this prescription item
        /// </summary>
        private void Handle_WatchTutorialVideo_ButtonClick(object sender, EventArgs e)
        {
            ExerciseManager em = ExerciseManager.Instance;
            int res = em.MapNameToVideo(prescription[current_prescription_item_index].Exercise, this);
            if (!VideoTutorialLaunched)
            {
                if (res != 0)
                {
                    VideoTutorialLaunched = true;
                    FragmentTransaction fm = FragmentManager.BeginTransaction();
                    VideoTutorialFragment dialog = VideoTutorialFragment.NewInstance(prescription[current_prescription_item_index].Exercise, this, res);
                    dialog.Show(fm, "dialog fragment");
                }
                else
                {
                    Toast.MakeText(this, "There is no tutorial for this exercise!", ToastLength.Short).Show();
                }
            }
        }

        /// <summary>
        /// Handles the launch of a new prescription item
        /// </summary>
        private void HandleStartGameButtonClick(object sender, EventArgs e)
        {
            if (!IsCurrentlyInGame)
            {
                var prescription_item = prescription[current_prescription_item_index];
                var prescription_vns_parameters = PrescriptionManager.Instance.CurrentPrescription.VNS;

                if (DeviceManager.Instance.CheckDeviceAttached(this, prescription_item.Device))
                {
                    if (DeviceManager.Instance.CheckDeviceAttachedAndPermissions(this, prescription_item.Device))
                    {
                        bool launch_game = false;
                        if (prescription_item.Device == ExerciseDeviceType.ReCheck)
                        {
                            launch_game = DeviceManager.Instance.CheckCorrectReCheckModuleAttached(this, prescription_item.Exercise);
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
                            prescription_item_start_time = DateTime.Now;
                            bool launch_success = GameLaunchManager.LaunchGame(this, prescription_item, prescription_vns_parameters, true);
                            if (prescription_item.Game.IsExternalApplication)
                            {
                                ExternalApplicationHasBeenLaunched = true;
                            }
                        }
                    }
                }
                else
                {
                    AlertDialog.Builder dialog = new AlertDialog.Builder(this);
                    AlertDialog alert = dialog.Create();
                    alert.SetTitle("Confirm");
                    string deviceMsg = DeviceManager.Instance.GetDeviceMessage(prescription[current_prescription_item_index].Device.ToString());
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
