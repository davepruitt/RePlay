using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Android.App;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Microsoft.AppCenter.Analytics;
using RePlay.Activities;
using RePlay.CustomViews;
using RePlay.Entity;
using RePlay.Manager;
using RePlay_Exercises;

namespace RePlay.Fragments
{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0672 // Member overrides obsolete member
    public class AddNewPrescriptionItemFragment : DialogFragment
    {
        #region Events that callers can subscribe to

        public class PrescriptionItemEditEventArgs : EventArgs
        {
            public PrescriptionItem NewOrEdited_PrescriptionItem;
            public int Position;

            public PrescriptionItemEditEventArgs(PrescriptionItem p)
            {
                NewOrEdited_PrescriptionItem = p;
            }

            public PrescriptionItemEditEventArgs(PrescriptionItem p, int pos)
            {
                NewOrEdited_PrescriptionItem = p;
                Position = pos;
            }
        }

        public event EventHandler NewPrescriptionItemConfirmed;

        public event EventHandler EditPrescriptionItemConfirmed;

        public event EventHandler NewImmediateGameplayItemConfirmed;

        public event EventHandler DialogPausedOrCancelled;

        #endregion

        #region Enum for which type of fragment this is

        public enum AddPrescriptionItemMode
        {
            AddNewItemToPrescription,
            EditExistingItemOfPrescription,
            CreateItemForImmediateGameplay
        }

        private AddPrescriptionItemMode dialog_mode = AddPrescriptionItemMode.AddNewItemToPrescription;

        #endregion

        #region Private UI members

        //ReTrieve set selection UI pieces
        private View dialoglayout = null;
        private Button confirmSet = null;
        private ListView set_id_listview = null;
        private AlertDialog alert = null;

        //Main dialog UI pieces
        private TextView addExerciseText = null;
        private View rootView = null;
        private TextView deviceText = null;
        private Spinner deviceSpinner = null;
        private TextView gameText = null;
        private Spinner gameSpinner = null;
        private TextView exerciseText = null;
        private Spinner exerciseSpinner = null;
        private TextView difficultyText = null;
        private Spinner difficultySpinner = null;
        private TextView setText = null;
        private Button setButton = null;
        private LinearLayout timeWrapper = null;
        private Button addButton = null;
        private Button cancelButton = null;
        private NumberPicker timeNumberPicker = null;
        private TextView timeText = null;
        private TextView units = null;
        private TextView gainText = null;
        private EditText gainEditText = null;

        #endregion

        #region Private data members

        private int existing_item_position = 0;
        private bool preset_game_guard_flag = false;
        private bool preset_device_guard_flag = false;

        private PrescriptionItem prescription_item_to_build = new PrescriptionItem();
        private RePlayGame selected_game = null;
        private Activity CallerActivity;
        private bool SelectRetrieveSetLaunched = false;

        private const int MAX_TIME = 15;
        private const int MIN_TIME = 1;
        private const int MAX_REPS = 30;
        private const int MIN_REPS = 5;

        private string pre_selected_game_name = string.Empty;

        #endregion

        #region Constructor

        public AddNewPrescriptionItemFragment(Activity caller, AddPrescriptionItemMode mode, string game_internal_name = "")
        {
            CallerActivity = caller;
            dialog_mode = mode;
            pre_selected_game_name = game_internal_name;
        }

        public AddNewPrescriptionItemFragment(Activity caller, 
            AddPrescriptionItemMode mode, 
            PrescriptionItem existing_item,
            int position)
        {
            CallerActivity = caller;
            dialog_mode = mode;
            prescription_item_to_build = existing_item.DeepCopy();
            pre_selected_game_name = existing_item.Game.InternalName;
            existing_item_position = position;
        }

        #endregion

