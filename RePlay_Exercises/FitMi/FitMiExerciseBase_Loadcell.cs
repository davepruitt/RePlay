using System;
using System.Collections.Generic;
using Android.App;
using RePlay_Common;

namespace RePlay_Exercises.FitMi
{
    public abstract class FitMiExerciseBase_Loadcell : FitMiExerciseBase
    {
        #region Private data members

        protected List<double> baseline_collection_1 = new List<double>();
        protected List<double> baseline_collection_2 = new List<double>();
        private int baseline_collection_max_elements = 5000;
        private bool reset_exercise_flag = false;

        public int Force_1 { get; protected set; } = 0;
        public int Force_2 { get; protected set; } = 0;

        protected double baseline_force_value_1 = 500.0;
        protected double baseline_force_value_2 = 500.0;

        #endregion

        #region Constructor

        public FitMiExerciseBase_Loadcell(Activity a, double gain)
            : base(a, gain)
        {
            //Repetitions Mode stuff
            ConvertSignalToVelocity = false;
            SinglePolarity = true;
        }

        #endregion

        #region Overrides

        public override List<double> RetrieveBaselineData()
        {
            return new List<double>() { baseline_force_value_1, baseline_force_value_2 };
        }

        public override void EnableBaselineDataCollection(bool enable)
        {
            reset_exercise_flag = enable;
            baseline_collection_1.Clear();
            baseline_collection_2.Clear();
        }

        public override bool ResetExercise(bool long_reset = false)
        {
            if (long_reset)
            {
                if (baseline_collection_1.Count > 0 && baseline_collection_2.Count > 0)
                {
                    baseline_force_value_1 = TxBDC_Math.CalcuateWeightedMeanUsingIndicesAsWeights(baseline_collection_1);
                    baseline_force_value_2 = TxBDC_Math.CalcuateWeightedMeanUsingIndicesAsWeights(baseline_collection_2);
                }
            }
            else
            {
                try
                {
                    FitMi_Controller.CheckForNewPuckData();
                    baseline_force_value_1 = FitMi_Controller.PuckPack0.Loadcell;
                    baseline_force_value_2 = FitMi_Controller.PuckPack1.Loadcell;
                }
                catch (Exception)
                {
                    //empty
                }
            }

            if (SinglePolarity)
            {
                return baseline_force_value_1 > 1;
            }
            else
            {
                return (baseline_force_value_1 > 1 && baseline_force_value_2 > 1);
            }
        }

        public override void Update()
        {
            base.Update();

            //Grab the current force value on the loadcell of the puck
            Force_1 = FitMi_Controller.PuckPack0.Loadcell;
            Force_2 = FitMi_Controller.PuckPack1.Loadcell;

            if (reset_exercise_flag)
            {
                baseline_collection_1.Add(Force_1);
                baseline_collection_2.Add(Force_2);
            }
            else
            {
                var Force1Actual = (Convert.ToDouble(Force_1) - baseline_force_value_1);
                var Force2Actual = -(Convert.ToDouble(Force_2) - baseline_force_value_2);

                if (Force1Actual >= 2)
                {
                    CurrentActualValue = Force1Actual;
                }
                else if (Force2Actual <= -2)
                {
                    CurrentActualValue = Force2Actual;
                }
                else
                {
                    CurrentActualValue = 0;
                }
            }
        }

        #endregion
    }
}