using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Nfc;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Microsoft.Appcenter.Ingestion.Models;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using ReCheck.Droid.Model;
using ReCheck.Model;
using RePlay_Common;
using RePlay_DeviceCommunications;
using RePlay_Exercises;

namespace ReCheck.Droid.ViewModel
{
    public class ExercisePage_NoDevice_ViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private int total_stimulations = 0;

        ReplayMicrocontroller replayMicrocontroller;
        Participant participant;
        Xamarin.Forms.Color txbdc_green_color;

        List<ExerciseType> completed_left_hand_exercises = new List<ExerciseType>();
        List<ExerciseType> completed_right_hand_exercises = new List<ExerciseType>();
        Dictionary<ExerciseType, Tuple<double, double>> left_hand_exercise_results = new Dictionary<ExerciseType, Tuple<double, double>>();
        Dictionary<ExerciseType, Tuple<double, double>> right_hand_exercise_results = new Dictionary<ExerciseType, Tuple<double, double>>();

        Dictionary<ExerciseType, List<string>> left_property_names = new Dictionary<ExerciseType, List<string>>()
        {
            { ExerciseType.RePlay_Isometric_Handle, new List<string>() { "LSH_Color", "LSH_Visible", "LSH_Usage" } },
            { ExerciseType.RePlay_Isometric_Knob, new List<string>() { "LSK_Color", "LSK_Visible", "LSK_Usage" } },
            { ExerciseType.RePlay_Isometric_Wrist, new List<string>() { "LSW_Color", "LSW_Visible", "LSW_Usage" } },
            { ExerciseType.RePlay_Isometric_PinchLeft, new List<string>() { "LSP_Color", "LSP_Visible", "LSP_Usage" } },
            { ExerciseType.RePlay_RangeOfMotion_Handle, new List<string>() { "LMH_Color", "LMH_Visible", "LMH_Usage" } },
            { ExerciseType.RePlay_RangeOfMotion_Knob, new List<string>() { "LMK_Color", "LMK_Visible", "LMK_Usage" } },
            { ExerciseType.RePlay_RangeOfMotion_Wrist, new List<string>() { "LMW_Color", "LMW_Visible", "LMW_Usage" } }
        };

        Dictionary<ExerciseType, List<string>> right_property_names = new Dictionary<ExerciseType, List<string>>()
        {
            { ExerciseType.RePlay_Isometric_Handle, new List<string>() { "RSH_Color", "RSH_Visible", "RSH_Usage" } },
            { ExerciseType.RePlay_Isometric_Knob, new List<string>() { "RSK_Color", "RSK_Visible", "RSK_Usage" } },
            { ExerciseType.RePlay_Isometric_Wrist, new List<string>() { "RSW_Color", "RSW_Visible", "RSW_Usage" } },
            { ExerciseType.RePlay_Isometric_Pinch, new List<string>() { "RSP_Color", "RSP_Visible", "RSP_Usage" } },
            { ExerciseType.RePlay_RangeOfMotion_Handle, new List<string>() { "RMH_Color", "RMH_Visible", "RMH_Usage" } },
            { ExerciseType.RePlay_RangeOfMotion_Knob, new List<string>() { "RMK_Color", "RMK_Visible", "RMK_Usage" } },
            { ExerciseType.RePlay_RangeOfMotion_Wrist, new List<string>() { "RMW_Color", "RMW_Visible", "RMW_Usage" } }
        };

        #endregion

        #region Constructor

        public ExercisePage_NoDevice_ViewModel(ReplayMicrocontroller microcontroller, Participant p, Xamarin.Forms.Color txbdc_green_color_resource)
        {
            replayMicrocontroller = microcontroller;
            participant = p;
            txbdc_green_color = txbdc_green_color_resource;

            if (participant != null)
            {
                participant.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
            }

            if (replayMicrocontroller != null)
            {
                replayMicrocontroller.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
            }
        }

        #endregion

        #region Methods