        #region OnCreate

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override void OnPause()
        {
            base.OnPause();
            DialogPausedOrCancelled?.Invoke(this, new EventArgs());
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            Dialog.SetCanceledOnTouchOutside(false);

            // Init layout objects
            rootView = inflater.Inflate(Resource.Layout.AddNewPrescriptionItem, container, false);
            addExerciseText = rootView.FindViewById<TextView>(Resource.Id.addExerciseText);
            deviceText = rootView.FindViewById<TextView>(Resource.Id.deviceText);
            deviceSpinner = rootView.FindViewById<Spinner>(Resource.Id.deviceSpinner);
            gameText = rootView.FindViewById<TextView>(Resource.Id.gameText);
            gameSpinner = rootView.FindViewById<Spinner>(Resource.Id.gameSpinner);
            exerciseText = rootView.FindViewById<TextView>(Resource.Id.exerciseText);
            exerciseSpinner = rootView.FindViewById<Spinner>(Resource.Id.exerciseSpinner);
            difficultyText = rootView.FindViewById<TextView>(Resource.Id.difficultyText);
            difficultySpinner = rootView.FindViewById<Spinner>(Resource.Id.diffSpinner);
            setText = rootView.FindViewById<TextView>(Resource.Id.setText);
            setButton = rootView.FindViewById<Button>(Resource.Id.selectSetButton);
            timeWrapper = rootView.FindViewById<LinearLayout>(Resource.Id.number_wheel_wrapper);
            addButton = rootView.FindViewById<Button>(Resource.Id.addButton);
            cancelButton = rootView.FindViewById<Button>(Resource.Id.cancelButton);
            timeNumberPicker = rootView.FindViewById<NumberPicker>(Resource.Id.timeNumberPicker);
            timeText = rootView.FindViewById<TextView>(Resource.Id.timeText);
            units = rootView.FindViewById<TextView>(Resource.Id.timeTextUnits);
            gainText = rootView.FindViewById<TextView>(Resource.Id.gainText);
            gainEditText = rootView.FindViewById<EditText>(Resource.Id.gainEditText);

            // Set all UI pieces to be invisible
            gameText.Visibility = ViewStates.Gone;
            gameSpinner.Visibility = ViewStates.Gone;
            deviceText.Visibility = ViewStates.Gone;
            deviceSpinner.Visibility = ViewStates.Gone;
            exerciseText.Visibility = ViewStates.Gone;
            exerciseSpinner.Visibility = ViewStates.Gone;
            difficultyText.Visibility = ViewStates.Gone;
            difficultySpinner.Visibility = ViewStates.Gone;
            timeText.Visibility = ViewStates.Gone;
            timeWrapper.Visibility = ViewStates.Gone;
            units.Visibility = ViewStates.Gone;
            gainText.Visibility = ViewStates.Gone;
            gainEditText.Visibility = ViewStates.Gone;
            addButton.Visibility = ViewStates.Gone;

            //Set methods that will be called when things in the GUI are changed by the user
            gameSpinner.ItemSelected += HandleGameSelected_FromGUI;
            deviceSpinner.ItemSelected += HandleDeviceSelected;
            gainEditText.TextChanged += HandleGainChanged;
            gainEditText.EditorAction += GainEditText_EditorAction;
            gainEditText.FocusChange += GainEditText_FocusChange;
            timeNumberPicker.ValueChanged += HandleDurationChanged;
            setButton.Click += HandleSelectSetButtonClick;
            difficultySpinner.ItemSelected += HandleDifficultySelected;
            exerciseSpinner.ItemSelected += HandleExerciseChanged;
            cancelButton.Click += HandleDialogCancelled;
            addButton.Click += HandleDialogConfirm;

            //Set the items that will exist in the game selection dropdown box
            var game_name_list = GameManager.Instance.Games.Select(x => x.ExternalName).ToList();
            game_name_list.Add("Select a game");
            gameSpinner.Adapter = new HintArrayAdapter(CallerActivity, Android.Resource.Layout.SimpleSpinnerDropDownItem, game_name_list);
            gameSpinner.SetSelection(game_name_list.Count - 1);

            //Check to see what mode this dialog has been opened in...
            if (dialog_mode == AddPrescriptionItemMode.CreateItemForImmediateGameplay)
            {
                //If we are creating a new item that will be played immediately...

                //Grab the game from the game manager
                selected_game = GameManager.Instance.Games.Where(x => x.InternalName.Equals(pre_selected_game_name)).FirstOrDefault();
                if (selected_game != null)
                {
                    //Set some GUI elements
                    addExerciseText.Text = selected_game.ExternalName;
                    addButton.Text = "BEGIN";
                    gameText.Visibility = ViewStates.Gone;
                    gameSpinner.Visibility = ViewStates.Gone;
                    gameSpinner.Adapter = new ArrayAdapter<string>(CallerActivity, 
                        Android.Resource.Layout.SimpleSpinnerDropDownItem, 
                        new List<string>() { selected_game.ExternalName } );
                    gameSpinner.SetSelection(0);
                    gameSpinner.Enabled = false;

                    //Now handle things as if we had selected a game
                    HandleGameSelected_Externally();
                }
            }
            else if (dialog_mode == AddPrescriptionItemMode.EditExistingItemOfPrescription)
            {
                //If we are editing an existing prescription item...

                //Set some GUI elements
                addExerciseText.Text = "EDIT ITEM OF ASSIGNMENT";
                addButton.Text = "CONFIRM";

                //Set the selected game
                selected_game = prescription_item_to_build.Game;

                //Get the external name of the selected game
                PopulateGUIWithInitialValues();

                //Set a guard flag so we don't respond to undesireable Android "selected" events
                preset_game_guard_flag = true;
                preset_device_guard_flag = true;
            }
            else
            {
                //If we are creating a brand new prescription item...

                //Set some GUI elements 
                addExerciseText.Text = "ADD AN ITEM TO THE ASSIGNMENT";
                addButton.Text = "ADD";

                //Set the visibility of the game selection spinner
                gameText.Visibility = ViewStates.Visible;
                gameSpinner.Visibility = ViewStates.Visible;
            }

            return rootView;
        }

