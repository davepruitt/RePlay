using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RePlay_DeviceCommunications;

namespace RePlay_Exercises.RePlay
{
    public class RePlayExercise_IsometricPinch : RePlayExercise_Isometric
    {
        #region Private data members

        private long loadcell1_raw_current_value = 0;
        private long loadcell2_raw_current_value = 0;

        #endregion

        #region Constructor

        public RePlayExercise_IsometricPinch(ReplayMicrocontroller c, Activity a, double gain)
            : base(c, a, gain)
        {
            //empty
        }

        public RePlayExercise_IsometricPinch(Activity a, double gain)
            : base(a, gain)
        {
            //empty
        }

        #endregion

        #region Overrides

        public override void Update()
        {
            (var t, var s1, var s2) = ReplayController.FetchLatestData();

            if ((t != null) && (s1 != null) && (s2 != null) &&
                (t.Count > 0) && (s1.Count > 0) && (s2.Count > 0) &&
                (t.Count == s1.Count) && (s1.Count == s2.Count))
            {
                try
                {
                    //Fill in the debugging properties
                    lock (DebuggingPropertiesLock)
                    {
                        DebuggingProperties["L1_Raw"] = s1.Last();
                        DebuggingProperties["L1_Slope"] = ReplayController.Loadcell_1_Slope;
                        DebuggingProperties["L1_Offset"] = ReplayController.Loadcell_1_Offset;
                        DebuggingProperties["L2_Raw"] = s2.Last();
                        DebuggingProperties["L2_Slope"] = ReplayController.Loadcell_2_Slope;
                        DebuggingProperties["L2_Offset"] = ReplayController.Loadcell_2_Offset;
                        DebuggingProperties["Lx_Gain"] = ReplayController.Loadcell_Gain;
                        DebuggingProperties["Device"] = (int)ReplayController.CurrentDeviceType;
                    }
                    //Finished filling in debugging properties

                    //Let's see if we are currently resetting the baseline...
                    if (reset_exercise_flag)
                    {
                        //Add values to the collection of raw loadcell values tht we will use to
                        //calculate a new baseline...
                        baseline_collection_1.AddRange(s1.Select(x => Convert.ToDouble(x)));
                        baseline_collection_2.AddRange(s2.Select(x => Convert.ToDouble(x)));
                    }
                    else
                    {
                        //If we are NOT resetting the baseline, then we carry on normally.
                        //In this case, we grab the latest raw values:
                        var latest_t = t.Last();
                        var latest_s1 = s1.Last();
                        var latest_s2 = s2.Last();

                        //Save the latest raw values to some private class variables
                        //This is important because the data saved to the data file is
                        //the raw data in these variables.
                        loadcell1_raw_current_value = latest_s1;
                        loadcell2_raw_current_value = latest_s2;

                        //Then we transform the values based on the baseline and the slope:
                        var transformed_s1 = (latest_s1 - Baseline_1) / Slope_1;
                        var transformed_s2 = (latest_s2 - Baseline_2) / Slope_2;

                        //Then we create a signal that combines both loadcells together:
                        var monitored_sig = -((transformed_s1 + transformed_s2) / 2.0);

                        //Then we set some properties that will be used by other parts of the application:
                        CurrentActualValue = monitored_sig;
                    }
                }
                catch (Exception e)
                {
                    //empty
                }
            }
        }

        public override void SaveExerciseData()
        {
            Exercise_SaveData.SaveRePlayPinchExerciseData(DataSaver, 
                loadcell1_raw_current_value, 
                loadcell2_raw_current_value);
        }

        #endregion

        public override bool DoesDeviceMatch()
        {
            if (ReplayController != null)
            {
                return (ReplayController.CurrentDeviceType == ReplayDeviceType.Pinch);
            }
            else
            {
                return false;
            }
        }
    }
}