        public void SaveReport ()
        {
            string result = string.Empty;
            List<ExerciseType> ordered_exercises = new List<ExerciseType>()
            {
                ExerciseType.RePlay_Isometric_Handle,
                ExerciseType.RePlay_Isometric_Knob,
                ExerciseType.RePlay_Isometric_Wrist,
                ExerciseType.RePlay_Isometric_Pinch,
                ExerciseType.RePlay_RangeOfMotion_Handle,
                ExerciseType.RePlay_RangeOfMotion_Knob,
                ExerciseType.RePlay_RangeOfMotion_Wrist
            };

            List<string> ordered_keys = new List<string>()
            {
                "Participant ID",
                "Date",
                "Stims",

                "LEFT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_Isometric_Handle),
                "LEFT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_Isometric_Knob),
                "LEFT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_Isometric_Wrist),
                "LEFT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_Isometric_PinchLeft),
                "LEFT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_RangeOfMotion_Handle),
                "LEFT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_RangeOfMotion_Knob),
                "LEFT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_RangeOfMotion_Wrist),

                "RIGHT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_Isometric_Handle),
                "RIGHT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_Isometric_Knob),
                "RIGHT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_Isometric_Wrist),
                "RIGHT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_Isometric_Pinch),
                "RIGHT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_RangeOfMotion_Handle),
                "RIGHT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_RangeOfMotion_Knob),
                "RIGHT " + ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(ExerciseType.RePlay_RangeOfMotion_Wrist),
            };

            List<string> ordered_values = new List<string>();

            //Add the participant ID to the list of ordered values
            string participant_id = string.Empty;
            if (participant != null)
            {
                participant_id = participant.ParticipantID;
                if (string.IsNullOrEmpty(participant_id))
                {
                    participant_id = "UNKNOWN";
                }
            }
            else
            {
                participant_id = "EXERCISE";
            }

            ordered_values.Add(participant_id);

            //Now add the current date
            ordered_values.Add("'" + DateTime.Now.ToString());

            //Now add the total stims
            ordered_values.Add(total_stimulations.ToString());

            //Now add the results for all left and right hand exercises
            for (int hand = 0; hand < 2; hand++)
            {
                for (int i = 0; i < ordered_exercises.Count; i++)
                {
                    var current_exercise = ordered_exercises[i];
                    if (hand == 0 && current_exercise == ExerciseType.RePlay_Isometric_Pinch)
                    {
                        current_exercise = ExerciseType.RePlay_Isometric_PinchLeft;
                    }

                    //Check to see if this exercise was performed
                    if (hand == 0)
                    {
                        if (left_hand_exercise_results.ContainsKey(current_exercise))
                        {
                            var mean = Convert.ToInt32(Math.Round(left_hand_exercise_results[current_exercise].Item1));
                            var err = Convert.ToInt32(Math.Round(left_hand_exercise_results[current_exercise].Item2));
                            var str_to_add = mean + " +/- " + err;
                            ordered_values.Add(str_to_add);
                        }
                        else
                        {
                            ordered_values.Add("NaN");
                        }
                    }
                    else
                    {
                        if (right_hand_exercise_results.ContainsKey(current_exercise))
                        {
                            var mean = Convert.ToInt32(Math.Round(right_hand_exercise_results[current_exercise].Item1));
                            var err = Convert.ToInt32(Math.Round(right_hand_exercise_results[current_exercise].Item2));
                            var str_to_add = mean + " +/- " + err;
                            ordered_values.Add(str_to_add);
                        }
                        else
                        {
                            ordered_values.Add("NaN");
                        }
                    }
                }
            }

            //Now let's print this out to the report file
            string external_file_storage = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            string file_name = "ReCheck_Generated_Report.csv";
            string replay_path = Path.Combine(external_file_storage, "TxBDC");
            replay_path = Path.Combine(replay_path, "ReCheck Generated Reports");
            string file_path = Path.Combine(replay_path, file_name);

            FileInfo file_info = new FileInfo(file_path);
            bool does_file_exist = file_info.Exists;

            //Create the folder if it does not exist
            new FileInfo(file_path).Directory.Create();

            //Open the file to append to it
            StreamWriter writer = new StreamWriter(file_path, true);

            //If the file did not exist to begin with, then write out the column headers
            if (!does_file_exist)
            {
                for (int i = 0; i < ordered_keys.Count; i++)
                {
                    writer.Write(ordered_keys[i]);
                    if (i < (ordered_keys.Count - 1))
                    {
                        writer.Write(",");
                    }
                }

                writer.WriteLine();
            }

            //Now write the data from this session
            for (int i = 0; i < ordered_values.Count; i++)
            {
                writer.Write(ordered_values[i]);
                if (i < (ordered_values.Count - 1))
                {
                    writer.Write(",");
                }
            }

            writer.WriteLine();

            //Close the writer
            writer.Flush();
            writer.Close();
        }

        public void UpdateTotalStimulations (RepetitionsModel reps_model)
        {
            if (reps_model != null)
            {
                total_stimulations += reps_model.StimulationsDelivered;
                NotifyPropertyChanged("TotalStimulationsReceived");
            }
        }

