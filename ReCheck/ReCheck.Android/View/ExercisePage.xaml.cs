using Microcharts;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Plugin.CurrentActivity;
using ReCheck.Droid.Model;
using ReCheck.Model;
using ReCheck.ViewModel;
using RePlay_Common;
using RePlay_DeviceCommunications;
using RePlay_Exercises;
using RePlay_VNS_Triggering;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Timer = System.Timers.Timer;

namespace ReCheck.Droid.View
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ExercisePage : ContentPage
	{
        #region Public events

        public event EventHandler StopExercisingEvent;
        public event EventHandler DeviceMissingEvent;

        #endregion

        #region Private data members

        ExercisePageViewModel exercise_viewmodel;
        
        Timer update_timer;

        DateTime last_centering_time = DateTime.MinValue;
        TimeSpan centering_duration = TimeSpan.FromMilliseconds(500);
        bool currently_centering = false;
        object centering_lock = new object();

        private PCM_Manager restore_connection_manager;
        
        #endregion

        public ExercisePage (PCM_Manager pcm, ReCheckConfigurationModel config, ReplayMicrocontroller replayMicrocontroller, Participant p, bool isUsingLeftHand)
		{
			InitializeComponent ();

            restore_connection_manager = pcm;

            //Create a touch gesture recognizer for the UI
            TapGestureRecognizer touchGestureRecognizer = new TapGestureRecognizer();
            touchGestureRecognizer.Tapped += HandleScreenTouchEvent;
            PrimaryGrid.GestureRecognizers.Add(touchGestureRecognizer);

            //Instantiate the view-model
            exercise_viewmodel = new ExercisePageViewModel(pcm, config, replayMicrocontroller, p, isUsingLeftHand);
            exercise_viewmodel.ExerciseFinished += HandleExerciseFinished;

            //Set the view-model class for the UI
            BindingContext = exercise_viewmodel;

            //Start a timer, which will determine when we update the UI
            update_timer = new Timer(30);
            update_timer.AutoReset = true;
            update_timer.Enabled = true;
            update_timer.Elapsed += Update_timer_Elapsed;
            update_timer.Start();
        }

        private void HandleScreenTouchEvent(object sender, EventArgs e)
        {
            lock (centering_lock)
            {
                if (!currently_centering)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        CenteringOverlay.IsVisible = true;
                        CenteringOverlay.ForceLayout();
                    });
                    
                    currently_centering = true;
                    last_centering_time = DateTime.Now;

                    exercise_viewmodel.TareSignal();
                }
            }
        }

        protected override void OnAppearing()
        {
            //empty
        }

        protected override bool OnBackButtonPressed()
        {
            StopExercise();
            return true;
        }

        private void StopExercisesButton_Clicked(object sender, EventArgs e)
        {
            StopExercise();
        }

        private async void StopExercise ()
        {
            if (exercise_viewmodel != null)
            {
                await exercise_viewmodel.StopExercise();
            }

            StopExercisingEvent?.Invoke(this, new EventArgs());
            PopThisPage();
        }

        private void HandleExerciseFinished(object sender, EventArgs e)
        {
            DeviceMissingEvent?.Invoke(this, new EventArgs());

            var top_of_stack = Navigation.ModalStack.LastOrDefault();
            if (this == top_of_stack)
            {
                PopThisPage();
            }
        }

        private void Update_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            exercise_viewmodel.Update();

            lock (centering_lock)
            {
                if (currently_centering && DateTime.Now >= (last_centering_time + centering_duration))
                {
                    currently_centering = false;

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        CenteringOverlay.IsVisible = false;
                        CenteringOverlay.ForceLayout();
                    });
                }
            }
        }

        private async void PopThisPage ()
        {
            update_timer.Stop();
            await Navigation.PopModalAsync(false);
        }

        public RepetitionsModel GetAssessmentModel ()
        {
            return exercise_viewmodel.AssessmentSessionModel;
        }

        private void RecheckPCMConnectionImageButton_Pressed(object sender, EventArgs e)
        {
            var btn = sender as ImageButton;
            if (btn != null)
            {
                btn.ScaleTo(0.67, 50, Easing.Linear);
            }
        }

        private void RecheckPCMConnectionImageButton_Released(object sender, EventArgs e)
        {
            var btn = sender as ImageButton;
            if (btn != null)
            {
                btn.ScaleTo(1.0, 50, Easing.Linear);
            }
        }

        private void RecheckPCMConnectionImageButton_Clicked(object sender, EventArgs e)
        {
            restore_connection_manager.CheckPCMStatus();
        }
    }
}