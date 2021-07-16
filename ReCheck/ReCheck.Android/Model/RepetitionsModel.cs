using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Android.App;
using RePlay_Exercises;
using RePlay_Common;
using Android.Util;
using RePlay_VNS_Triggering;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AppCenter.Crashes;
using ReCheck.Droid.Model;
using Android.SE.Omapi;
using RePlay_Exercises.RePlay;

namespace ReCheck.Model
{
    /// <summary>
    /// Model class for the rep-it-out mode
    /// </summary>
    public class RepetitionsModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private ReCheckConfigurationModel configuration_settings = null;
        private BackgroundWorker background_thread = null;
        private Activity activity = null;

        private SessionState current_session_state = SessionState.NotStarted;
        private TrialState current_trial_state = TrialState.Ready;

        private double minimum_trial_duration_seconds;
        private List<TrialModel> all_trials = new List<TrialModel>();
        private bool stim_flag = false;
        private object stim_flag_lock = new object();
        private object data_lock = new object();

        private BinaryWriter gamedata_save_file_handle;

        private bool worker_completed = false;
        private object worker_completed_lock = new object();

        private List<double> debounce_list = new List<double>();
        private int debounce_size = 10;

        private bool is_first_time = true;
        private DateTime reset_baseline_time = DateTime.Now;
        private TimeSpan reset_baseline_duration = TimeSpan.FromMilliseconds(500);
        private TimeSpan long_reset_baseline_duration = TimeSpan.FromMilliseconds(5000);
        private TimeSpan short_reset_baseline_duration = TimeSpan.FromMilliseconds(100);
        private bool quick_rebaseline_flag = false;
        private object quick_rebaseline_flag_lock = new object();
        private bool is_positive_trial = true;
        private bool is_left_handed_session = false;

        private VNSAlgorithmParameters vns_algorithm_parameters = new VNSAlgorithmParameters();

        #endregion

        #region Constructor

        public RepetitionsModel(Activity a, ReCheckConfigurationModel config, PCM_Manager pcm)
        {
            //Initialize the FitMi and RePlay controller classes
            activity = a;
            configuration_settings = config;

            VNSManager = new VNSAlgorithm_Standard();
            PCM = pcm;
            PCM.PropertyChanged += (b, c) =>
            {
                if (PCM.CurrentStimulationTimeoutPeriod_SafeToUse != TimeSpan.Zero)
                {
                    //VNSManager.MinimumStimulationInterval = PCM.CurrentStimulationTimeoutPeriod_SafeToUse;
                }
            };

            //Create the background thread
            background_thread = new BackgroundWorker();
            background_thread.DoWork += Background_thread_DoWork;
            background_thread.ProgressChanged += Background_thread_ProgressChanged;
            background_thread.RunWorkerCompleted += Background_thread_RunWorkerCompleted;
            background_thread.WorkerSupportsCancellation = true;
            background_thread.WorkerReportsProgress = true;
        }

        #endregion

        #region Public properties

        public event EventHandler<EventArgs> ErrorEncountered;

        public bool ExerciseRunning
        {
            get
            {
                return (current_session_state == SessionState.SessionRunning);
            }
        }

        public bool DeviceSetupError
        {
            get
            {
                return (current_session_state == SessionState.SetupFailed);
            }
        }

        public bool BackgroundThreadRunning
        {
            get
            {
                return !worker_completed;
            }
        }

        public bool IsFirstTimeToResetBaseline
        {
            get
            {
                return is_first_time;
            }
        }

        public bool BackgroundThreadExitedInError { get; private set; } = false;

        public VNSAlgorithm_Standard VNSManager { get; private set; }

        public PCM_Manager PCM { get; private set; }

        public int ExerciseThresholdLookbackCount { get; private set; } = 3;

        public ThresholdType ExerciseThresholdType { get; private set; } = ThresholdType.Unknown;

        public ExerciseType ExerciseType { get; private set; } = ExerciseType.Unknown;

        public ExerciseBase Exercise { get; private set; } = null;

        public int RequiredRepetitionsCount { get; private set; } = 0;

        public int RepetitionsCompleted { get; private set; } = 0;

        public int StimulationsDelivered { get; private set; } = 0;