        private void PopulateGUIWithInitialValues ()
        {
            //Populate the game name list and select the game that is already chosen for this prescription item
            gameText.Visibility = ViewStates.Visible;
            gameSpinner.Visibility = ViewStates.Visible;
            var game_name_list = GameManager.Instance.Games.Select(x => x.ExternalName).ToList();
            game_name_list.Add("Select a game");

            var selected_game_external_name = selected_game.ExternalName;
            var selected_game_name_idx = game_name_list.IndexOf(selected_game_external_name);
            if (selected_game_name_idx > -1)
            {
                gameSpinner.SetSelection(selected_game_name_idx);
            }

            //Now set up all the other GUI elements
            SetupDeviceSpinner(true);
            SetupExerciseSpinner(true);
            SetupDifficultySpinner(true);
            SetupRetrieveSetSelector(true);
            SetupDurationPicker(true);
            SetupGainEditor(true);

            //Make the add button visible
            addButton.Visibility = ViewStates.Visible;
        }

        private int SetupDeviceSpinner (bool preselect = false)
        {
            int result = 0;

            if (selected_game != null)
            {
                //Copy the list of the supported devices for the currently selected game
                var supported_devices = selected_game.SupportedDevices.ToList();

                //We will return the number of devices supported by this game to the caller
                result = supported_devices.Count;

                //If there is more than one device supported by the selected game...
                if (supported_devices.Count > 1)
                {
                    //Add a label to the top of the list
                    supported_devices.Add("Select a device");

                    //Set up the device spinner
                    deviceSpinner.Adapter = new HintArrayAdapter(CallerActivity, Android.Resource.Layout.SimpleSpinnerDropDownItem, supported_devices);
                    deviceSpinner.SetSelection(supported_devices.Count - 1);
                    deviceSpinner.Enabled = true;
                    if (preselect)
                    {
                        var preselected_device = prescription_item_to_build.Device;
                        var pre_device_str = ExerciseDeviceTypeConverter.ConvertExerciseDeviceTypeToDescription(preselected_device);
                        var idx = supported_devices.FindIndex(x => x.Equals(pre_device_str));
                        if (idx > -1)
                        {
                            deviceSpinner.SetSelection(idx);
                        }
                    }
                    
                    //Make the device spinner visible
                    deviceText.Visibility = ViewStates.Visible;
                    deviceSpinner.Visibility = ViewStates.Visible;
                }
                else
                {
                    //If only 1 device is supported by the game:
                    deviceSpinner.Adapter = new ArrayAdapter<string>(CallerActivity, Android.Resource.Layout.SimpleSpinnerDropDownItem, supported_devices);
                    deviceSpinner.SetSelection(0);
                    deviceSpinner.Enabled = false;

                    //Make the device spinner visible
                    deviceText.Visibility = ViewStates.Visible;
                    deviceSpinner.Visibility = ViewStates.Visible;
                }
            }

            return result;
        }

