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
using RePlay_Common;
using RePlay_VNS_Triggering;

namespace RePlay_Exercises.RePlay
{
    public abstract class RePlayExercise_Isometric : RePlayExerciseBase
    {
        #region Protected variables

        protected List<double> baseline_collection_1 = new List<double>();
        protected List<double> baseline_collection_2 = new List<double>();
        protected int baseline_collection_max_elements = 5000;
        protected bool reset_exercise_flag = false;

        public double Baseline_1 { get; protected set; } = 0;
        public double Baseline_2 { get; protected set; } = 0;
        public double Slope_1 { get; protected set; } = 1;
        public double Slope_2 { get; protected set; } = 1;

        #endregion

        public RePlayExercise_Isometric(ReplayMicrocontroller c, Activity a, double gain)
            : base(c, a, gain)
        {
            MaximumNormalizedRange = 30;
            ReturnThreshold = 20;
            DebuggingProperties = new Dictionary<string, double>()
            {
                { "Device", 0 },
                { "L1_Raw", 0 },
                { "L1_Slope", 0 },
                { "L1_Offset", 0 },
                { "L2_Raw", 0 },
                { "L2_Slope", 0 },
                { "L2_Offset", 0 },
                { "Lx_Gain", 0 }
            };
        }

        public RePlayExercise_Isometric (Activity a, double gain)
            : base (a, gain)
        {
            MaximumNormalizedRange = 30;
            ReturnThreshold = 20;
            DebuggingProperties = new Dictionary<string, double>()
            {
                { "L1_Raw", 0 },
                { "L1_Slope", 0 },
                { "L1_Offset", 0 },
                { "L2_Raw", 0 },
                { "L2_Slope", 0 },
                { "L2_Offset", 0 },
                { "Lx_Gain", 0 }
            };
        }

        #region Overrides

        public override List<double> RetrieveBaselineData()
        {
            return new List<double>() { Baseline_1, Baseline_2 };
        }

        public override void EnableBaselineDataCollection(bool enable)
        {
            reset_exercise_flag = enable;
            baseline_collection_1.Clear();
            baseline_collection_2.Clear();
        }

        public override bool ResetExercise(bool long_reset = false)
        {
            bool is_slope_good = false;

            if (long_reset)
            {
                if (baseline_collection_1.Count > 0 && baseline_collection_2.Count > 0)
                {
                    Baseline_1 = TxBDC_Math.CalcuateWeightedMeanUsingIndicesAsWeights(baseline_collection_1);
                    Baseline_2 = TxBDC_Math.CalcuateWeightedMeanUsingIndicesAsWeights(baseline_collection_2);
                    Slope_1 = ReplayController.Loadcell_1_Slope;
                    Slope_2 = ReplayController.Loadcell_2_Slope;

                    is_slope_good = (Slope_1 > 1);
                }
            }
            else
            {
                (var t, var s1, var s2) = ReplayController.FetchLatestData(true, 100);

                if (t.Count > 0)
                {
                    var latest_s1 = TxBDC_Math.Median(s1);
                    var latest_s2 = TxBDC_Math.Median(s2);
                    Baseline_1 = latest_s1;
                    Baseline_2 = latest_s2;
                    Slope_1 = ReplayController.Loadcell_1_Slope;
                    Slope_2 = ReplayController.Loadcell_2_Slope;

                    is_slope_good = (Slope_1 > 1);
                }
            }

            //Save the new baseline and slope data in the data file before continuing
            Exercise_SaveData.SaveRePlayIsometricCalibrationData(DataSaver, Baseline_1, Baseline_2, Slope_1, Slope_2);

            //Return
            return is_slope_good;
        }
        
        public override void Update()
        {
            (var t, var s1, var s2) = ReplayController.FetchLatestData();

            if ((t != null) && (s1 != null) && (s2 != null) &&
                (t.Count > 0) && (s1.Count > 0) && (s2.Count > 0) &&
                (t.Count == s1.Count) && (s1.Count == s2.Count))
            {
                try
                {
                    if (reset_exercise_flag)
                    {
                        baseline_collection_1.AddRange(s1.Select(x => Convert.ToDouble(x)));
                        baseline_collection_2.AddRange(s2.Select(x => Convert.ToDouble(x)));
                    }
                    else
                    {
                        var latest_t = t.Last();
                        var latest_s1 = TxBDC_Math.Median(s1);
                        var transformed_s1 = ((latest_s1 - Baseline_1) / Slope_1);

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
            string game, string exercise, string tablet_id, string subject_id, bool from_prescription,
            VNSAlgorithmParameters vns_algorithm_parameters)
        {
            //Execute the base "SetupFile" method.
            base.SetupFile(build_date, version_name, version_code, game, exercise, 
                tablet_id, subject_id, from_prescription, vns_algorithm_parameters);

            //Now save some information specific to the pinch devices
            Exercise_SaveData.SaveRePlayIsometricCalibrationData(DataSaver, Baseline_1, Baseline_2, Slope_1, Slope_2);
        }

        #endregion
    }
}