        public double CurrentExerciseValue { get; private set; } = 0;

        public double CurrentVNSAlgorithmValue { get; private set; } = 0;

        public double CurrentTrialMaxValueAchieved { get; private set; } = 0;

        public double Threshold { get; private set; } = 7;

        public double ReturnThreshold { get; private set; } = 5;

        public bool IsMotionDirectionPositive
        {
            get
            {
                return is_positive_trial;
            }
        }

        public bool StimulationFlag
        {
            get
            {
                lock (stim_flag_lock)
                {
                    return stim_flag;
                }
            }
            set
            {
                lock (stim_flag_lock)
                {
                    stim_flag = value;
                }
            }
        }

        public string SubjectID { get; set; }

        public string TabletID { get; set; }

        public RepetitionsModeDataVisualizationType DataVisualizationMode { get; set; } = RepetitionsModeDataVisualizationType.Velocity;

        public int TotalPositiveAttempts
        {
            get
            {
                var num_pos = all_trials.Where(x => x.MotionDirection == TrialMotionDirection.Positive).ToList().Count;
                return num_pos;
            }
        }

        public int TotalNegativeAttempts
        {
            get
            {
                var num_neg = all_trials.Where(x => x.MotionDirection == TrialMotionDirection.Negative).ToList().Count;
                return num_neg;
            }
        }

        public double MeanPositivePeak
        {
            get
            {
                var pos_trials = all_trials.Where(x => x.MotionDirection == TrialMotionDirection.Positive).ToList();
                double mean_peak = double.NaN;
                if (pos_trials.Count > 0)
                {
                    mean_peak = pos_trials.Select(x => x.TrialMaximum).Average();
                }

                return mean_peak;
            }
        }

        public double MeanNegativePeak
        {
            get
            {
                var neg_trials = all_trials.Where(x => x.MotionDirection == TrialMotionDirection.Negative).ToList();
                double mean_peak = double.NaN;
                if (neg_trials.Count > 0)
                {
                    mean_peak = neg_trials.Select(x => x.TrialMaximum).Average();
                }

                return mean_peak;
            }
        }

        #endregion

        #region Public methods

