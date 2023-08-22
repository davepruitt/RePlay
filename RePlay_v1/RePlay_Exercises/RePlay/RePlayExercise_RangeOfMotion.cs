using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using RePlay_Common;
using RePlay_DeviceCommunications;
using RePlay_VNS_Triggering;

namespace RePlay_Exercises.RePlay
{
    public abstract class RePlayExercise_RangeOfMotion : RePlayExerciseBase
    {
        #region Protected variables

        protected List<double> baseline_collection = new List<double>();
        private int baseline_collection_max_elements = 5000;
        private bool reset_exercise_flag = false;

        private double TICKS_PER_DEGREE = 4.0;
        protected double Baseline_1 = 0;

        #endregion

        #region Constructor

        public RePlayExercise_RangeOfMotion (ReplayMicrocontroller c, Activity a, double gain)
            : base(c, a, gain)
        {
            MaximumNormalizedRange = 30;
            DebuggingProperties = new Dictionary<string, double>()
            {
                { "Raw", 0 }
            };
        }

        public RePlayExercise_RangeOfMotion (Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 30;
        }

        #endregion

        #region Overrides

        public override List<double> RetrieveBaselineData()
        {
            return new List<double>() { Baseline_1 };
        }

        public override void EnableBaselineDataCollection(bool enable)
        {
            reset_exercise_flag = enable;
            baseline_collection.Clear();
        }

        public override bool ResetExercise(bool long_reset = false)
        {
            bool success = false;

            if (long_reset)
            {
                if (baseline_collection.Count > 0)
                {
                    Baseline_1 = TxBDC_Math.CalcuateWeightedMeanUsingIndicesAsWeights(baseline_collection);
                    success = true;
                }
            }
            else
            {
                (var t, var s1, var s2) = ReplayController.FetchLatestData(true, 100);
                if (t.Count > 0)
                {
                    var latest_s1 = TxBDC_Math.Median(s1);
                    Baseline_1 = latest_s1;
                    success = true;
                }
            }

            //Save the new baseline
            Exercise_SaveData.SaveRePlayRangeOfMotionCalibrationData(DataSaver, Baseline_1);

            //Return
            return success;
        }
        
        public override void Update()
        {
            (var t, var s1, var s2) = ReplayController.FetchLatestData();

            if ((t != null) && (s1 != null) &&
                (t.Count > 0) && (s1.Count > 0) &&
                (t.Count == s1.Count))
            {
                try
                {
                    if (reset_exercise_flag)
                    {
                        baseline_collection.AddRange(s1.Select(x => Convert.ToDouble(x)));
                    }
                    else
                    {
                        var latest_t = t.Last();
                        var latest_s1 = TxBDC_Math.Median(s1);
                        var transformed_s1 = ((latest_s1 - Baseline_1) / TICKS_PER_DEGREE);

                        CurrentActualValue = transformed_s1;
                    }
                }
                catch (Exception e)
                {
                    //empty
                }
            }
        }

        public override void SetupFile(DateTime build_date, string version_name, string version_code, 
            string game, string exercise, string tablet_id, 
            string subject_id, bool from_prescription,
            VNSAlgorithmParameters vns_algorithm_parameters)
        {
            //Execute the base "SetupFile" method.
            base.SetupFile(build_date, version_name, version_code, game, exercise, 
                tablet_id, subject_id, from_prescription, vns_algorithm_parameters);

            //Now save some information specific to the pinch devices
            Exercise_SaveData.SaveRePlayRangeOfMotionCalibrationData(DataSaver, Baseline_1);
        }

        #endregion
    }
}