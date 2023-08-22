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
using RePlay_Common;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExerciseBase_FlipStyle : FitMiExerciseBase
    {
        #region Private data members

        protected bool reset_exercise_flag = false;
        protected double bg_prev_theta = 0;
        protected double bg_delta_theta = 0;
        protected double bg_final_theta = 0;
        protected double bg_baseline_theta = 0;

        #endregion

        #region Constructor

        public FitMiExerciseBase_FlipStyle(Activity a, double gain)
            : base(a, gain)
        {
            //Repetitions Mode stuff
            ConvertSignalToVelocity = true;
            ReturnThreshold = 0.01;
            SinglePolarity = false;
        }

        #endregion

        #region Methods

        public override List<double> RetrieveBaselineData()
        {
            return new List<double>() { bg_baseline_theta };
        }

        public override void EnableBaselineDataCollection(bool enable)
        {
            //empty
        }

        public override bool ResetExercise(bool long_reset = false)
        {
            reset_exercise_flag = true;
            return true;
        }

        public override void Update()
        {
            base.Update();

            var raw_z_angle = FitMi_Controller.PuckPack0.GetZAngle();
            var new_theta = 90.0 - raw_z_angle;

            if (reset_exercise_flag)
            {
                bg_prev_theta = new_theta;
                bg_delta_theta = 0;
                bg_final_theta = 0;
                reset_exercise_flag = false;
            }
            else
            {
                bg_delta_theta = TxBDC_Math.SmallestAngleDifference(new_theta, bg_prev_theta);
            }

            bg_final_theta += bg_delta_theta;
            bg_prev_theta = new_theta;

            //Calculate the value normalized to the range (and the range is sensitivity dependent)
            CurrentActualValue = bg_final_theta;
        }

        #endregion
    }
}