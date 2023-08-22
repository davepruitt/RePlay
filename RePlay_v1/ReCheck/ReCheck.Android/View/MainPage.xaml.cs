using Plugin.CurrentActivity;
using ReCheck.Droid.Model;
using ReCheck.Droid.ViewModel;
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

namespace ReCheck.Droid.View
{
    public partial class MainPage : ContentPage
    {
        private enum NavigationFlagValues
        {
            None,
            Assessment,
            Settings
        }

        private NavigationFlagValues navigation_flag = NavigationFlagValues.None;

        private DeviceManager device_manager;
        private ReCheckConfigurationModel assessment_configuration_model;

        private ReplayMicrocontroller replay_microcontroller = new ReplayMicrocontroller(CrossCurrentActivity.Current.Activity);
        private PCM_Manager restore_connection_manager = new PCM_Manager(CrossCurrentActivity.Current.Activity);

        private Assessment_LoginPage loginPage = new Assessment_LoginPage();
        private Assessment_ParticipantSelectionPage participantSelectionPage;

        public MainPage(DeviceManager deviceManager, ReCheckConfigurationModel assessmentModel)
        {
            InitializeComponent();
            device_manager = deviceManager;
            assessment_configuration_model = assessmentModel;

            loginPage.Disappearing += LoginPage_Disappearing;

            BindingContext = new MainPageViewModel(assessment_configuration_model, restore_connection_manager);
        }

        private async void ExerciseButton_Clicked(object sender, EventArgs e)
        {
            if (device_manager.CheckDeviceAttached(CrossCurrentActivity.Current.Activity, ExerciseDeviceType.ReCheck))
            {
                if (device_manager.CheckDeviceAttachedAndPermissions(CrossCurrentActivity.Current.Activity, ExerciseDeviceType.ReCheck))
                {
                    if (!replay_microcontroller.IsConnectionOpen())
                    {
                        replay_microcontroller.Open();
                    }

                    await Navigation.PushModalAsync(new ExercisePage_NoDevice(restore_connection_manager, assessment_configuration_model, replay_microcontroller, null), false);
                }
            }
            else
            {
                await PopupNavigation.Instance.PushAsync(new Popup_DeviceNotFound(), true);
            }
        }

        private async void AssessmentButton_Clicked(object sender, EventArgs e)
        {
            if (device_manager.CheckDeviceAttached(CrossCurrentActivity.Current.Activity, ExerciseDeviceType.ReCheck))
            {
                if (device_manager.CheckDeviceAttachedAndPermissions(CrossCurrentActivity.Current.Activity, ExerciseDeviceType.ReCheck))
                {
                    if (!replay_microcontroller.IsConnectionOpen())
                    {
                        replay_microcontroller.Open();
                    }

                    navigation_flag = NavigationFlagValues.Assessment;
                    await Navigation.PushModalAsync(loginPage, false);
                }
            }
            else
            {
                await PopupNavigation.Instance.PushAsync(new Popup_DeviceNotFound(), true);
            }
        }

        private void LoginPage_Disappearing(object sender, EventArgs e)
        {
            if (loginPage != null && loginPage.IsPasswordCorrect)
            {
                loginPage.ResetLoginPage();

                if (navigation_flag == NavigationFlagValues.Assessment)
                {
                    //Bring up the "participant selection page"
                    participantSelectionPage = new Assessment_ParticipantSelectionPage(assessment_configuration_model);
                    participantSelectionPage.Disappearing += ParticipantSelectionPage_Disappearing;
                    Navigation.PushModalAsync(participantSelectionPage, false);
                }
                else if (navigation_flag == NavigationFlagValues.Settings)
                {
                    Navigation.PushModalAsync(new SettingsPage(assessment_configuration_model), false);
                }

                navigation_flag = NavigationFlagValues.None;
            }
        }

        private void ParticipantSelectionPage_Disappearing(object sender, EventArgs e)
        {
            Assessment_ParticipantSelectionPage selectionPage = sender as Assessment_ParticipantSelectionPage;

            if (selectionPage != null)
            {
                if (!string.IsNullOrEmpty(assessment_configuration_model.ParticipantID))
                {
                    Participant p = new Participant()
                    {
                        ParticipantID = assessment_configuration_model.ParticipantID
                    };

                    StartAssessment(p);
                }
            }
        }

        private async void StartAssessment (Participant p)
        {
            if (p != null)
            {
                if (device_manager.CheckDeviceAttached(CrossCurrentActivity.Current.Activity, ExerciseDeviceType.ReCheck))
                {
                    if (device_manager.CheckDeviceAttachedAndPermissions(CrossCurrentActivity.Current.Activity, ExerciseDeviceType.ReCheck))
                    {
                        //Display a "please wait" popup
                        await PopupNavigation.Instance.PushAsync(new Popup_PleaseWait(), true);

                        //Make sure that files exist on Google Sheets for the participant that we are to run an assessment for
                        await Task.Run(() =>
                        {
                            try
                            {
                                bool does_subject_file_exist = RePlay_GoogleCommunications.RePlay_Google.CheckIfSubjectFileExists(p.ParticipantID);
                                if (!does_subject_file_exist)
                                {
                                    RePlay_GoogleCommunications.RePlay_Google.CreateSubjectFile(p.ParticipantID);
                                    RePlay_GoogleCommunications.RePlay_Google.AddParticipantToProjectFile(p.ParticipantID);
                                }
                            }
                            catch (Exception ex)
                            {
                                //empty
                            }
                        });


                        if (!replay_microcontroller.IsConnectionOpen())
                        {
                            replay_microcontroller.Open();
                        }

                        //Push the new page
                        await Navigation.PushModalAsync(
                            new ExercisePage_NoDevice(
                                restore_connection_manager, 
                                assessment_configuration_model, 
                                replay_microcontroller, 
                                p), false);

                        //Close the "pease wait" popup
                        await PopupNavigation.Instance.PopAsync(true);
                    }
                }
                else
                {
                    await PopupNavigation.Instance.PushAsync(new Popup_DeviceNotFound(), true);
                }
            }
        }

        private void RecheckImageButton_Pressed(object sender, EventArgs e)
        {
            var btn = sender as ImageButton;
            if (btn != null)
            {
                btn.ScaleTo(0.67, 50, Easing.Linear);
            }
        }

        private void RecheckImageButton_Released(object sender, EventArgs e)
        {
            var btn = sender as ImageButton;
            if (btn != null)
            {
                btn.ScaleTo(1.0, 50, Easing.Linear);
            }
        }

        private async void RecheckSettingsImageButton_Clicked(object sender, EventArgs e)
        {
            navigation_flag = NavigationFlagValues.Settings;
            await Navigation.PushModalAsync(loginPage, false);
        }

        private void RecheckPCMConnectionImageButton_Clicked(object sender, EventArgs e)
        {
            restore_connection_manager.CheckPCMStatus();
        }

        private void QuitButton_Clicked(object sender, EventArgs e)
        {
            Android.Content.Intent startMain = new Android.Content.Intent(Android.Content.Intent.ActionMain);
            startMain.AddCategory(Android.Content.Intent.CategoryHome);
            startMain.SetFlags(Android.Content.ActivityFlags.NewTask);
            CrossCurrentActivity.Current.Activity.StartActivity(startMain);
        }
    }
}
