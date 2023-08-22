using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using RePlay_Common;
using Plugin.CurrentActivity;
using System.Collections.Generic;
using System.Linq;
using ReCheck.Model;
using System;
using RePlay_Exercises;
using OxyPlot.Annotations;
using RePlay_DeviceCommunications;
using ReCheck.Droid.Model;
using System.Threading.Tasks;
using RePlay_VNS_Triggering;
using RePlay_Exercises.RePlay;

namespace ReCheck.ViewModel
{
    public class ExercisePageViewModel : NotifyPropertyChangedObject
    {
        #region Raised events

        public EventHandler ExerciseFinished;

        #endregion

        #region Private data members

        private Participant participant;
        private ReCheckConfigurationModel recheck_configuration;
        private PCM_Manager restore_connection_manager;
        private RepetitionsModel repetitions_model;
        private ReplayMicrocontroller microcontroller_model;
        private bool is_left_hand = false;
        private ExerciseType exercise_type = ExerciseType.RePlay_Isometric_Pinch;

        private object update_lock = new object();
        private List<double> data = new List<double>();
        private int max_data_count = 100;

        public PlotModel Model { get; set; }

        #endregion

        #region Constructor

        public ExercisePageViewModel(PCM_Manager pcm, ReCheckConfigurationModel config, ReplayMicrocontroller replayMicrocontroller, Participant p, bool isLeftHand)
        {
            restore_connection_manager = pcm;
            restore_connection_manager.PropertyChanged += HandleRestoreCommunication;

            recheck_configuration = config;
            participant = p;

            microcontroller_model = replayMicrocontroller;
            is_left_hand = isLeftHand;

            InitializeRepetitionsModel(pcm, config, replayMicrocontroller, p);
            InitializePlotModel();
        }

        private void InitializeRepetitionsModel (PCM_Manager pcm, ReCheckConfigurationModel config, ReplayMicrocontroller replayMicrocontroller, Participant p)
        {
            //Initialize the exercise
            string tablet_id = config.TabletIdentifier;
            string subject_id = (p != null) ? p.ParticipantID : string.Empty;
            double gain = 1.0;
            int rep_count = 0;
            ExerciseType exerciseType = ExerciseTypeConverter.ConvertReplayDeviceTypeToExerciseType(replayMicrocontroller.CurrentDeviceType);
            exercise_type = exerciseType;
            var replayExercise = RePlayExerciseBase.InstantiateCorrectReplayExerciseClass(replayMicrocontroller, exerciseType, CrossCurrentActivity.Current.Activity, 1.0);

            //Instantiate the model
            repetitions_model = new RepetitionsModel(CrossCurrentActivity.Current.Activity, config, pcm);

            //Subscribe to property changes on the model object
            repetitions_model.PropertyChanged += Model_PropertyChanged;
            repetitions_model.ErrorEncountered += Handle_ErrorEncountered;

            //Initialize default VNS algorithm parameters
            VNSAlgorithmParameters vns_algorithm_parameters = new VNSAlgorithmParameters();
            if (config.AutomaticStimulationEnabled)
            {
                vns_algorithm_parameters.Enabled = true;
            }

            //Start the exercise
            repetitions_model.StartExercise(exerciseType, 
                replayExercise, 
                rep_count, 
                ThresholdType.StaticThreshold, 
                tablet_id, 
                subject_id, 
                gain,
                is_left_hand,
                vns_algorithm_parameters);
        }