        public void AddToCompletedList(ExerciseType t, double mean, double err, bool is_left_hand)
        {
            if (is_left_hand)
            {
                if (!completed_left_hand_exercises.Contains(t))
                {
                    if (t == ExerciseType.RePlay_Isometric_Pinch)
                    {
                        t = ExerciseType.RePlay_Isometric_PinchLeft;
                    }

                    completed_left_hand_exercises.Add(t);
                    left_hand_exercise_results[t] = new Tuple<double, double>(mean, err);

                    foreach (var p in left_property_names[t])
                    {
                        NotifyPropertyChanged(p);
                    }
                }
            }
            else if (!completed_right_hand_exercises.Contains(t))
            {
                if (t == ExerciseType.RePlay_Isometric_PinchLeft)
                {
                    Log.Debug("ReCheck", "You shouldn't be here!");
                    Analytics.TrackEvent("Left-handed pinch with right hand");
                    return;
                }

                completed_right_hand_exercises.Add(t);
                right_hand_exercise_results[t] = new Tuple<double, double>(mean, err);

                foreach (var p in right_property_names[t])
                {
                    NotifyPropertyChanged(p);
                }
            }
        }

        #endregion

        #region Properties

        public string TotalStimulationsReceived
        {
            get
            {
                return total_stimulations.ToString();
            }
        }

        [ReactToModelPropertyChanged(new string[] { "ExercisesCompleted" })]
        public string CompletedExercisesText
        {
            get
            {
                string result = string.Empty;

                if (participant != null)
                {
                    if (participant.ExercisesCompleted.Count > 0)
                    {
                        result += "You have completed the following exercises so far:\n\n";

                        var distinct_values = participant.ExercisesCompleted.Distinct().ToList();
                        for (int i = 0; i < distinct_values.Count; i++)
                        {
                            string exercise_name = ExerciseTypeConverter.ConvertExerciseTypeToDescription(distinct_values[i]);
                            result += "(" + (i + 1).ToString() + ") " + exercise_name + "\n";
                        }
                    }
                }

                return result;
            }
        }