        private string SetupExerciseSpinner (bool preselect = false)
        {
            string result = string.Empty;

            if (selected_game != null)
            {
                //Get the list of all exercises that this game supports
                var all_supported_exercises = selected_game.SupportedExercises.ToList();

                //Now let's convert the list to their respective enum types
                var exercises_as_enum = all_supported_exercises.Select(x => ExerciseTypeConverter.ConvertEnumMemberStringToExerciseType(x)).ToList();

                //Now let's find the device that each exercise uses
                var device_for_each_exercise = exercises_as_enum.Select(x => ExerciseTypeConverter.GetExerciseDeviceType(x)).ToList();

                //Now let's get the UI-facing string representation of each exercise
                var all_supported_exercises_ui_facing_str = exercises_as_enum.Select(x => ExerciseTypeConverter.ConvertExerciseTypeToDescription(x)).ToList();

                //Now let's populate a list of exercise names that this game/device combo can support
                List<string> final_exercise_list_for_ui = new List<string>();
                for (int i = 0; i < device_for_each_exercise.Count; i++)
                {
                    if (device_for_each_exercise[i] == prescription_item_to_build.Device)
                    {
                        final_exercise_list_for_ui.Add(all_supported_exercises_ui_facing_str[i]);
                    }
                }

                //If this game supports multiple exercises that the user can choose from...
                if (final_exercise_list_for_ui.Count > 1)
                {
                    //We will return the first exercise in the list to the caller
                    //This is meant to be the "default" selection
                    result = final_exercise_list_for_ui[0];

                    //Now let's set up the exercise spinner
                    exerciseSpinner.Adapter = new ArrayAdapter<string>(CallerActivity, Android.Resource.Layout.SimpleSpinnerDropDownItem, final_exercise_list_for_ui);
                    exerciseSpinner.Enabled = true;
                    exerciseSpinner.SetSelection(0);
                    if (preselect)
                    {
                        var preselected_exercise = prescription_item_to_build.Exercise;
                        var pre_exercise_str_ui_facing = ExerciseTypeConverter.ConvertExerciseTypeToDescription(preselected_exercise);
                        var idx = final_exercise_list_for_ui.FindIndex(x => x.Equals(pre_exercise_str_ui_facing));
                        if (idx > -1)
                        {
                            exerciseSpinner.SetSelection(idx);
                        }
                    }

                    //Make the exercise spinner visible
                    exerciseText.Visibility = ViewStates.Visible;
                    exerciseSpinner.Visibility = ViewStates.Visible;
                }
                else
                {
                    //Otherwise, if the user cannot choose an exercise...

                    //First, check to see if there is ONE element in the exercise list
                    if (final_exercise_list_for_ui.Count == 1)
                    {
                        //Show the exercise in the spinner, but disable the spinner
                        exerciseSpinner.Adapter = new ArrayAdapter<string>(CallerActivity, Android.Resource.Layout.SimpleSpinnerDropDownItem, final_exercise_list_for_ui);
                        exerciseSpinner.SetSelection(0);
                        exerciseSpinner.Enabled = false;

                        //Make the exercise spinner visible
                        exerciseText.Visibility = ViewStates.Visible;
                        exerciseSpinner.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        //Don't show the exercise spinner. Make sure it's disabled.
                        exerciseSpinner.Enabled = false;
                        exerciseText.Visibility = ViewStates.Gone;
                        exerciseSpinner.Visibility = ViewStates.Gone;
                    }
                }
            }

            return result;
        }

