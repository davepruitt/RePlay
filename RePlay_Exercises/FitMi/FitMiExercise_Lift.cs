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
    public class FitMiExercise_Lift : FitMiExerciseBase
    {
        #region Protected data members

        protected bool reset_exercise_flag = false;
        protected double bg_baseline_accx = 0;
        protected double bg_baseline_accy = 0;
        protected double bg_velocity = 0;

        #endregion

        #region Constructor

        public FitMiExercise_Lift(Activity a, double gain)
            : base(a, gain)
        {
            Instruction = "Lift the puck";

            //Repetitions Mode
            ConvertSignalToVelocity = false;
            SinglePolarity = true;
        }

        #endregion

        #region Overrides

        public override List<double> RetrieveBaselineData()
        {
            return new List<double>() { bg_baseline_accx, bg_baseline_accy };
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

            //Grab the actual angle of the puck
            double accx = FitMi_Controller.PuckPack0.Accelerometer[0];
            double accy = FitMi_Controller.PuckPack0.Accelerometer[1];
            if (reset_exercise_flag)
            {
                bg_baseline_accx = accx;
                bg_baseline_accy = accy;
                bg_velocity = 0;
                reset_exercise_flag = false;
            }

            double new_value = Math.Abs(accx - bg_baseline_accx) + Math.Abs(accy - bg_baseline_accy);
            bg_velocity += new_value;

            if (Math.Abs(new_value) < 4)
            {
                bg_velocity *= 0.9;
            }

            //Calculate the value normalized to the range (and the range is sensitivity dependent)
            CurrentActualValue = bg_velocity;
        }

        #endregion
    }
}