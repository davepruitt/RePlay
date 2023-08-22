using Android.App;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using RePlay_Activity_Common;
using RePlay_Activity_SpaceRunner.UI;
using RePlay_Common;
using RePlay_Exercises;
using RePlay_VNS_Triggering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RePlay_Activity_SpaceRunner.Main
{
    public class InputManager
    {
        #region Private Properties

        private Activity Activity;
        private ExerciseDeviceType Device;
        private ExerciseType Type;
        public VNSAlgorithm_Standard VNS;
        private PCM_Manager PCM;
        private bool is_replay_debug_mode;

        private List<double> activity_buffer = new List<double>();

        #endregion

        #region Public Properties

        public ExerciseBase Exercise;

        public double NormalizedExerciseData { get; private set; }

        public double BinaryExerciseData { get; private set; }

        public TouchCollection TouchData { get; private set; }

        #endregion

        #region Constructor

        public InputManager(Activity a, PCM_Manager pcm, ExerciseDeviceType device, ExerciseType type, 
            string tablet, string subject, double gain, bool from_prescription,
            VNSAlgorithmParameters vns_algorithm_parameters, bool debug_mode)
        {
            is_replay_debug_mode = debug_mode;
            PCM = pcm;
            Device = device;
            Type = type;
            Activity = a;
            SetupExercise(tablet, subject, gain, from_prescription, vns_algorithm_parameters);
        }

        #endregion

        #region Public Methods

        public bool ReconnectToDevice ()
        {
            bool success = Exercise.SetupDevice();
            return success;
        }

        // Setup and instantiate exercise
        public void SetupExercise(string tablet, string subject, double gain, bool from_prescription,
            VNSAlgorithmParameters vns_algorithm_parameters)
        {
            VNS = new VNSAlgorithm_Standard();
            VNS.Initialize_VNS_Algorithm(DateTime.Now, vns_algorithm_parameters);
            PCM.PropertyChanged += (b, c) =>
            {
                //empty
            };

            PCM.PCM_Event += (a, b) =>
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

            Exercise = ExerciseBase.InstantiateCorrectExerciseClass(Type, Activity, gain);
            if (Device == ExerciseDeviceType.ReCheck)
            {
                bool ready = Exercise.SetupDevice();
                if (!ready) throw new Exception("Device could not be setup!");

                ready = false;
                var start = DateTime.Now;
                while (!ready || (DateTime.Now - start).TotalMilliseconds < 2000)
                {
                    ready = Exercise.ResetExercise();
                }

                //Get build information
                var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(Game.Activity);
                var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
                var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

                Exercise.SetupFile(build_date,
                    version_name,
                    version_code,
                    "SpaceRunner", 
                    ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(Type), 
                    tablet, 
                    subject,
                    from_prescription,
                    vns_algorithm_parameters);
            }
            else if (Device == ExerciseDeviceType.FitMi)
            {
                Exercise.SinglePolarity = true;
                bool ready = Exercise.SetupDevice();
                if (!ready) throw new Exception("Device could not be setup!");

                ready = false;
                var start = DateTime.Now;
                while (!ready || (DateTime.Now - start).TotalMilliseconds < 2000)
                {
                    ready = Exercise.ResetExercise();
                }

                //Get build information
                var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(Game.Activity);
                var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
                var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

                Exercise.SetupFile(build_date, 
                    version_name, 
                    version_code, 
                    "SpaceRunner", 
                    ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(Type), 
                    tablet, 
                    subject,
                    from_prescription,
                    vns_algorithm_parameters);
            }
            else
            {
                SetupFile(tablet, subject);
            }
        }

        // Update exercise data
        public void Update(TouchCollection touchCollection, SpaceRunner_GameplayUI gameplay_ui)
        {
            TouchData = touchCollection;
            
            if (Device == ExerciseDeviceType.FitMi || Device == ExerciseDeviceType.ReCheck)
            {
                Exercise.Update();
                NormalizedExerciseData = Exercise.CurrentNormalizedValue;

                activity_buffer.Add(Math.Abs(NormalizedExerciseData));
                activity_buffer.LimitTo(10, true);
                BinaryExerciseData = activity_buffer.All(x => x >= 0.05) ? 1 : 0;
                
                Exercise.SaveExerciseData();

                bool stim = VNS.Determine_VNS_Triggering(DateTime.Now, Exercise.CurrentNormalizedValue);
                if (stim)
                {
                    gameplay_ui.DisplayStimulationIcon(VNS.Parameters.Enabled, TimeSpan.FromSeconds(2.0));
                    Exercise_SaveData.SaveStimulationTriggerAtCurrentTime(Exercise.DataSaver);
                    if (VNS.Parameters.Enabled)
                    {
                        PCM.QuickStim();
                    }
                }

                if (is_replay_debug_mode)
                {
                    var game_activity = Game.Activity as RePlay_Game_Activity;
                    if (game_activity != null)
                    {
                        game_activity.game_signal_chart.AddDataPoint(Math.Abs(Exercise.CurrentNormalizedValue));
                        game_activity.vns_signal_chart.AddDataPoint(
                            VNS.Plotting_Get_Latest_Calculated_Value(),
                            VNS.Plotting_Get_VNS_Positive_Threshold(),
                            VNS.Plotting_Get_VNS_Negative_Threshold()
                            );
                    }
                }
            }
            else
            {
                if (TouchData.Count > 0)
                {
                    if (TouchData[0].State == TouchLocationState.Pressed) BinaryExerciseData = 1;
                    else if (TouchData[0].State == TouchLocationState.Released) BinaryExerciseData = 0;
                }

                bool stim = VNS.Determine_VNS_Triggering(DateTime.Now, BinaryExerciseData);
                if (stim)
                {
                    gameplay_ui.DisplayStimulationIcon(VNS.Parameters.Enabled, TimeSpan.FromSeconds(2.0));
                    if (VNS.Parameters.Enabled)
                    {
                        PCM.QuickStim();
                    }
                }
            }
        }

        // Close input
        public void CloseInput()
        {
            if (Device == ExerciseDeviceType.FitMi || Device == ExerciseDeviceType.ReCheck)
            {
                Exercise.CloseFile();
                Exercise.Close();
                //VNS.CloseRecordingFile();
            }
        }

        public void SetupFile(string tablet, string subject_id)
        {
            string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string game_name = "SpaceRunner";
            string file_name = subject_id + "_" + game_name + "_" + current_date_time_stamp + ".txt";
            string external_file_storage = Activity.ApplicationContext.GetExternalFilesDir(null).AbsolutePath;
            //string file_path = Path.Combine(external_file_storage, spacerunner_file_path);
            //file_path = Path.Combine(file_path, file_name);

            ////Create the folder if it does not exist
            //new FileInfo(file_path).Directory.Create();

            ////Open a handle to be able to write to the file
            //var f_stream = new FileStream(file_path, FileMode.Create);
            //BinaryWriter result = new BinaryWriter(f_stream, Encoding.ASCII);

            ////Write out header information for this file

            ////First, let's write a file version number
            //result.Write(touchscreen_data_file_version);

            ////Next, let's write a timestamp
            //DateTime session_start_time = DateTime.Now;
            //var matlab_version_of_start_time = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(session_start_time);
            //result.Write(matlab_version_of_start_time);
        }

        public string GetInstructions()
        {
            if (Device == ExerciseDeviceType.ReCheck)
            {
                return "Use the replay device to eject the astronaut";
            }
            else if (Device == ExerciseDeviceType.FitMi)
            {
                return "Press the FitMi puck to eject the astronaut";
            }
            else
            {
                return "Tap the screen to eject the astronaut";
            }
        }

        #endregion

    }
}