        private void SetupDifficultySpinner (bool preselect = false)
        {
            //Next, we need to determine whether to show the difficulty spinner or not
            if (selected_game != null &&
                selected_game.UsesDifficulty &&
                selected_game.DifficultyLevels != null &&
                selected_game.DifficultyLevels.Count > 0)
            {
                //If this game allows for the selection of difficulty levels, let's show the difficulty spinner
                difficultyText.Visibility = ViewStates.Visible;
                difficultySpinner.Visibility = ViewStates.Visible;

                //And let's populate the difficulty spinner with the appropriate selection items
                var available_difficulties = selected_game.DifficultyLevels.ToList();
                difficultySpinner.Adapter = new ArrayAdapter<string>(CallerActivity, Android.Resource.Layout.SimpleSpinnerDropDownItem, available_difficulties);
                difficultySpinner.SetSelection(0);
                if (preselect)
                {
                    var preselected_difficulty = prescription_item_to_build.Difficulty.ToString();
                    var idx = available_difficulties.FindIndex(x => x.Equals(preselected_difficulty));
                    if (idx > -1)
                    {
                        difficultySpinner.SetSelection(idx);
                    }
                }
            }
            else
            {
                //Make the difficulty spinner invisible if this game doesn't use difficulty settings
                difficultyText.Visibility = ViewStates.Gone;
                difficultySpinner.Visibility = ViewStates.Gone;
            }
        }

        private void SetupDurationPicker (bool preselect = false)
        {
            if (selected_game != null)
            {
                //Now we need to handle the spinner that shows the amount of time or the number of reps that will be done
                //during this exercise
                if (GameManager.Instance.IsRepetitionsMode(selected_game))
                {
                    timeText.Text = "Reps:";
                    units.Text = "reps";
                    timeNumberPicker.MinValue = MIN_REPS;
                    timeNumberPicker.MaxValue = MAX_REPS;
                    timeNumberPicker.Value = MIN_REPS;
                    timeNumberPicker.WrapSelectorWheel = true;
                }
                else
                {
                    timeText.Text = "Time:";
                    units.Text = "minute(s)";
                    timeNumberPicker.MinValue = MIN_TIME;
                    timeNumberPicker.MaxValue = MAX_TIME;
                    timeNumberPicker.Value = MIN_TIME;
                    timeNumberPicker.WrapSelectorWheel = true;
                }

                timeNumberPicker.Visibility = ViewStates.Visible;
                timeText.Visibility = ViewStates.Visible;
                units.Visibility = ViewStates.Visible;
                timeWrapper.Visibility = ViewStates.Visible;

                if (preselect)
                {
                    timeNumberPicker.Value = prescription_item_to_build.Duration;
                }
            }
        }

        private void SetupGainEditor (bool preselect = false)
        {
            //Finally, we need to allow the user to set the gain
            //This only applies to certain games that allow gain changes.
            if (selected_game != null && selected_game.UsesGain)
            {
                //Make the gain text visible
                gainText.Visibility = ViewStates.Visible;
                gainEditText.Visibility = ViewStates.Visible;

                if (preselect)
                {
                    gainEditText.Text = prescription_item_to_build.Gain.ToString();
                }
                else
                {
                    gainEditText.Text = "1.0";
                }
            }
        }

        private void SetupRetrieveSetSelector (bool preselect = false)
        {
            //Now we need to handle the UI that allows the user to select ReTrieve sets (if this game is ReTrieve)
            if (selected_game != null && GameManager.Instance.IsRetrieve(selected_game))
            {
                //Make the ReTrieve "set" button visible, allowing the user to select sets for ReTrieve
                setText.Visibility = ViewStates.Visible;
                setButton.Visibility = ViewStates.Visible;

                if (preselect)
                {
                    var set_ids = prescription_item_to_build.RetrieveSetIDs;
                    if (set_ids != null && set_ids.Count > 0)
                    {
                        //Now create a unified string of all selected sets to display in the GUI
                        var set_names = GameManager.Instance.RetrieveSetIDstoSet(set_ids);
                        var str = string.Join(", ", set_names);
                        var str_modified = (str.Length <= 25) ? str : str.Substring(0, 24) + "...";
                        setButton.Text = str_modified;
                    }
                }
            }
            else
            {
                //If this game is not ReTrieve, then we should not show the set button
                setText.Visibility = ViewStates.Gone;
                setButton.Visibility = ViewStates.Gone;
            }
        }