        private void InitializePlotModel ()
        {
            LinearAxis xaxis = new LinearAxis();
            xaxis.Position = AxisPosition.Bottom;
            xaxis.Minimum = 0;
            xaxis.Maximum = max_data_count;
            xaxis.IsPanEnabled = false;
            xaxis.IsZoomEnabled = false;
            xaxis.Selectable = false;
            xaxis.IsAxisVisible = false;

            LinearAxis yaxis = new LinearAxis();
            yaxis.Position = AxisPosition.Left;
            yaxis.MinimumRange = 10;
            yaxis.Minimum = -(repetitions_model.Threshold * 3);
            yaxis.Maximum = (repetitions_model.Threshold * 3);
            yaxis.IsPanEnabled = false;
            yaxis.IsZoomEnabled = false;
            yaxis.Selectable = false;

            AreaSeries areaSeries = new AreaSeries()
            {
                Color = OxyColors.CornflowerBlue,
                StrokeThickness = 2
            };

            data = Enumerable.Repeat<double>(0, max_data_count).ToList();
            var data_points = data.Select((y, x) => new DataPoint(x, y)).ToList();
            var data_points_2 = data.Select((y, x) => new DataPoint(x, 0)).ToList();

            areaSeries.Points.AddRange(data_points);

            Model = new PlotModel();
            Model.Background = OxyColors.Transparent;
            Model.PlotAreaBorderThickness = new OxyThickness(0);
            Model.PlotAreaBorderColor = OxyColors.Transparent;

            Model.Axes.Add(xaxis);
            Model.Axes.Add(yaxis);
            Model.Series.Add(areaSeries);

            LineAnnotation initiation_threshold_annotation = new LineAnnotation()
            {
                StrokeThickness = 1,
                Color = OxyColors.Red,
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Horizontal,
                Y = repetitions_model.ReturnThreshold,
                Tag = "initiation_threshold",
            };

            LineAnnotation hit_threshold_annotation = new LineAnnotation()
            {
                StrokeThickness = 1,
                Color = OxyColors.Green,
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Horizontal,
                Y = repetitions_model.Threshold,
                Tag = "hit_threshold",
                Text = "Reach this point!",
                TextColor = OxyColors.Black,
                TextVerticalAlignment = VerticalAlignment.Bottom,
                FontSize = 28,
            };

            Model.Annotations.Add(initiation_threshold_annotation);
            Model.Annotations.Add(hit_threshold_annotation);
        }

        #endregion

        #region Update method

        public void Update ()
        {
            lock (update_lock)
            {
                if (Model.Series.Count > 0)
                {
                    lock(Model.SyncRoot)
                    {
                        var areaSeries = Model.Series[0] as AreaSeries;
                        if (areaSeries != null)
                        {
                            data.Add(repetitions_model.CurrentExerciseValue);
                            data.LimitTo(max_data_count, true);
                            
                            var data_points = data.Select((y, x) => new DataPoint(x, y)).ToList();
                            areaSeries.Points.Clear();
                            areaSeries.Points.AddRange(data_points);
                        }
                    }

                    Model.InvalidatePlot(true);
                }
            }
        }

        #endregion

        #region Public methods

        public async Task StopExercise ()
        {
            await repetitions_model.StopExercise_Async();
        }

        public void TareSignal ()
        {
            repetitions_model.PerformQuickRebaseline();
        }

        #endregion

        #region Event handlers that respond to events from the model

        private void HandleRestoreCommunication(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("PCM_Connection_Image");
            NotifyPropertyChanged("IPG_ID");
            NotifyPropertyChanged("PCM_ID");
        }