        public double CalculateGoal (TrialMotionDirection trialMotionDirection)
        {
            try
            {
                double result = double.NaN;
                var peaks = all_trials.Where(x => x.MotionDirection == trialMotionDirection).ToList();

                if (peaks.Count >= 3)
                {
                    var last_peaks = peaks.TakeLast(10).Select(x => x.TrialMaximum).ToList();
                    var mean_last_peaks = last_peaks.Average();

                    result = mean_last_peaks;
                    if (trialMotionDirection == TrialMotionDirection.Negative)
                    {
                        result = -result;
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                return double.NaN;
            }
        }

        public (double, double) CalculateTrials_MeanAndErr ()
        {
            if (all_trials.Count >= 2)
            {
                var peaks = all_trials.Select(x => x.TrialMaximum).ToList();
                var avg_peak = peaks.Average();
                var std_dev_peak = TxBDC_Math.StdDev(peaks);
                return (avg_peak, std_dev_peak);
            }
            else
            {
                return (double.NaN, double.NaN);
            }
        }

        public int GetCountdownTimeRemaining()
        {
            var elapsed_time = DateTime.Now - reset_baseline_time;
            var time_remaining = reset_baseline_duration - elapsed_time;
            return Convert.ToInt32(Math.Ceiling(time_remaining.TotalSeconds));
        }

        public void PerformQuickRebaseline()
        {
            lock (quick_rebaseline_flag_lock)
            {
                quick_rebaseline_flag = true;
            }
        }

        public void ContinueExercise()
        {
            if (current_session_state == SessionState.ErrorDetected)
            {
                current_session_state = SessionState.SessionRunning;
            }
        }

        public bool ReconnectToDevice()
        {
            bool success = Exercise.SetupDevice();
            return success;
        }

        /// <summary>
        /// Begins execution of a new exercise
        /// </summary>
        /// <param name="exercise"></param>
        public void StartExercise(ExerciseType exerciseType, RePlayExerciseBase exercise, int num_reps, 
            ThresholdType t, string tablet, string subject, double gain, bool is_left_hand,
            VNSAlgorithmParameters vnsAlgorithmParameters)
        {
            TabletID = tablet;

            //If the background thread isn't already executing...
            if (!background_thread.IsBusy)
            {
                Exercise = exercise;
                ExerciseType = exerciseType;

                //Set the number of repetitions the user should do
                RequiredRepetitionsCount = num_reps;

                //Set the threshold type
                ExerciseThresholdType = t;
                SubjectID = subject;
                minimum_trial_duration_seconds = Exercise.MinimumTrialDuration.TotalSeconds;
                all_trials = new List<TrialModel>();
                debounce_list = new List<double>();
                current_session_state = SessionState.BeginResetBaseline;
                current_trial_state = TrialState.Ready;
                is_left_handed_session = is_left_hand;
                vns_algorithm_parameters = vnsAlgorithmParameters;

                if (Exercise is RePlayExercise_Isometric)
                {
                    DataVisualizationMode = RepetitionsModeDataVisualizationType.Actual;
                }
                else
                {
                    DataVisualizationMode = RepetitionsModeDataVisualizationType.Velocity;
                }

                BeginExercise();
            }
        }

        public void BeginExercise()
        {
            //Start up the background thread
            if (!background_thread.IsBusy)
            {
                background_thread.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Stops execution of the exercise that is currently running
        /// </summary>
        public void StopExercise()
        {
            //If the background thread is running...
            if (background_thread.IsBusy)
            {
                //Cancel the background thread
                background_thread.CancelAsync();
            }

            //Close the gamedata file
            Exercise_SaveData.CloseFile(gamedata_save_file_handle);

            Exercise.CloseFile();
        }

        public async Task StopExercise_Async()
        {
            try
            {
                //If the background thread is running...
                if (background_thread.IsBusy)
                {
                    //Cancel the background thread
                    background_thread.CancelAsync();
                }

                await Task.Run(async () =>
                {
                    //Wait until the worker completes
                    while (!worker_completed)
                    {
                        await Task.Delay(100);
                    }
                });

                //Close the gamedata file
                Exercise_SaveData.CloseFile(gamedata_save_file_handle);

                Exercise.CloseFile();
            }
            catch (Exception e)
            {
                Crashes.TrackError(e, new Dictionary<string, string>() { { "ERROR", "Error while closing Repetitions Mode" } });
            }
        }

        #endregion

        #region Private methods

        private void CalculateNewHitThreshold()
        {
            if ((ExerciseThresholdType == ThresholdType.MedianAdaptiveThreshold) &&
                (all_trials.Count >= ExerciseThresholdLookbackCount))
            {
                var num_trials_to_skip = all_trials.Count - ExerciseThresholdLookbackCount;
                var trials_to_analyze = all_trials.Skip(num_trials_to_skip).ToList();
                var all_max_values = trials_to_analyze.Select(x => x.TrialMaximum).ToList();
                var new_threshold = TxBDC_Math.Median(all_max_values);

                //Set the new threshold value
                Threshold = new_threshold;

                //Is this ok to do? (I think so - Eric)
                Exercise.HitThreshold = Threshold;
            }
        }

        #endregion

        #region Background thread methods

        private void Background_thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Set flags indicating the background thread is no longer running
            current_session_state = SessionState.NotStarted;

            lock (worker_completed_lock)
            {
                worker_completed = true;
            }

            if (e.Error != null)
            {
                Crashes.TrackError(e.Error, new Dictionary<string, string>() { { "Repetitions Mode", "Error in background thread causing unexpected exit!" } });
                BackgroundThreadExitedInError = true;
                NotifyPropertyChanged("Model");
            }
        }

        private void Background_thread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string user_state = e.UserState as string;
            if (!string.IsNullOrEmpty(user_state))
            {
                NotifyPropertyChanged("DeviceMissing");
            }
            else
            {
                if (e.ProgressPercentage == (int)TrialState.Reset)
                {
                    NotifyPropertyChanged("TrialReset");
                }
                else
                {
                    NotifyPropertyChanged("Model");
                }
            }
        }

        private void Background_thread_DoWork(object sender, DoWorkEventArgs e)
        {
            var replay_exercise = Exercise as RePlayExerciseBase;
            if (replay_exercise != null)
            {
                replay_exercise.EnableStreaming(true);
            }
            
            //Reset the worker completed flag
            lock (worker_completed_lock)
            {
                worker_completed = false;
            }

            string exercise_string = ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType);

            //Get build information
            var build_date = BuildInformationManager.RetrieveBuildDate(activity);
            var version_name = BuildInformationManager.RetrieveVersionName();
            var version_code = BuildInformationManager.RetrieveVersionCode();

            //Initialize the VNS algorithm
            VNSManager.Initialize_VNS_Algorithm(DateTime.Now, vns_algorithm_parameters);

            //If this session is being run in "exercise" mode instead of "assessment" mode, then SubjectID will be
            //an empty string. This will affect the path to which data is being saved, and we want to make sure that
            //data is being saved to the correct path. So here we will set a temporary SubjectID that will be used
            //for data saving purposes.
            string temp_subject_id = "ReCheck_Exercises_UnknownSubjectID";
            if (!string.IsNullOrEmpty(SubjectID))
            {
                temp_subject_id = SubjectID;
            }

            //Open a file for saving data for this session
            Exercise.SetupFile(build_date, version_name, version_code, "ReCheck", exercise_string, 
                TabletID, temp_subject_id, false, vns_algorithm_parameters);
            Exercise_SaveData.SaveHandednessDefinition(Exercise.DataSaver, is_left_handed_session);

            string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string game_file_name = temp_subject_id + "_" + "ReCheck" + "_" + current_date_time_stamp + "_gamedata.txt";

            gamedata_save_file_handle = Exercise_SaveData.OpenFileForSaving(Exercise.CurrentActivity,
                game_file_name,
                build_date,
                version_name,
                version_code,
                TabletID,
                temp_subject_id,
                "ReCheck",
                exercise_string,
                ExerciseBase.StandardExerciseSensitivity,
                Exercise.Gain,
                ExerciseBase.StandardExerciseSensitivity,
                false,
                vns_algorithm_parameters);
            RepetitionsSaveGameData.SaveHandedness(gamedata_save_file_handle, is_left_handed_session);

            //Set the first trial to be a "positive" trial (as opposed to a negative trial)
            is_positive_trial = true;

            //Create a variable to hold the current trial
            TrialModel current_trial = null;

            if (Exercise != null)
            {
                //ReturnThreshold = Exercise.ReturnThreshold;
                DateTime start = DateTime.Now;

                RepetitionsSaveGameData.SaveMetaData(gamedata_save_file_handle, RequiredRepetitionsCount, ExerciseThresholdType, Exercise);
                RepetitionsSaveGameData.SaveRebaselineEvent(gamedata_save_file_handle, this, Exercise.RetrieveBaselineData());

                Log.Info("WORKER", "Entering while loop...");
                while (!background_thread.CancellationPending)
                {
                    if (replay_exercise != null)
                    {
                        var matching_device = replay_exercise.DoesDeviceMatch();
                        if (!matching_device)
                        {
                            if (current_session_state != SessionState.DeviceMissing)
                            {
                                current_session_state = SessionState.DeviceMissing;
                                background_thread.ReportProgress((int)current_trial_state, "DeviceMissing");
                            }
                        }
                    }

                    try
                    {
                        Exercise.Update();
                    }
                    catch (Exception exercise_updated_exception)
                    {
                        current_trial = null;
                        current_trial_state = TrialState.Ready;
                        current_session_state = SessionState.ErrorDetected;
                        Crashes.TrackError(exercise_updated_exception);
                        activity.RunOnUiThread(() =>
                        {
                            ErrorEncountered?.Invoke(this, new EventArgs());
                        });
                        break;
                    }

                    switch (current_session_state)
                    {
                        case SessionState.NotStarted:
                            break;
                        case SessionState.SetupFailed:
                            break;
                        case SessionState.ErrorDetected:
                            break;
                        case SessionState.DeviceMissing:
                            break;
                        case SessionState.BeginResetBaseline:

                            Exercise.EnableBaselineDataCollection(true);
                            reset_baseline_time = DateTime.Now;
                            current_session_state = SessionState.WaitResetBaseline;

                            break;
                        case SessionState.WaitResetBaseline:

                            if (DateTime.Now >= (reset_baseline_time + reset_baseline_duration))
                            {
                                current_session_state = SessionState.FinishResetBaseline;
                            }

                            break;
                        case SessionState.FinishResetBaseline:

                            Exercise.ResetExercise(true);
                            Exercise.EnableBaselineDataCollection(false);
                            current_session_state = SessionState.SessionRunning;
                            is_first_time = false;

                            RepetitionsSaveGameData.SaveRebaselineEvent(gamedata_save_file_handle, this, Exercise.RetrieveBaselineData());

                            break;
                        case SessionState.SessionRunning:

                            //Grab the latest value processed by the exercise's algorithm
                            debounce_list.Add(Exercise.CurrentActualValue);
                            
                            debounce_list.LimitTo(debounce_size, true);
                            if (debounce_list.Count == debounce_size)
                            {
                                if (DataVisualizationMode == RepetitionsModeDataVisualizationType.Actual)
                                {
                                    CurrentExerciseValue = TxBDC_Math.Median(debounce_list);
                                }
                                else if (DataVisualizationMode == RepetitionsModeDataVisualizationType.Velocity)
                                {
                                    CurrentExerciseValue = TxBDC_Math.Diff(debounce_list).Average();
                                }
                                else if (DataVisualizationMode == RepetitionsModeDataVisualizationType.PositiveVelocity)
                                {
                                    var diff_debounce_list = TxBDC_Math.Diff(debounce_list);
                                    for (int i = 0; i < debounce_list.Count && i < diff_debounce_list.Count; i++)
                                    {
                                        if (debounce_list[i] > 0 && diff_debounce_list[i] < 0)
                                        {
                                            diff_debounce_list[i] = 0;
                                        }
                                        else if (debounce_list[i] < 0 && diff_debounce_list[i] > 0)
                                        {
                                            diff_debounce_list[i] = 0;
                                        }
                                    }

                                    CurrentExerciseValue = diff_debounce_list.Average();
                                }
                                else
                                {
                                    CurrentExerciseValue = 0;
                                }
                            }
                            else
                            {
                                CurrentExerciseValue = 0;
                            }

                            //If the flag has been set indicating that the baseline needs to be reset, then
                            //let's change the session state accordingly and break out of this switch statement...
                            if (quick_rebaseline_flag)
                            {
                                lock (quick_rebaseline_flag_lock)
                                {
                                    quick_rebaseline_flag = false;
                                }

                                reset_baseline_duration = short_reset_baseline_duration;
                                current_session_state = SessionState.BeginResetBaseline;
                                current_trial_state = TrialState.Ready;

                                break;
                            }

                            //Take some actions depending on the current trial state...
                            switch (current_trial_state)
                            {
                                case TrialState.Reset:
                                    current_trial_state = TrialState.Ready;
                                    break;
                                case TrialState.Ready:

                                    CurrentTrialMaxValueAchieved = CurrentExerciseValue;

                                    bool progress = false;
                                    if (!Exercise.SinglePolarity)
                                    {
                                        if ((CurrentExerciseValue >= ReturnThreshold && is_positive_trial) ||
                                            (CurrentExerciseValue <= -ReturnThreshold && !is_positive_trial))
                                        {
                                            progress = true;
                                        }
                                    }
                                    else
                                    {
                                        if (CurrentExerciseValue >= ReturnThreshold)
                                        {
                                            progress = true;
                                        }
                                    }

                                    //Check to see if the initiation threshold has been exceeded
                                    if (progress)
                                    {
                                        //If so, create a new trial object
                                        current_trial = new TrialModel();
                                        current_trial.TrialStartTime = DateTime.Now;

                                        if (!Exercise.SinglePolarity)
                                        {
                                            current_trial.MotionDirection = (is_positive_trial) ? TrialMotionDirection.Positive : TrialMotionDirection.Negative;
                                        }
                                        else
                                        {
                                            current_trial.MotionDirection = (CurrentExerciseValue > 0) ? TrialMotionDirection.Positive : TrialMotionDirection.Negative;
                                        }

                                        //Set the trial state to be in progress
                                        current_trial_state = TrialState.InProgress;

                                        //Save the new trial header data into the file
                                        RepetitionsSaveGameData.SaveRepHeaderData(gamedata_save_file_handle, current_trial.TrialStartTime, Exercise);
                                    }

                                    break;
                                case TrialState.InProgress:

                                    //Determine if VNS should be triggered
                                    bool should_trigger_stimulation = false;
                                    try
                                    {
                                        should_trigger_stimulation = VNSManager.Determine_VNS_Triggering(DateTime.Now, CurrentExerciseValue);
                                    }
                                    catch (Exception)
                                    {
                                        //empty
                                    }
                                    
                                    if (should_trigger_stimulation)
                                    {
                                        Exercise_SaveData.SaveStimulationTriggerAtCurrentTime(Exercise.DataSaver);
                                        if (configuration_settings.AutomaticStimulationEnabled)
                                        {
                                            if (VNSManager.Parameters.Enabled)
                                            {
                                                StimulationsDelivered++;
                                                PCM.QuickStim();
                                            }
                                        }
                                    }

                                    //Check to see if this trial's max value has been exceeded
                                    if (Math.Abs(CurrentExerciseValue) > CurrentTrialMaxValueAchieved)
                                    {
                                        CurrentTrialMaxValueAchieved = Math.Abs(CurrentExerciseValue);
                                    }

                                    var current_time = DateTime.Now;

                                    //Check to see if the trial has finished by the user returning below the return threshold
                                    bool crossed_return_threshold = false;
                                    if ((current_trial.MotionDirection == TrialMotionDirection.Positive && CurrentExerciseValue < ReturnThreshold) ||
                                         (current_trial.MotionDirection == TrialMotionDirection.Negative && CurrentExerciseValue > -ReturnThreshold))
                                    {
                                        crossed_return_threshold = true;
                                    }

                                    if (crossed_return_threshold)
                                    {
                                        //If enough time has passed such that this can be considered a full trial
                                        if ((current_time - current_trial.TrialStartTime).TotalSeconds >= minimum_trial_duration_seconds)
                                        {
                                            //In this scenario, the trial has finished. Let's clean up...
                                            current_trial.TrialEndTime = DateTime.Now;

                                            //Get the peak of the signal for this trial
                                            try
                                            {
                                                current_trial.TrialMaximum = Math.Max(current_trial.TrialData.Max(), Math.Abs(current_trial.TrialData.Min()));
                                            }
                                            catch (Exception)
                                            {
                                                //empty
                                            }

                                            //Add this trial to the list of trials
                                            all_trials.Add(current_trial);

                                            //If the person successfully exceeded the trial threshold, count this as a repetition
                                            if (CurrentTrialMaxValueAchieved >= Threshold)
                                            {
                                                RepetitionsCompleted++;
                                                is_positive_trial = !is_positive_trial;
                                            }

                                            //Save the timestamp of the end of this attempt to the game data file
                                            RepetitionsSaveGameData.SaveEndOfAttemptAtCurrentTime(gamedata_save_file_handle);

                                            //Reset the current trial object
                                            current_trial = null;

                                            //Reset the trial state
                                            current_trial_state = TrialState.Reset;

                                            //Calculate a new hit threshold for the next trial
                                            CalculateNewHitThreshold();
                                        }
                                        else
                                        {
                                            //If the amount of time that has passed is less than the minimum trial duration...
                                            //Consider this a false trial, just reset everything
                                            current_trial = null;
                                            current_trial_state = TrialState.Reset;
                                        }
                                    }
                                    else
                                    {
                                        //In this scenario, the trial is still running...
                                        current_trial.TrialData.Add(CurrentExerciseValue);
                                    }

                                    break;
                            }

                            break;
                    }

                    //Save data to the file
                    Exercise.SaveExerciseData();

                    //Save the current game data
                    RepetitionsSaveGameData.SaveGameData(gamedata_save_file_handle, Exercise);

                    //Report changes to the GUI
                    background_thread.ReportProgress((int)current_trial_state);

                    //Sleep the thread for a bit so we don't consume the whole processor
                    Thread.Sleep(33);
                }
            }
        }

        #endregion
    }
}