        private void HandleGameSelected_FromGUI (object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if (gameSpinner.SelectedItem != null)
            {
                var game_external_name = gameSpinner.SelectedItem.ToString();
                selected_game = GameManager.Instance.Games.Where(x => x.ExternalName.Equals(game_external_name)).FirstOrDefault();
                if (selected_game != null)
                {
                    HandleGameSelected();
                }
            }
        }

        private void HandleGameSelected_Externally ()
        {
            HandleGameSelected();
        }

        private void HandleGameSelected ()
        {
            if (preset_game_guard_flag)
            {
                preset_game_guard_flag = false;
                return;
            }

            if (selected_game != null)
            {
                //Set the game in the prescription item we are building
                prescription_item_to_build.GameName = selected_game.InternalName;

                //Set up the device spinner
                int num_supported_devices = SetupDeviceSpinner();

                //Hide all lower views
                exerciseText.Visibility = ViewStates.Gone;
                exerciseSpinner.Visibility = ViewStates.Gone;
                difficultyText.Visibility = ViewStates.Gone;
                difficultySpinner.Visibility = ViewStates.Gone;
                timeText.Visibility = ViewStates.Gone;
                timeWrapper.Visibility = ViewStates.Gone;
                units.Visibility = ViewStates.Gone;
                addButton.Visibility = ViewStates.Gone;
                gainText.Visibility = ViewStates.Gone;
                gainEditText.Visibility = ViewStates.Gone;

                //If this game is ReTrieve, disable the begin button by default
                if (GameManager.Instance.IsRetrieve(selected_game))
                {
                    prescription_item_to_build.RetrieveSetIDs = new List<int>();
                    addButton.SetBackgroundResource(Resource.Color.txbdc_lightgrey);
                    addButton.Enabled = false;
                }

                //Automatically handle the device selection
                if (num_supported_devices <= 1)
                {
                    HandleDeviceSelected(deviceSpinner, new AdapterView.ItemSelectedEventArgs(null, null, 0, 0));
                }
            }
        }

        private void HandleDeviceSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if (preset_device_guard_flag)
            {
                preset_device_guard_flag = false;
                return;
            }

            //After a device is selected, we need to show all the exercises that can be done with that
            //game and device combination.
            if (selected_game != null)
            {
                //Set the device in the prescription item we are building
                if (deviceSpinner.SelectedItem != null)
                {
                    var selected_device_name = deviceSpinner.SelectedItem.ToString();
                    var selected_device_type = ExerciseDeviceTypeConverter.ConvertDescriptionToExerciseDeviceType(selected_device_name);
                    prescription_item_to_build.Device = selected_device_type;
                }
                else
                {
                    if (selected_game.SupportedDevices != null && selected_game.SupportedDevices.Count > 0)
                    {
                        prescription_item_to_build.Device = ExerciseDeviceTypeConverter.ConvertDescriptionToExerciseDeviceType(selected_game.SupportedDevices[0]);
                    }
                }
                
                if (prescription_item_to_build.Device != ExerciseDeviceType.Unknown)
                {
                    //Set up the exercise spinner
                    string default_exercise_selection_ui_facing_str = SetupExerciseSpinner();
                    prescription_item_to_build.Exercise = ExerciseTypeConverter.ConvertDescriptionToExerciseType(default_exercise_selection_ui_facing_str);

                    //After we have finished working on the exercise spinner, now let's work on the other UI elements
                    SetupDifficultySpinner();
                    prescription_item_to_build.Difficulty = 1;

                    SetupRetrieveSetSelector();
                    prescription_item_to_build.RetrieveSetIDs = new List<int>();

                    //Setup the duration/rep count picker
                    SetupDurationPicker();
                    if (GameManager.Instance.IsRepetitionsMode(prescription_item_to_build.Game))
                    {
                        prescription_item_to_build.Duration = MIN_REPS;
                    }
                    else
                    {
                        prescription_item_to_build.Duration = MIN_TIME;
                    }
                    
                    //Setup the gain editor
                    SetupGainEditor();
                    prescription_item_to_build.Gain = 1.0;
                }

                //The add button should be made visible after a device is selcted
                addButton.Visibility = ViewStates.Visible;
            }
        }