        [ReactToModelPropertyChanged(new string[] { "ExercisesCompleted" })]
        public bool IsCompletedExercisesTextVisible
        {
            get
            {
                if (participant != null)
                {
                    return (participant.ExercisesCompleted.Count > 0);
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion

        #region Colors

        public Xamarin.Forms.Color LSH_Color
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Handle)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color LSK_Color
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Knob)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color LSW_Color
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Wrist)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color LSP_Color
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_Isometric_PinchLeft)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color LMH_Color
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Handle)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color LMK_Color
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Knob)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color LMW_Color
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Wrist)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color RSH_Color
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Handle)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color RSK_Color
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Knob)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color RSW_Color
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Wrist)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color RSP_Color
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Pinch)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color RMH_Color
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Handle)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color RMK_Color
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Knob)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        public Xamarin.Forms.Color RMW_Color
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Wrist)) ? txbdc_green_color : Xamarin.Forms.Color.LightGray;
            }
        }

        #endregion

        #region Check marks

        public bool LSH_Visible
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Handle));
            }
        }

        public bool LSK_Visible
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Knob));
            }
        }

        public bool LSW_Visible
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Wrist));
            }
        }

        public bool LSP_Visible
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_Isometric_PinchLeft));
            }
        }

        public bool LMH_Visible
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Handle));
            }
        }

        public bool LMK_Visible
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Knob));
            }
        }

        public bool LMW_Visible
        {
            get
            {
                return (completed_left_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Wrist));
            }
        }

        public bool RSH_Visible
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Handle));
            }
        }

        public bool RSK_Visible
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Knob));
            }
        }

        public bool RSW_Visible
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Wrist));
            }
        }

        public bool RSP_Visible
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_Isometric_Pinch));
            }
        }

        public bool RMH_Visible
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Handle));
            }
        }

        public bool RMK_Visible
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Knob));
            }
        }

        public bool RMW_Visible
        {
            get
            {
                return (completed_right_hand_exercises.Contains(ExerciseType.RePlay_RangeOfMotion_Wrist));
            }
        }

        #endregion

        #region Left-hand usage data

        private string GetUsageDataString (bool left_hand, ExerciseType t)
        {
            string result = string.Empty;

            if (left_hand)
            {
                if (left_hand_exercise_results.ContainsKey(t))
                {
                    var tuple = left_hand_exercise_results[t];
                    result += Convert.ToInt32(Math.Round(tuple.Item1)).ToString() + " " + Char.ConvertFromUtf32(0xB1) + " " +
                        Convert.ToInt32(Math.Round(tuple.Item2)).ToString();
                }
            }
            else
            {
                if (right_hand_exercise_results.ContainsKey(t))
                {
                    var tuple = right_hand_exercise_results[t];
                    result += Convert.ToInt32(Math.Round(tuple.Item1)).ToString() + " " + Char.ConvertFromUtf32(0xB1) + " " +
                        Convert.ToInt32(Math.Round(tuple.Item2)).ToString();
                }
            }

            //Add the units as a suffix
            if (!string.IsNullOrEmpty(result))
            {
                if (t == ExerciseType.RePlay_RangeOfMotion_Handle || t == ExerciseType.RePlay_RangeOfMotion_Knob ||
                t == ExerciseType.RePlay_RangeOfMotion_Wrist)
                {
                    result += "d";
                }
                else
                {
                    result += "g";
                }
            }
            
            return result;
        }

        public string LSH_Usage
        {
            get
            {
                return GetUsageDataString(true, ExerciseType.RePlay_Isometric_Handle);
            }
        }

        public string LSK_Usage
        {
            get
            {
                return GetUsageDataString(true, ExerciseType.RePlay_Isometric_Knob);
            }
        }

        public string LSW_Usage
        {
            get
            {
                return GetUsageDataString(true, ExerciseType.RePlay_Isometric_Wrist);
            }
        }

        public string LSP_Usage
        {
            get
            {
                return GetUsageDataString(true, ExerciseType.RePlay_Isometric_PinchLeft);
            }
        }

        public string LMH_Usage
        {
            get
            {
                return GetUsageDataString(true, ExerciseType.RePlay_RangeOfMotion_Handle);
            }
        }

        public string LMK_Usage
        {
            get
            {
                return GetUsageDataString(true, ExerciseType.RePlay_RangeOfMotion_Knob);
            }
        }

        public string LMW_Usage
        {
            get
            {
                return GetUsageDataString(true, ExerciseType.RePlay_RangeOfMotion_Wrist);
            }
        }

        #endregion

        #region Right-hand usage data
        public string RSH_Usage
        {
            get
            {
                return GetUsageDataString(false, ExerciseType.RePlay_Isometric_Handle);
            }
        }

        public string RSK_Usage
        {
            get
            {
                return GetUsageDataString(false, ExerciseType.RePlay_Isometric_Knob);
            }
        }

        public string RSW_Usage
        {
            get
            {
                return GetUsageDataString(false, ExerciseType.RePlay_Isometric_Wrist);
            }
        }

        public string RSP_Usage
        {
            get
            {
                return GetUsageDataString(false, ExerciseType.RePlay_Isometric_Pinch);
            }
        }

        public string RMH_Usage
        {
            get
            {
                return GetUsageDataString(false, ExerciseType.RePlay_RangeOfMotion_Handle);
            }
        }

        public string RMK_Usage
        {
            get
            {
                return GetUsageDataString(false, ExerciseType.RePlay_RangeOfMotion_Knob);
            }
        }

        public string RMW_Usage
        {
            get
            {
                return GetUsageDataString(false, ExerciseType.RePlay_RangeOfMotion_Wrist);
            }
        }

        #endregion

        #region Button visibility

        [ReactToModelPropertyChanged(new string[] { "CurrentDeviceType" })]
        public bool IsLeftHandButtonVisible
        {
            get
            {
                if (replayMicrocontroller != null)
                {
                    return (replayMicrocontroller.CurrentDeviceType != ReplayDeviceType.Unknown &&
                        replayMicrocontroller.CurrentDeviceType != ReplayDeviceType.Pinch);
                }
                else
                {
                    return false;
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "CurrentDeviceType" })]
        public bool IsRightHandButtonVisible
        {
            get
            {
                if (replayMicrocontroller != null)
                {
                    return (replayMicrocontroller.CurrentDeviceType != ReplayDeviceType.Unknown &&
                        replayMicrocontroller.CurrentDeviceType != ReplayDeviceType.Pinch_Left);
                }
                else
                {
                    return false;
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "CurrentDeviceType" })]
        public bool AreHandednessButtonsVisible
        {
            get
            {
                if (replayMicrocontroller != null)
                {
                    return (replayMicrocontroller.CurrentDeviceType != ReplayDeviceType.Unknown);
                }
                else
                {
                    return false;
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "CurrentDeviceType" })]
        public bool IsSecondaryTextVisible
        {
            get
            {
                if (replayMicrocontroller != null)
                {
                    return (replayMicrocontroller.CurrentDeviceType == ReplayDeviceType.Unknown);
                }
                else
                {
                    return false;
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "CurrentDeviceType" })]
        public string PrimaryText
        {
            get
            {
                if (replayMicrocontroller != null)
                {
                    if (replayMicrocontroller.CurrentDeviceType != ReplayDeviceType.Unknown)
                    {
                        var device_description = ReplayDeviceTypeConverter.ConvertDeviceTypeToDescription(replayMicrocontroller.CurrentDeviceType);
                        return "Select an arm for the " + device_description + " module.";
                    }
                }

                return "PLEASE INSERT A MODULE";
            }
        }

        [ReactToModelPropertyChanged(new string[] { "CurrentDeviceType" })]
        public string SecondaryText
        {
            get
            {
                if (replayMicrocontroller != null)
                {
                    if (replayMicrocontroller.CurrentDeviceType != ReplayDeviceType.Unknown)
                    {
                        return " ";
                    }
                }

                return "Modules that you have already used during this session are highlighted in green below.";
            }
        }

        #endregion
    }
}