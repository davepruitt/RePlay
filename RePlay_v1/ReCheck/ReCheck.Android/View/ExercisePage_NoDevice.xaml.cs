using Plugin.CurrentActivity;
using ReCheck.Droid.Model;
using ReCheck.Droid.ViewModel;
using ReCheck.Model;
using RePlay_DeviceCommunications;
using RePlay_Exercises;
using RePlay_VNS_Triggering;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;
using Android.Util;

namespace ReCheck.Droid.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ExercisePage_NoDevice : ContentPage
    {
        private ExercisePage_NoDevice_ViewModel view_model;

        private PCM_Manager restore_connection_manager;
        private ReplayMicrocontroller replay_microcontroller;
        private bool device_missing = false;
        private bool stop_exercise_button_pressed = false;
        private bool is_left_hand = false;

        private Participant participant = null;
        private ReCheckConfigurationModel current_configuration = null;

        private DateTime session_start_time = DateTime.MinValue;
        private DateTime session_end_time = DateTime.MinValue;

        private bool is_bad_calibration_information_msg_visible = false;


        public ExercisePage_NoDevice(PCM_Manager pcm, ReCheckConfigurationModel config, ReplayMicrocontroller replayMicrocontroller, Participant p)
        {
            InitializeComponent();
            
            replay_microcontroller = replayMicrocontroller;
            replay_microcontroller.PropertyChanged += HandleDeviceChangedEvents;

            restore_connection_manager = pcm;

            participant = p;
            current_configuration = config;

            var txbdc_green_color = (Xamarin.Forms.Color)Application.Current.Resources["txbdc_green"];

            view_model = new ExercisePage_NoDevice_ViewModel(replayMicrocontroller, participant, txbdc_green_color);
            BindingContext = view_model;

            CheckForDeviceWithBadCalibrationInformation();
        }

        private void LeftHandButton_Clicked(object sender, EventArgs e)
        {
            is_left_hand = true;
            AttemptStartExercise(true);
        }

        private void RightHandButton_Clicked(object sender, EventArgs e)
        {
            is_left_hand = false;
            AttemptStartExercise(false);
        }

        private async void AttemptStartExercise(bool leftHand)
        {
            //Just to be sure, before we attempt to start the exercise, let's check again to make sure
            //we have good calibration information.
            CheckForDeviceWithBadCalibrationInformation();

            //Now attempt to start the exercise.
            if (replay_microcontroller.CurrentDeviceType != ReplayDeviceType.Unknown &&
                !is_bad_calibration_information_msg_visible)
            {
                ExerciseType exerciseType = ExerciseTypeConverter.ConvertReplayDeviceTypeToExerciseType(replay_microcontroller.CurrentDeviceType);
                if (participant != null)
                {
                    participant.AddExerciseToCompletedExercisesList(exerciseType);
                }

                session_start_time = DateTime.Now;

                ExercisePage exercisePage = new ExercisePage(restore_connection_manager, current_configuration, replay_microcontroller, participant, leftHand);
                exercisePage.Disappearing += ExercisePage_Disappearing;
                exercisePage.StopExercisingEvent += ExercisePage_StopExercisingEvent;
                exercisePage.DeviceMissingEvent += ExercisePage_DeviceMissingEvent;

                Log.Debug("ExerciseStart", "Attempting to start exercise");
                await Navigation.PushModalAsync(exercisePage, false);
            }
        }

        private async void ExercisePage_Disappearing(object sender, EventArgs e)
        {
            ExercisePage ep = sender as ExercisePage;
            if (ep != null)
            {
                RepetitionsModel m = ep.GetAssessmentModel();
                if (m != null)
                {
                    //Update the total number of stims received during today's session.
                    view_model.UpdateTotalStimulations(m);

                    //Update whether or not the task is completed.
                    if (m.RepetitionsCompleted >= current_configuration.RepetitionsRequiredForTaskCompletion)
                    {
                        (double mean, double err) = m.CalculateTrials_MeanAndErr();
                        view_model.AddToCompletedList(m.ExerciseType, mean, err, is_left_hand);
                    }

                    if (participant != null)
                    {
                        //Display a "please wait" popup
                        await PopupNavigation.Instance.PushAsync(new Popup_PleaseWait(), true);

                        //Record the session end time
                        session_end_time = DateTime.Now;
                        TimeSpan session_duration = session_end_time - session_start_time;

                        if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                        {
                            await Task.Run(() =>
                            {
                                try
                                {
                                    //Save the data from this subject to the Google sheet
                                    RePlay_GoogleCommunications.RePlay_Google.UpdateSubjectFile(
                                        participant.ParticipantID,
                                        DateTime.Now,
                                        current_configuration.TabletIdentifier,
                                        "ReCheck",
                                        RePlay_Exercises.ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(m.ExerciseType),
                                        "NA",
                                        session_duration,
                                        (m.TotalPositiveAttempts + m.TotalNegativeAttempts).ToString());
                                }
                                catch (Exception ex)
                                {
                                    //empty
                                }
                            });       
                        }
                        
                        //Close the popup
                        await PopupNavigation.Instance.PopAsync(true);
                    }
                }
            }
        }

        private void ExercisePage_DeviceMissingEvent(object sender, EventArgs e)
        {
            device_missing = true;
        }

        private void ExercisePage_StopExercisingEvent(object sender, EventArgs e)
        {
            stop_exercise_button_pressed = true;
        }

        private void HandleDeviceChangedEvents(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("CurrentDeviceType"))
            {
                CheckForDeviceWithBadCalibrationInformation();
            }
        }

        private void CheckForDeviceWithBadCalibrationInformation ()
        {
            bool bad_calibration_information = false;

            if (replay_microcontroller != null)
            {
                if (replay_microcontroller.CurrentDeviceState == ReplayMicrocontroller.ControllerDeviceState.DeviceReady)
                {
                    if (replay_microcontroller.CurrentDeviceType != ReplayDeviceType.Unknown)
                    {
                        switch (replay_microcontroller.CurrentDeviceType)
                        {
                            /* Wrist, Knob, and Handle isometric devices use a single load cell and are handled
                             * in the same way. */

                            case ReplayDeviceType.Wrist_Isometric:
                            case ReplayDeviceType.Knob_Isometric:
                            case ReplayDeviceType.Handle_Isometric:

                                if (replay_microcontroller.Loadcell_1_Slope <= 1)
                                {
                                    bad_calibration_information = true;
                                }

                                break;

                            /* Pinch and Left-Pinch devices use 2 load cells and are handled identically to each
                             * other. */

                            case ReplayDeviceType.Pinch:
                            case ReplayDeviceType.Pinch_Left:

                                if (replay_microcontroller.Loadcell_1_Slope <= 1 ||
                                    replay_microcontroller.Loadcell_2_Slope <= 1)
                                {
                                    bad_calibration_information = true;
                                }

                                break;


                            /* All other devices do not use a load cell and do not matter here. */

                            default:
                                break;
                        }
                    }
                }
            }

            //If we have determined that there is bad calibration information, then let's display an error
            //message about it.
            if (bad_calibration_information)
            {
                if (!is_bad_calibration_information_msg_visible)
                {
                    is_bad_calibration_information_msg_visible = true;

                    //Display a popup indicating that the device needs to be calibrated.
                    var bad_calibration_popup = new Popup_BadCalibration();
                    bad_calibration_popup.Disappearing += (s, e) =>
                    {
                        is_bad_calibration_information_msg_visible = false;
                    };

                    PopupNavigation.Instance.PushAsync(bad_calibration_popup, true);
                }
            }
            else
            {
                if (is_bad_calibration_information_msg_visible)
                {
                    is_bad_calibration_information_msg_visible = false;

                    //Close the popup that tells the user to calibrate the device.
                    PopupNavigation.Instance.PopAsync(true);
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            PopThisPage();
            return true;
        }

        private void QuitAssessmentButton_Clicked(object sender, EventArgs e)
        {
            PopThisPage();
        }

        private async void PopThisPage ()
        {
            SaveReport();
            await Navigation.PopModalAsync();
        }

        private void SaveReport ()
        {
            view_model.SaveReport();
        }
    }
}