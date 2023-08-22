using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Microcharts;
using Microcharts.Droid;
using Refractored.Controls;
using RePlay_Common;
using RePlay_Exercises;
using RePlay_Exercises.RePlay;
using RePlay_DeviceCommunications;
using Orientation = Android.Widget.Orientation;
using Newtonsoft.Json;
using RePlay_Exercises.FitMi;

namespace RePlay_Activity_RepetitionsMode
{
    [Activity(Label = "ExerciseRunning", LaunchMode = Android.Content.PM.LaunchMode.SingleInstance)]
    public class ExerciseRunning : Activity
    {
        #region Private Properties

        private FrameLayout MainLayout;
        private LinearLayout ErrorMenuLayout;
        private LinearLayout RebaselineLayout;
        private TextView DebuggingPropertiesTextView;
        private ChartView SessionProgressChartView;
        private ImageView StimulationRequestIcon;

        private int pcm_connection_icon_tristate = -1;
        private bool is_rebaselining_view_visible = false;
        
        private byte[] txbdc_green_color_rgb = new byte[3] { 0x69, 0xBE, 0x28 };
        private byte[] fitmi_blue_color_rgb = new byte[3] { 0x69, 0xEE, 0xFF };
        private byte[] fitmi_yellow_color_rgb = new byte[3] { 0xFF, 0xFF, 0x5D };
        private SkiaSharp.SKColor txbdc_green_color = SkiaSharp.SKColor.Empty;
        private SkiaSharp.SKColor fitmi_blue_color = SkiaSharp.SKColor.Empty;
        private SkiaSharp.SKColor fitmi_yellow_color = SkiaSharp.SKColor.Empty;

        private RadialGaugeChart progress_chart;
        private TxBDC_BarChart current_trial_chart;

        private double bar_chart_y_axis_max_val = 1.0;

        private bool DebugMode { get; set; } = false;
        private bool ShowChart { get; set; } = false;
        private bool Continuous { get; set; } = false;
        private double MaxRMSValue { get; set; } = 1.0;
        private ExerciseType Exercise { get; set; }
        private string InstructionText { get; set; }

        private RepetitionsModel model = null;


        private bool IsStimulationEnabled = false;
        private bool IsStimulationIconEnabled = false;
        private bool IsStimulationIconVisible = false;
        private DateTime StimulationIconStartTime = DateTime.MinValue;
        private TimeSpan StimulationIconDuration = TimeSpan.FromSeconds(2.0);

        #endregion

        #region OnCreate

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            string game_launch_params_json = Intent.GetStringExtra("game_launch_parameters_json");
            GameLaunchParameters game_launch_parameters = new GameLaunchParameters();
            try
            {
                game_launch_parameters = JsonConvert.DeserializeObject<GameLaunchParameters>(game_launch_params_json);
            }
            catch (Exception ex)
            {
                //empty
            }


            //Read in the parameters that are passed in from the wrapper app, or use default parameters...
            Continuous = game_launch_parameters.Continuous;
            int rep_count = Convert.ToInt32(game_launch_parameters.Duration);
            Exercise = game_launch_parameters.Exercise;
            string tablet_id = game_launch_parameters.TabletID;
            string subject_id = game_launch_parameters.SubjectID;
            bool from_prescription = game_launch_parameters.LaunchedFromPrescription;
            DebugMode = game_launch_parameters.DebugMode;
            double gain = game_launch_parameters.Gain;
            IsStimulationIconEnabled = game_launch_parameters.ShowStimulationRequests;
            IsStimulationEnabled = game_launch_parameters.VNS_AlgorithmParameters.Enabled;

            //Grab the reps mode layout and add it as a child view to our main layout frame
            var res_id = Resources.GetIdentifier("reps_mode", "layout", Application.Context.PackageName);
            View repsModeLayout = LayoutInflater.Inflate(res_id, null);
            MainLayout = new FrameLayout(this.ApplicationContext);
            MainLayout.AddView(repsModeLayout);

            // Set our view from the "main" layout resource
            SetContentView(MainLayout);
            
            StartImmersiveMode();

