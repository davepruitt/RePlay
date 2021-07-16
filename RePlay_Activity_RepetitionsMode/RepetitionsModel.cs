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
using RePlay_Activity_Common;
using RePlay_Exercises.RePlay;

namespace RePlay_Activity_RepetitionsMode
{
    /// <summary>
    /// Model class for the rep-it-out mode
    /// </summary>
    public class RepetitionsModel : NotifyPropertyChangedObject
    {
        #region Private data members
        
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
        private DateTime session_start_time = DateTime.MaxValue;
        private DateTime reset_baseline_time = DateTime.Now;
        private TimeSpan reset_baseline_duration = TimeSpan.FromMilliseconds(5000);
        private TimeSpan long_reset_baseline_duration = TimeSpan.FromMilliseconds(5000);
        private TimeSpan short_reset_baseline_duration = TimeSpan.FromMilliseconds(100);
        private bool quick_rebaseline_flag = false;
        private object quick_rebaseline_flag_lock = new object();
        private bool is_positive_trial = true;
        private bool from_prescription;

        private VNSAlgorithmParameters vns_algorithm_parameters = new VNSAlgorithmParameters();

        #endregion

        #region Constructor

        public RepetitionsModel(Activity a)
        {
            //Initialize the FitMi and RePlay controller classes
            activity = a;
            VNSManager = new VNSAlgorithm_Standard();
            PCM = new PCM_Manager(a);
            PCM.PropertyChanged += (b, c) =>
            {
                //empty
            };

            PCM.PCM_Event += (useless, b) =>
            {
                try
                {
                    Exercise_SaveData.SaveMessageFromReStoreService(Exercise.DataSaver, b);
                }
                catch (Exception)
                {
                    //empty
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

        public double CurrentExerciseValue { get; private set; } = 0;

        public double CurrentTrialMaxValueAchieved { get; private set; } = 0;

        public double Threshold
        {
            get
            {
                if (Exercise != null)
                {
                    return Exercise.HitThreshold;
                }
                else
                {
                    return 0;
                }
            }
        }

        public double ReturnThreshold
        {
            get
            {
                if (Exercise != null)
                {
                    return Exercise.ReturnThreshold;
                }
                else
                {
                    return 0;
                }
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
        
        #endregion

        #region Public methods

        public int GetCountdownTimeRemaining ()
        {
            var elapsed_time = DateTime.Now - reset_baseline_time;
            var time_remaining = reset_baseline_duration - elapsed_time;
            return Convert.ToInt32(Math.Ceiling(time_remaining.TotalSeconds));
        }

        public void PerformQuickRebaseline ()
        {
            lock (quick_rebaseline_flag_lock)
            {
                quick_rebaseline_flag = true;
            }
        }

        public void ContinueExercise ()
        {
            if (current_session_state == SessionState.ErrorDetected)
            {
                current_session_state = SessionState.SessionRunning;
            }
        }

        public bool ReconnectToDevice ()
        {
            bool success = Exercise.SetupDevice();
            return success;
        }

        /// <summary>
        /// Begins execution of a new exercise
        /// </summary>
        /// <param name="exercise"></param>
        public void StartExercise(ExerciseType exercise, int num_reps, ThresholdType t, 
            string tablet, string subject, double gain, bool from_prescr,
            VNSAlgorithmParameters vnsAlgorithmParameters)
        {
            TabletID = tablet;
            
            //If the background thread isn't already executing...
            if (!background_thread.IsBusy)
            {
                //Set the exercise type
                ExerciseType = exercise;

                //Get the device type for this exercise
                var exercise_device_type = ExerciseTypeConverter.GetExerciseDeviceType(ExerciseType);
                Exercise = ExerciseBase.InstantiateCorrectExerciseClass(exercise, activity, gain);

                session_start_time = DateTime.Now;
                bool ready = Exercise.SetupDevice();
                if (ready)
                {
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
                    from_prescription = from_prescr;
                    vns_algorithm_parameters = vnsAlgorithmParameters;

                    BeginExercise();
                }
                else
                {
                    Crashes.TrackError(new Exception("Device could not be setup!"), new Dictionary<string, string>() { { "Repetitions Mode", "Error setting up device" } });
                    NotifyPropertyChanged("Model");
                    current_session_state = SessionState.SetupFailed;
                }
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
            Exercise.Close();
        }

        public async Task StopExercise_Async ()
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
                Exercise.Close();
            }
            catch (Exception e)
            {
                Crashes.TrackError(e, new Dictionary<string, string>() { { "ERROR", "Error while closing Repetitions Mode" } } );
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
                if (user_state.Equals("DeviceMissing"))
                {
                    NotifyPropertyChanged(user_state);
                    return;
                }
                else if (user_state.Equals("stim"))
                {
                    NotifyPropertyChanged(user_state);
                    return;
                }
            }

            NotifyPropertyChanged("Model");
        }

        private void Background_thread_DoWork(object sender, DoWorkEventArgs e)
        {
            //Reset the worker completed flag
            lock (worker_completed_lock)
            {
                worker_completed = false;
            }

            string exercise_string = ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType);

            //Get build information
            var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(activity);
            var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
            var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

            //Make an adjustment to the noise floor depending on whether we are using the
            //standard signal, transformed signal, or the normalized signal for control.
            if (Exercise is RePlayExerciseBase)
            {
                vns_algorithm_parameters.NoiseFloor = (vns_algorithm_parameters.NoiseFloor * Exercise.Gain);
            }
            else
            {
                vns_algorithm_parameters.NoiseFloor = (vns_algorithm_parameters.NoiseFloor * Exercise.Gain) / ExerciseBase.StandardExerciseSensitivity;
            }

            //Initialize the VNS algorithm
            VNSManager.Initialize_VNS_Algorithm(DateTime.Now, vns_algorithm_parameters);

            //Open a file for saving data for this session
            Exercise.SetupFile(build_date, version_name, version_code, "RepetitionsMode", 
                exercise_string, TabletID, SubjectID, from_prescription, vns_algorithm_parameters);

            string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string game_file_name = SubjectID + "_" + "RepetitionsMode" + "_" + current_date_time_stamp + "_gamedata.txt";

            gamedata_save_file_handle = Exercise_SaveData.OpenFileForSaving(Exercise.CurrentActivity,
                game_file_name, 
                build_date,
                version_name,
                version_code,
                TabletID,
                SubjectID, 
                "RepetitionsMode", 
                exercise_string,
                ExerciseBase.StandardExerciseSensitivity, 
                Exercise.Gain,
                ExerciseBase.StandardExerciseSensitivity,
                from_prescription,
                vns_algorithm_parameters);

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
                    bool did_stim_request_occur = false;

                    if (Exercise is RePlayExerciseBase)
                    {
                        var replay_exercise = Exercise as RePlayExerciseBase;
                        var replay_controller = replay_exercise.GetController();
                        if (replay_controller != null)
                        {
                            var last_device_update_time = replay_controller.LastCurrentDeviceTypeUpdateTime;
                            if (last_device_update_time > session_start_time)
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

                            //Grab the exercise's current value
                            if (Exercise is RePlayExerciseBase)
                            {
                                //For RePlay exercises, we are showing the "transformed" value
                                debounce_list.Add(Exercise.CurrentTransformedValue);
                            }
                            else
                            {
                                //For FitMi exercises, we are showing the "normalized" value
                                debounce_list.Add(Exercise.CurrentNormalizedValue);
                            }
                            
                            debounce_list.LimitTo(debounce_size, true);
                            if (debounce_list.Count == debounce_size)
                            {
                                if (Exercise.ConvertSignalToVelocity)
                                {
                                    CurrentExerciseValue = TxBDC_Math.Diff(debounce_list).Average();
                                }
                                else
                                {
                                    CurrentExerciseValue = TxBDC_Math.Median(debounce_list);
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
                                VNSManager?.Flush_VNS_Buffers();

                                break;
                            }

                            //Regardless of trial state, if the session is running, let's determine whether to stimulate
                            //The "current stimulation algorithm value" is equal to the median of the debounce list. This is basically
                            //the same as the "CurrentExerciseValue" unless the Exercise has ConvertSignalToVelocity set to true.
                            var current_stimulation_algorithm_value = (debounce_list.Count >= debounce_size) ? TxBDC_Math.Median(debounce_list) : 0;
                            bool stimRunning = VNSManager.Determine_VNS_Triggering(DateTime.Now, current_stimulation_algorithm_value);
                            if (stimRunning)
                            {
                                did_stim_request_occur = true;
                                Exercise_SaveData.SaveStimulationTriggerAtCurrentTime(Exercise.DataSaver);
                                if (VNSManager.Parameters.Enabled)
                                {
                                    PCM.QuickStim();
                                }
                            }

                            //Take some actions depending on the current trial state...
                            switch (current_trial_state)
                            {
                                case TrialState.Ready:
                                    
                                    CurrentTrialMaxValueAchieved = CurrentExerciseValue;

                                    bool progress_to_next_state = false;
                                    if (Exercise.SinglePolarity)
                                    {
                                        if (CurrentExerciseValue >= Exercise.ReturnThreshold)
                                        {
                                            progress_to_next_state = true;
                                        }
                                    }
                                    else
                                    {
                                        if (Exercise.ForceAlternation)
                                        {
                                            if ((CurrentExerciseValue >= Exercise.ReturnThreshold && is_positive_trial) ||
                                                (CurrentExerciseValue <= -Exercise.ReturnThreshold && !is_positive_trial))
                                            {
                                                progress_to_next_state = true;
                                            }
                                            
                                        }
                                        else
                                        {
                                            if (CurrentExerciseValue >= Exercise.ReturnThreshold || CurrentExerciseValue <= -Exercise.ReturnThreshold)
                                            {
                                                progress_to_next_state = true;
                                                is_positive_trial = CurrentExerciseValue >= 0;
                                            }
                                        }
                                    }

                                    //Check to see if the initiation threshold has been exceeded
                                    if (progress_to_next_state)
                                    {
                                        //If so, create a new trial object
                                        current_trial = new TrialModel();
                                        current_trial.TrialStartTime = DateTime.Now;

                                        if (Exercise.SinglePolarity)
                                        {
                                            current_trial.MotionDirection = TrialMotionDirection.Positive;
                                        }
                                        else
                                        {
                                            current_trial.MotionDirection = (is_positive_trial) ? TrialMotionDirection.Positive : TrialMotionDirection.Negative;
                                        }

                                        //Set the trial state to be in progress
                                        current_trial_state = TrialState.InProgress;

                                        //Save the new trial header data into the file
                                        RepetitionsSaveGameData.SaveRepHeaderData(gamedata_save_file_handle, current_trial.TrialStartTime, Exercise);
                                    }
                                    
                                    break;
                                case TrialState.InProgress:

                                    //Check to see if this trial's max value has been exceeded
                                    if (Math.Abs(CurrentExerciseValue) > CurrentTrialMaxValueAchieved)
                                    {
                                        CurrentTrialMaxValueAchieved = Math.Abs(CurrentExerciseValue);
                                    }

                                    var current_time = DateTime.Now;

                                    //Check to see if the trial has finished by the user returning below the return threshold
                                    bool crossed_return_threshold = false;
                                    if ((current_trial.MotionDirection == TrialMotionDirection.Positive && CurrentExerciseValue < Exercise.ReturnThreshold) ||
                                         (current_trial.MotionDirection == TrialMotionDirection.Negative && CurrentExerciseValue > -Exercise.ReturnThreshold))
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
                                            if (CurrentTrialMaxValueAchieved >= Exercise.HitThreshold)
                                            {
                                                RepetitionsCompleted++;
                                                is_positive_trial = !is_positive_trial;
                                            }

                                            //Save the timestamp of the end of this attempt to the game data file
                                            RepetitionsSaveGameData.SaveEndOfAttemptAtCurrentTime(gamedata_save_file_handle);

                                            //Reset the current trial object
                                            current_trial = null;

                                            //Reset the trial state
                                            current_trial_state = TrialState.Ready;
                                        }
                                        else
                                        {
                                            //If the amount of time that has passed is less than the minimum trial duration...
                                            //Consider this a false trial, just reset everything
                                            current_trial = null;
                                            current_trial_state = TrialState.Ready;
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
                    string user_state_msg = (did_stim_request_occur) ? "stim" : string.Empty;
                    background_thread.ReportProgress(0, user_state_msg);

                    //Sleep the thread for a bit so we don't consume the whole processor
                    Thread.Sleep(10);
                }
            }
        }

        #endregion
    }
}