        private async void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("TrialReset"))
            {
                lock (Model.SyncRoot)
                {
                    //Reset the bounds of the vertical axis
                    var vertical_axis = Model.Axes.Where(x => x.Position == AxisPosition.Left).FirstOrDefault();
                    if (vertical_axis != null)
                    {
                        (double mean, double err) = repetitions_model.CalculateTrials_MeanAndErr();
                        if (!double.IsNaN(mean) && !double.IsNaN(err))
                        {
                            var pos_limit = mean + 3 * err;
                            vertical_axis.Minimum = -pos_limit;
                            vertical_axis.Maximum = pos_limit;
                        }
                    }

                    TrialMotionDirection trialMotionDirection = (repetitions_model.IsMotionDirectionPositive) ? TrialMotionDirection.Positive : TrialMotionDirection.Negative;

                    foreach (LineAnnotation lineAnnotation in Model.Annotations)
                    {
                        if (lineAnnotation != null)
                        {
                            string tag = lineAnnotation.Tag as string;
                            if (!string.IsNullOrEmpty(tag))
                            {
                                if (tag.Equals("hit_threshold"))
                                {
                                    var new_y_val = repetitions_model.CalculateGoal(trialMotionDirection);
                                    if (!double.IsNaN(new_y_val))
                                    {
                                        lineAnnotation.Y = new_y_val;
                                    }
                                    else
                                    {
                                        lineAnnotation.Y = (repetitions_model.IsMotionDirectionPositive) ? Math.Abs(lineAnnotation.Y) : -Math.Abs(lineAnnotation.Y);
                                    }
                                    
                                    lineAnnotation.TextVerticalAlignment = (repetitions_model.IsMotionDirectionPositive) ? VerticalAlignment.Bottom : VerticalAlignment.Top;
                                }
                                else if (tag.Equals("initiation_threshold"))
                                {
                                    lineAnnotation.Y = (repetitions_model.IsMotionDirectionPositive) ? repetitions_model.ReturnThreshold : -repetitions_model.ReturnThreshold;
                                }
                            }
                        }
                    }
                }

                NotifyPropertyChanged("RepetitionsCount");
                NotifyPropertyChanged("StimulationCount");
            }
            else if (e.PropertyName.Equals("DeviceMissing"))
            {
                await StopExercise();
                ExerciseFinished?.Invoke(this, new EventArgs());
            }
        }

        private void Handle_ErrorEncountered(object sender, EventArgs e)
        {
            //empty
        }

        #endregion

        #region Public properties

        public string AssessmentOrExerciseText
        {
            get
            {
                string result = (is_left_hand) ? "LEFT ARM " : "RIGHT ARM ";
                result += ExerciseTypeConverter.ConvertExerciseTypeToDescription(exercise_type).ToUpper();

                return result;
            }
        }

        public string StopAssessmentText
        {
            get
            {
                if (participant != null)
                {
                    return "STOP ASSESSMENT";
                }
                else
                {
                    return "STOP EXERCISE";
                }
            }
        }

        public string PCM_Connection_Image
        {
            get
            {
                if (restore_connection_manager.IsConnectedToPCM)
                {
                    return "pcm_connected.png";
                }
                else
                {
                    return "pcm_disconnected.png";
                }
            }
        }

        public string IPG_ID
        {
            get
            {
                if (string.IsNullOrEmpty(restore_connection_manager.Current_IPG_Identifier))
                {
                    return "Unknown";
                }
                else
                {
                    return restore_connection_manager.Current_IPG_Identifier;
                }
            }
        }

        public string PCM_ID
        {
            get
            {
                if (string.IsNullOrEmpty(restore_connection_manager.Current_PCM_Identifier))
                {
                    return "Unknown";
                }
                else
                {
                    return restore_connection_manager.Current_PCM_Identifier;
                }
            }
        }

        public bool IsRecheckPCMConnectionVisible
        {
            get
            {
                return recheck_configuration.AutomaticStimulationEnabled;
            }
        }

        public RepetitionsModel AssessmentSessionModel
        {
            get
            {
                return repetitions_model;
            }
        }

        public string StimulationCount
        {
            get
            {
                if (repetitions_model != null)
                {
                    return repetitions_model.StimulationsDelivered.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string RepetitionsCount
        {
            get
            {
                if (repetitions_model != null)
                {
                    return repetitions_model.RepetitionsCompleted.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string ModuleImage
        {
            get
            {
                if (is_left_hand)
                {
                    switch (microcontroller_model.CurrentDeviceType)
                    {
                        case ReplayDeviceType.Handle:
                            return "left_handle_rom.png";
                        case ReplayDeviceType.Knob:
                            return "left_knob_rom.png";
                        case ReplayDeviceType.Wrist:
                            return "left_wrist_rom.png";
                        case ReplayDeviceType.Handle_Isometric:
                            return "left_handle_rom.png";
                        case ReplayDeviceType.Knob_Isometric:
                            return "left_knob_rom.png";
                        case ReplayDeviceType.Wrist_Isometric:
                            return "left_wrist_iso.png";
                        case ReplayDeviceType.Pinch_Left:
                            return "left_pinch_iso.png";
                        default:
                            return "left_handle_rom.png";
                    }
                }
                else
                {
                    switch (microcontroller_model.CurrentDeviceType)
                    {
                        case ReplayDeviceType.Handle:
                            return "right_handle_rom.png";
                        case ReplayDeviceType.Knob:
                            return "right_knob_rom.png";
                        case ReplayDeviceType.Wrist:
                            return "right_wrist_rom.png";
                        case ReplayDeviceType.Handle_Isometric:
                            return "right_handle_rom.png";
                        case ReplayDeviceType.Knob_Isometric:
                            return "right_knob_rom.png";
                        case ReplayDeviceType.Wrist_Isometric:
                            return "right_wrist_iso.png";
                        case ReplayDeviceType.Pinch:
                            return "right_pinch_iso.png";
                        default:
                            return "right_handle_rom.png";
                    }
                }
            }
        }

        #endregion
    }
}