            //Set some colors
            fitmi_blue_color = new SkiaSharp.SKColor(fitmi_blue_color_rgb[0], fitmi_blue_color_rgb[1], fitmi_blue_color_rgb[2]);
            fitmi_yellow_color = new SkiaSharp.SKColor(fitmi_yellow_color_rgb[0], fitmi_yellow_color_rgb[1], fitmi_yellow_color_rgb[2]);
            txbdc_green_color = new SkiaSharp.SKColor(txbdc_green_color_rgb[0], txbdc_green_color_rgb[1], txbdc_green_color_rgb[2]);
            
            DebuggingPropertiesTextView = FindViewById<TextView>(Resources.GetIdentifier("repsmode_debugging_properties_textview", "id", Application.Context.PackageName));
            SessionProgressChartView = FindViewById<ChartView>(Resources.GetIdentifier("progressChartView", "id", Application.Context.PackageName));
            if (DebugMode)
            {
                SessionProgressChartView.Visibility = ViewStates.Gone;
                DebuggingPropertiesTextView.Visibility = ViewStates.Visible;
            }

            Button StopButton = FindViewById<Button>(Resources.GetIdentifier("stop_exercise", "id", Application.Context.PackageName));
            StopButton.Click += StopButton_Click;

            //Get the singleton instance of the model
            model = new RepetitionsModel(this);

            //Subscribe to property changes on the model object
            model.PropertyChanged += Model_PropertyChanged;
            model.ErrorEncountered += Handle_ErrorEncountered;

            //Initialize the exercise
            model.StartExercise(Exercise, rep_count, 
                RePlay_Activity_RepetitionsMode.ThresholdType.StaticThreshold, tablet_id, subject_id, gain,
                from_prescription, game_launch_parameters.VNS_AlgorithmParameters);

            //Initialize the user interface
            InitializeUserInterfaceElements();
        }

        // Restart immersive mode
        protected override void OnResume()
        {
            base.OnResume();

            StartImmersiveMode();
        }

        #endregion