        private void HandleGainChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            string current_gain_text = gainEditText.Text;
            bool success = double.TryParse(current_gain_text, out double result);
            if (success)
            {
                prescription_item_to_build.Gain = result;
            }
        }

        private void HandleDurationChanged(object sender, NumberPicker.ValueChangeEventArgs e)
        {
            //Set the new duration of the prescription item we are building
            prescription_item_to_build.Duration = timeNumberPicker.Value;
        }

        private void HandleDifficultySelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            //Grab the difficulty string
            var difficulty_string = difficultySpinner.SelectedItem.ToString();

            //Attempt to the convert the difficulty to an integer
            var success = Int32.TryParse(difficulty_string, out int result);
            if (success)
            {
                prescription_item_to_build.Difficulty = result;
            }
        }

        private void HandleExerciseChanged(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            //Grab the exercise type
            var selected_exercise_string = exerciseSpinner.SelectedItem.ToString();
            var selected_exercise = ExerciseTypeConverter.ConvertDescriptionToExerciseType(selected_exercise_string);

            //Set the exercise in the prescription we are building
            prescription_item_to_build.Exercise = selected_exercise;
        }

        private void HandleSelectSetButtonClick(object sender, EventArgs e)
        {
            if (selected_game != null && 
                GameManager.Instance.IsRetrieve(selected_game) && 
                !SelectRetrieveSetLaunched)
            {
                //Create a dialog that will present the set options to the user
                LayoutInflater retrieve_inflater = CallerActivity.LayoutInflater;
                dialoglayout = retrieve_inflater.Inflate(Resource.Layout.SelectRetrieveSetIds, null);

                AlertDialog.Builder dialog = new AlertDialog.Builder(CallerActivity);
                dialog.SetView(dialoglayout);
                alert = dialog.Create();
                alert.Window.RequestFeature(WindowFeatures.NoTitle);
                alert.CancelEvent += HandleSelectedSetsCancellation;

                //Define the behavior that occurs when the user confirms what sets they want
                confirmSet = dialoglayout.FindViewById<Button>(Resource.Id.confirmSet);
                confirmSet.Click += HandleSelectedSetsConfirmation;

                //Get the view object that will store the set ID selections
                set_id_listview = dialoglayout.FindViewById<ListView>(Resource.Id.setIDs);

                //Filter the ReTrieve sets by difficulty
                string difficulty_spinner_selected_string = string.Empty;
                try
                {
                    difficulty_spinner_selected_string = (string)difficultySpinner.SelectedItem;
                }
                catch (System.Exception)
                {
                    //empty
                }
                
                bool difficulty_parse_success = Int32.TryParse(difficulty_spinner_selected_string, out int selected_difficulty);
                if (!difficulty_parse_success)
                {
                    selected_difficulty = 1;
                }
                
                var filtered_retrieve_sets = GameManager.Instance.GetRetrieveSetsThatMatchDifficultyLevel(selected_game, selected_difficulty);

                //Set the content of the list-view to be the sets that the user can choose from
                set_id_listview.Adapter = new ArrayAdapter<string>(Context, Android.Resource.Layout.SimpleListItemMultipleChoice, filtered_retrieve_sets);
                set_id_listview.ChoiceMode = ChoiceMode.Multiple;

                //Set the checked items in the list-view based upon what sets were already chosen.
                if (prescription_item_to_build.RetrieveSetIDs != null)
                {
                    var set_names = GameManager.Instance.RetrieveSetIDstoSet(prescription_item_to_build.RetrieveSetIDs);
                    for (int i = 0; i < set_names.Count; i++)
                    {
                        var idx_in_listview = filtered_retrieve_sets.FindIndex(x => x.Equals(set_names[i]));
                        if (idx_in_listview > -1)
                        {
                            set_id_listview.SetItemChecked(idx_in_listview, true);
                        }
                    }
                }

                //Now show the dialog
                alert.Show();
            }
        }

        private void HandleSelectedSetsCancellation(object sender, EventArgs e)
        {
            SelectRetrieveSetLaunched = false;
        }

        private void HandleSelectedSetsConfirmation(object sender, EventArgs e)
        {
            //Check if nothing was selected
            if (set_id_listview.CheckedItemCount == 0)
            {
                //Make sure that the list of selected sets is empty
                if (prescription_item_to_build.RetrieveSetIDs != null)
                {
                    prescription_item_to_build.RetrieveSetIDs.Clear();
                }

                //Make sure the button text of the setButton is correct.
                setButton.Text = "Select Set";
                addButton.SetBackgroundResource(Resource.Color.txbdc_lightgrey);
                addButton.Enabled = false;
            }
            else
            {
                //Now, if some items were selected...

                //Grab the list of selected items by position
                var checked_items = set_id_listview.CheckedItemPositions;

                //Now create a new empty list to store the names of the newly selected sets
                var selected_retrieve_sets = new List<string>();

                //Now iterate over each selected item
                for (int i = 0; i < checked_items.Size(); i++)
                {
                    if (checked_items.ValueAt(i) == true)
                    {
                        //Get the name of the set that was selected
                        string set_name = set_id_listview.Adapter.GetItem(checked_items.KeyAt(i)).ToString();

                        //Add the name of that selected set to the list of all selected sets
                        selected_retrieve_sets.Add(set_name);
                    }
                }

                //Now get each of the set IDs
                var selected_set_ids = GameManager.Instance.RetrieveSetsToSetIDs(selected_retrieve_sets);

                //Now copy the list of set IDs to the prescription item that we are building.
                prescription_item_to_build.RetrieveSetIDs = selected_set_ids.ToList();

                //Now create a unified string of all selected sets to display in the GUI
                var str = string.Join(", ", selected_retrieve_sets);
                var str_modified = (str.Length <= 25) ? str : str.Substring(0, 24) + "...";
                setButton.Text = str_modified;

                //Enable the add button
                addButton.SetBackgroundResource(Resource.Drawable.txbdc_button_background_selector);
                addButton.Enabled = true;
            }

            //Finally, dismiss the current dialog
            alert.Dismiss();
            SelectRetrieveSetLaunched = false;
        }

        private void HandleDialogConfirm(object sender, EventArgs e)
        {
            if (dialog_mode == AddPrescriptionItemMode.AddNewItemToPrescription)
            {
                NewPrescriptionItemConfirmed?.Invoke(this, 
                    new PrescriptionItemEditEventArgs(prescription_item_to_build));
            }
            else if (dialog_mode == AddPrescriptionItemMode.EditExistingItemOfPrescription)
            {
                EditPrescriptionItemConfirmed?.Invoke(this,
                    new PrescriptionItemEditEventArgs(prescription_item_to_build, existing_item_position));
            }
            else
            {
                NewImmediateGameplayItemConfirmed?.Invoke(this,
                    new PrescriptionItemEditEventArgs(prescription_item_to_build));
            }

            Dismiss();
        }

        private void HandleDialogCancelled(object sender, EventArgs e)
        {
            DialogPausedOrCancelled?.Invoke(this, new EventArgs());
            Dismiss();
        }

        private void GainEditText_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Done ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Send ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Search ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Go)
            {
                ValidateGainText(sender as EditText);
            }
        }

        private void GainEditText_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (!e.HasFocus)
            {
                ValidateGainText(sender as EditText);
            }
        }

        private void ValidateGainText (EditText gainEditText)
        {
            if (gainEditText != null)
            {
                bool success = double.TryParse(gainEditText.Text, out double gainEditTextResult);
                if (success)
                {
                    if (gainEditTextResult < 0.0001)
                    {
                        gainEditText.Text = "0.0001";
                    }
                    else if (gainEditTextResult > 10000)
                    {
                        gainEditText.Text = "10000";
                    }
                }
                else
                {
                    gainEditText.Text = "1";
                }
            }
        }

        #endregion
    }
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0672 // Member overrides obsolete member
}