        #region ModelChanged

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (model != null)
            {
                if (e.PropertyName.Equals("DeviceMissing"))
                {
                    //End repetitions mode if the device module has been removed
                    EndRepsMode();
                    return;
                }

                if (e.PropertyName.Equals("stim"))
                {
                    if (IsStimulationIconEnabled)
                    {
                        IsStimulationIconVisible = true;
                        StimulationIconStartTime = DateTime.Now;
                        DisplayStimulationIcon();
                    }
                }

                if (IsStimulationIconVisible)
                {
                    if (DateTime.Now >= (StimulationIconStartTime + StimulationIconDuration))
                    {
                        IsStimulationIconVisible = false;
                        HideStimulationIcon();
                    }
                }

                //Update the PCM connection status icon
                int pcm_status = model.PCM.IsConnectedToPCM ? 1 : 0;
                if (pcm_status != pcm_connection_icon_tristate)
                {
                    pcm_connection_icon_tristate = pcm_status;

                    var pcm_connection_status_image = FindViewById<ImageView>(Resources.GetIdentifier("reps_mode_pcm_connection_status_icon", "id", Application.Context.PackageName));
                    if (pcm_connection_status_image != null)
                    {
                        string img_name = string.Empty;
                        if (model.PCM.IsConnectedToPCM)
                        {
                            if (IsStimulationEnabled)
                            {
                                img_name = "repsmode_pcm_connected";
                            }
                            else
                            {
                                img_name = "repsmode_pcm_connected_nostim";
                            }
                        }
                        else
                        {
                            img_name = "repsmode_pcm_disconnected";
                        }

                        int res_image = Resources.GetIdentifier(img_name, "drawable", Application.Context.PackageName);
                        pcm_connection_status_image.SetImageResource(res_image);
                    }
                }
                
                if (model.BackgroundThreadExitedInError || model.DeviceSetupError)
                {
                    //Stop listening for changes to the model
                    model.PropertyChanged -= Model_PropertyChanged;

                    //Display a message to the user
                    var exercise_description = FindViewById<TextView>(Resources.GetIdentifier("exercise_description", "id", Application.Context.PackageName));
                    exercise_description.Text = "An error has occurred. Please restart the exercise.";

                    //Return from this method
                    return;
                }
                else if (model.ExerciseRunning)
                {
                    // Determine whether to end reps mode
                    if (model.RepetitionsCompleted >= model.RequiredRepetitionsCount && !Continuous)
                    {
                        //Stop listening for changes to the model
                        model.PropertyChanged -= Model_PropertyChanged;

                        //End the RepetitionsMode
                        EndRepsMode();

                        //Return from this function
                        return;
                    }
                    else
                    {
                        var current_debounced_value = model.CurrentExerciseValue;

                        //Update the text view objects
                        var current_trial_text_view = FindViewById<TextView>(Resources.GetIdentifier("current_trial_text_view", "id", Application.Context.PackageName));
                        var max_trials_text_view = FindViewById<TextView>(Resources.GetIdentifier("max_trials_text_view", "id", Application.Context.PackageName));

                        if (current_trial_text_view != null && max_trials_text_view != null)
                        {
                            current_trial_text_view.SetText(model.RepetitionsCompleted.ToString(), TextView.BufferType.Normal);
                            max_trials_text_view.SetText(model.RequiredRepetitionsCount.ToString(), TextView.BufferType.Normal);
                        }

                        //Update the current trial chart
                        UpdateBarChart(model, current_debounced_value);

                        //Update the session progress chart
                        UpdateProgressChart(model);

                        //Update the debugging text view
                        UpdateDebuggingProperties(model);
                    }
                }
                else if (model.IsFirstTimeToResetBaseline)
                {
                    UpdateCountdown(model);
                }
            }
        }

        #endregion

        #region Handler for device communication errors

        private void Handle_ErrorEncountered(object sender, EventArgs e)
        {
            if (MainLayout != null)
            {
                int txbdc_button_foreground_rid = Resources.GetIdentifier("txbdc_button_foreground_selector", "drawable", PackageName);
                int txbdc_button_background_rid = Resources.GetIdentifier("txbdc_button_background_selector", "drawable", PackageName);

                //Create a text view to indicate to the user that there is an error
                TextView errorText = new TextView(ApplicationContext);
                errorText.Text = "We are sorry, but we have lost communication with the game controller device. If you unplugged it, please plug it back in. Press continue below to reconnect to the device and continue your exercise.";
                errorText.TextSize = 24;
                errorText.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
                errorText.SetTextColor(Android.Graphics.Color.White);
                errorText.SetForegroundGravity(GravityFlags.Center);
                errorText.Gravity = GravityFlags.Center;
                errorText.TextAlignment = TextAlignment.Center;

                //Create 2 buttons: one to attempt to continue the game, and the other to quit the game
                Button continueButton = new Button(ApplicationContext);
                continueButton.Text = "Continue exercise";
                continueButton.TextAlignment = TextAlignment.Center;
                LinearLayout.LayoutParams continueButtonLayoutParams = new LinearLayout.LayoutParams(700, 300);
                continueButtonLayoutParams.SetMargins(100, 100, 100, 100);
                continueButton.LayoutParameters = continueButtonLayoutParams;
                continueButton.Enabled = true;

                continueButton.Click += (s, evArgs) =>
                {
                    RepetitionsModel senderModel = sender as RepetitionsModel;
                    if (senderModel != null)
                    {
                        bool connectionSuccess = senderModel.ReconnectToDevice();
                        if (connectionSuccess)
                        {
                            senderModel.ContinueExercise();
                            if (ErrorMenuLayout != null)
                            {
                                MainLayout.RemoveView(ErrorMenuLayout);
                            }
                        }
                    }
                };

                if (txbdc_button_foreground_rid != 0 && txbdc_button_background_rid != 0)
                {
                    continueButton.SetBackgroundResource(txbdc_button_foreground_rid);
                    continueButton.SetTextAppearance(txbdc_button_background_rid);
                }

                Button quitButton = new Button(ApplicationContext);
                quitButton.Text = "Exit this exercise";
                quitButton.TextAlignment = TextAlignment.Center;
                LinearLayout.LayoutParams quitButtonLayoutParams = new LinearLayout.LayoutParams(700, 300);
                quitButtonLayoutParams.SetMargins(100, 100, 100, 100);
                quitButton.LayoutParameters = quitButtonLayoutParams;
                quitButton.Enabled = true;

                quitButton.Click += (s, evArgs) =>
                {
                    RepetitionsModel senderModel = sender as RepetitionsModel;
                    if (senderModel != null)
                    {
                        EndRepsMode();
                    }
                };

                if (txbdc_button_foreground_rid != 0 && txbdc_button_background_rid != 0)
                {
                    quitButton.SetBackgroundResource(txbdc_button_foreground_rid);
                    quitButton.SetTextAppearance(txbdc_button_background_rid);
                }

                //Create a horizontal linear layout to hold the buttons
                LinearLayout buttonLayout = new LinearLayout(ApplicationContext);
                buttonLayout.LayoutParameters = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
                buttonLayout.Orientation = Orientation.Horizontal;
                buttonLayout.AddView(continueButton);
                buttonLayout.AddView(quitButton);

                //Create a linear layout and place the text view inside of it
                ErrorMenuLayout = new LinearLayout(ApplicationContext);
                ErrorMenuLayout.SetBackgroundColor(Android.Graphics.Color.Argb(200, 40, 40, 40));
                ErrorMenuLayout.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
                ErrorMenuLayout.SetPadding(100, 100, 100, 100);
                ErrorMenuLayout.SetGravity(GravityFlags.Center);
                ErrorMenuLayout.SetForegroundGravity(GravityFlags.Center);
                ErrorMenuLayout.Orientation = Orientation.Vertical;

                ErrorMenuLayout.AddView(errorText);
                ErrorMenuLayout.AddView(buttonLayout);

                //Place the linear layout in our main frame layout
                MainLayout.SetForegroundGravity(GravityFlags.Center);
                MainLayout.AddView(ErrorMenuLayout);
            }
        }

        #endregion

        #region Button Handlers

        public override void OnBackPressed()
        {
            EndRepsMode();
        }

        public void StopButton_Click(object sender, EventArgs e)
        {
            EndRepsMode();
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (e.Action == MotionEventActions.Down)
            {
                model.PerformQuickRebaseline();
                ShowRebaselineView();
            }
            
            return base.OnTouchEvent(e);
        }

        private async void ShowRebaselineView ()
        {
            if (!is_rebaselining_view_visible)
            {
                is_rebaselining_view_visible = true;
                
                await Task.Run(async () =>
                {
                    this.RunOnUiThread(() =>
                    {
                        MainLayout.AddView(RebaselineLayout);
                    });
                    await Task.Delay(500);
                    this.RunOnUiThread(() =>
                    {
                        MainLayout.RemoveView(RebaselineLayout);
                    });
                });

                is_rebaselining_view_visible = false;
            }
        }

        #endregion

        #region Private Methods

        private async void EndRepsMode()
        {
            if (model != null)
            {
                await model.StopExercise_Async();
            }
            
            SetResult(Android.App.Result.Ok);
            Finish();
            return;
        }

        // Immersive mode for games
        private void StartImmersiveMode()
        {
            View decorView = Window.DecorView;
            Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            var uiOptions = (int)decorView.SystemUiVisibility;
            var newUiOptions = (int)uiOptions;

            newUiOptions |= (int)SystemUiFlags.Fullscreen;
            newUiOptions |= (int)SystemUiFlags.HideNavigation;
            newUiOptions |= (int)SystemUiFlags.Immersive;
            newUiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
        }

        private void InitializeUserInterfaceElements()
        {
            /* FIRST CREATE THE REBASELINE LAYOUT */

            //Create a linear layout and place the text view inside of it
            RebaselineLayout = new LinearLayout(ApplicationContext);
            RebaselineLayout.SetBackgroundColor(Android.Graphics.Color.Argb(200, 40, 40, 40));
            RebaselineLayout.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
            RebaselineLayout.SetPadding(100, 100, 100, 100);
            RebaselineLayout.SetGravity(GravityFlags.Center);
            RebaselineLayout.SetForegroundGravity(GravityFlags.Center);
            RebaselineLayout.Orientation = Orientation.Vertical;

            //Create a text view to indicate to the user that there is an error
            TextView rebaselineText = new TextView(ApplicationContext);
            rebaselineText.Text = "CENTERING";
            rebaselineText.TextSize = 72;
            rebaselineText.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
            rebaselineText.SetTextColor(Android.Graphics.Color.White);
            rebaselineText.SetForegroundGravity(GravityFlags.Center);
            rebaselineText.Gravity = GravityFlags.Center;
            rebaselineText.TextAlignment = TextAlignment.Center;

            RebaselineLayout.AddView(rebaselineText);

            /* END OF CREATING THE REBASELINE LAYOUT */

            if (model != null)
            {
                //Initialize the current trial chart
                var current_trial_entries = new[]
                {
                    new Entry(0.0f)
                    {
                        Color = txbdc_green_color
                    }
                };

                //Set the initial y-axis limits for the plot
                if (model.Exercise != null)
                {
                    if (model.Exercise is FitMiExerciseBase_FlipStyle)
                    {
                        bar_chart_y_axis_max_val = model.Exercise.ReturnThreshold * 3.0;
                    }
                    else
                    {
                        bar_chart_y_axis_max_val = Math.Max(bar_chart_y_axis_max_val,
                            model.Exercise.ReturnThreshold * 10.0);
                    }
                }

                current_trial_chart = new TxBDC_BarChart(false)
                {
                    Entries = current_trial_entries,
                    MaxValue = Convert.ToSingle(bar_chart_y_axis_max_val),
                    MinValue = (model.Exercise.SinglePolarity) ? -0.01f : -Convert.ToSingle(bar_chart_y_axis_max_val),
                    UseSpecialBackgroundColor = true,
                    SpecialBackgroundColor = txbdc_green_color
                };

                current_trial_chart.HitThreshold = new TxBDC_BarChart_HorizontalLineAnnotation()
                {
                    Y_Value = (float)model.Threshold,
                    LineColor = SkiaSharp.SKColors.Red
                };

                current_trial_chart.ReturnThreshold = new TxBDC_BarChart_HorizontalLineAnnotation()
                {
                    Y_Value = (float)model.ReturnThreshold,
                    LineColor = SkiaSharp.SKColors.Black
                };

                current_trial_chart.NegHitThreshold = new TxBDC_BarChart_HorizontalLineAnnotation()
                {
                    Y_Value = (float)-model.Threshold,
                    LineColor = SkiaSharp.SKColors.Red
                };

                current_trial_chart.NegReturnThreshold = new TxBDC_BarChart_HorizontalLineAnnotation()
                {
                    Y_Value = (float)-model.ReturnThreshold,
                    LineColor = SkiaSharp.SKColors.Black
                };

                var rid = Resources.GetIdentifier("currentTrialChartView", "id", Application.Context.PackageName);
                if (rid == 0)
                {
                    rid = Resources.GetIdentifier("currenttrialchartview", "id", Application.Context.PackageName);
                }

                var current_trial_chart_view = FindViewById<ChartView>(rid);
                if (current_trial_chart_view != null)
                {
                    current_trial_chart_view.Chart = current_trial_chart;
                }

                //Initialize the session progress chart
                var progress_chart_entries = new[]
                {
                    new Entry(0)
                    {
                        Color = txbdc_green_color
                    }
                };

                progress_chart = new RadialGaugeChart()
                {
                    Entries = progress_chart_entries,
                    MaxValue = model.RequiredRepetitionsCount,
                    MinValue = 0,
                    Margin = 0
                };

                rid = Resources.GetIdentifier("progressChartView", "id", Application.Context.PackageName);
                if (rid == 0)
                {
                    rid = Resources.GetIdentifier("progresschartview", "id", Application.Context.PackageName);
                }

                var progress_chart_view = FindViewById<ChartView>(rid);
                if (progress_chart_view != null)
                {
                    progress_chart_view.Chart = progress_chart;
                }

                // Initialize TextViews
                var exercise_text_view = FindViewById<TextView>(Resources.GetIdentifier("exercise_name", "id", Application.Context.PackageName));
                exercise_text_view.Text = RePlay_Exercises.ExerciseTypeConverter.ConvertExerciseTypeToDescription(Exercise);

                var current_trial_text_view = FindViewById<TextView>(Resources.GetIdentifier("current_trial_text_view", "id", Application.Context.PackageName));
                var intermediate_text_view = FindViewById<TextView>(Resources.GetIdentifier("intermediate_text_view", "id", Application.Context.PackageName));
                var max_trials_text_view = FindViewById<TextView>(Resources.GetIdentifier("max_trials_text_view", "id", Application.Context.PackageName));

                if (current_trial_text_view != null && intermediate_text_view != null && max_trials_text_view != null)
                {
                    current_trial_text_view.SetText("0", TextView.BufferType.Normal);
                    intermediate_text_view.SetText("/", TextView.BufferType.Normal);
                    max_trials_text_view.SetText(model.RequiredRepetitionsCount.ToString(), TextView.BufferType.Normal);
                }

                var exercise_description = FindViewById<TextView>(Resources.GetIdentifier("exercise_description", "id", Application.Context.PackageName));
                exercise_description.Text = "Get ready to begin";
                InstructionText = model.Exercise.Instruction;

                //Get the stimulation request icon
                StimulationRequestIcon = FindViewById<ImageView>(Resources.GetIdentifier("reps_mode_stimulation_request_icon", "id", Application.Context.PackageName));
            }
        }

        private void UpdateProgressChart(RepetitionsModel model)
        {
            var progress_entries = new[]
            {
                new Entry(model.RepetitionsCompleted)
                {
                    Color = txbdc_green_color
                }
            };

            progress_chart.Entries = progress_entries;

            var rid = Resources.GetIdentifier("progressChartView", "id", Application.Context.PackageName);
            if (rid == 0)
            {
                rid = Resources.GetIdentifier("progresschartview", "id", Application.Context.PackageName);
            }
            var progress_chart_view = FindViewById<ChartView>(rid);
            if (progress_chart_view != null)
            {
                progress_chart_view.Chart = progress_chart;
                progress_chart_view.Invalidate();
            }
        }

        private void UpdateBarChart(RepetitionsModel model, double current_value)
        {
            //Make sure the current value is actually a number
            if (double.IsInfinity(current_value) || double.IsNaN(current_value))
            {
                return;
            }

            //Check to see if the current value is greater than the max value for the bar chart
            //If so, adjust the max value for the bar chart
            double abs_current_value = Math.Abs(current_value);
            if (abs_current_value >= bar_chart_y_axis_max_val)
            {
                bar_chart_y_axis_max_val = abs_current_value * 1.5;
            }

            //Attempt to convert the current value to a 32-bit integer
            int integer_current_value = 0;
            try
            {
                integer_current_value = Convert.ToInt32(current_value);
            }
            catch (Exception e)
            {
                //empty
            }

            //Determine which color we will make the bar on the bar plot
            var current_color = txbdc_green_color;
            if (model.Exercise.DeviceClass == ExerciseDeviceType.FitMi)
            {
                //If the value is above the return threshold, it will be blue.
                //Otherwise, if the value is below the negative return threshold, it will be yellow.
                //Otherwise, if it is within the threshold boundaries, it will be the txbdc green color.
                current_color = (current_value > model.ReturnThreshold) ? fitmi_blue_color : 
                    (current_value < -model.ReturnThreshold) ? fitmi_yellow_color :
                    txbdc_green_color;
            }

            //TO DO: fix this. Does this really need to happen on every frame???
            var exercise_description = FindViewById<TextView>(Resources.GetIdentifier("exercise_description", "id", Application.Context.PackageName));
            exercise_description.Text = model.Exercise.Instruction;
            //END OF TO DO

            //Create an "entry" which will be plotted on the bar chart
            var current_trial_entries = new[]
            {
                new Entry((float)current_value)
                {
                    Color = current_color,
                    Label = Convert.ToInt32(current_value).ToString()
                }
            };

            current_trial_chart = new TxBDC_BarChart(false)
            {
                Entries = current_trial_entries,
                MaxValue = Convert.ToSingle(bar_chart_y_axis_max_val),
                MinValue = (model.Exercise.SinglePolarity) ? -0.01f : -Convert.ToSingle(bar_chart_y_axis_max_val),
                UseSpecialBackgroundColor = true,
                SpecialBackgroundColor = txbdc_green_color,
                LabelTextSize = 40
            };

            current_trial_chart.HitThreshold = new TxBDC_BarChart_HorizontalLineAnnotation()
            {
                Y_Value = (float)model.Threshold,
                LineColor = SkiaSharp.SKColors.Red
            };

            current_trial_chart.ReturnThreshold = new TxBDC_BarChart_HorizontalLineAnnotation()
            {
                Y_Value = (float)model.ReturnThreshold,
                LineColor = SkiaSharp.SKColors.Black
            };

            current_trial_chart.NegHitThreshold = new TxBDC_BarChart_HorizontalLineAnnotation()
            {
                Y_Value = (float)-model.Threshold,
                LineColor = SkiaSharp.SKColors.Red
            };

            current_trial_chart.NegReturnThreshold = new TxBDC_BarChart_HorizontalLineAnnotation()
            {
                Y_Value = (float)-model.ReturnThreshold,
                LineColor = SkiaSharp.SKColors.Black
            };
            
            current_trial_chart.LineAnnotations.Add(new TxBDC_BarChart_HorizontalLineAnnotation()
            {
                Y_Value = 0,
                LineColor = SkiaSharp.SKColors.CornflowerBlue
            });

            var rid = Resources.GetIdentifier("currentTrialChartView", "id", Application.Context.PackageName);
            if (rid == 0)
            {
                rid = Resources.GetIdentifier("currenttrialchartview", "id", Application.Context.PackageName);
            }
            var current_trial_chart_view = FindViewById<ChartView>(rid);
            if (current_trial_chart_view != null)
            {
                current_trial_chart_view.Chart = current_trial_chart;
                current_trial_chart_view.Invalidate();
            }
        }

        private void UpdateCountdown(RepetitionsModel model)
        {
            int time_remaining = model.GetCountdownTimeRemaining();

            if (time_remaining <= 0)
            {
                var exercise_description = FindViewById<TextView>(Resources.GetIdentifier("exercise_description", "id", Application.Context.PackageName));
                exercise_description.Text = InstructionText;
            }
            else
            {
                var exercise_description = FindViewById<TextView>(Resources.GetIdentifier("exercise_description", "id", Application.Context.PackageName));
                exercise_description.Text = "Starting in " + time_remaining.ToString();
            }
        }

        private void UpdateDebuggingProperties (RepetitionsModel model)
        {
            if (DebugMode)
            {
                if (model != null && model.Exercise != null)
                {
                    string result = string.Empty;
                    var debugging_properties = model.Exercise.GetDebuggingProperties();

                    foreach (var kvp in debugging_properties)
                    {
                        string label = kvp.Key;
                        double value = kvp.Value;

                        if (model.Exercise is RePlayExercise_Isometric && label.Equals("Device"))
                        {
                            try
                            {
                                ReplayDeviceType device_type = (ReplayDeviceType) Convert.ToInt32(value);
                                result += label + ": " + RePlay_DeviceCommunications.ReplayDeviceTypeConverter.ConvertDeviceTypeToDescription(device_type);
                            }
                            catch (Exception e)
                            {
                                //empty
                            }
                        }
                        else
                        {
                            result += label + ": " + value + "\n";
                        }
                    }

                    DebuggingPropertiesTextView.Text = result;
                }
            }
        }

        private void DisplayStimulationIcon ()
        {
            string img_name = string.Empty;
            if (IsStimulationEnabled)
            {
                img_name = "repsmode_stim_symbol";
            }
            else
            {
                img_name = "repsmode_stim_symbol_nostim";
            }

            int res_image = Resources.GetIdentifier(img_name, "drawable", Application.Context.PackageName);
            StimulationRequestIcon.SetImageResource(res_image);

            StimulationRequestIcon.Visibility = ViewStates.Visible;
        }

        private void HideStimulationIcon ()
        {
            StimulationRequestIcon.Visibility = ViewStates.Invisible;
        }

        #endregion

    }
}