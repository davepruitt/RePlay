using Android.App;
using RePlay_Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RePlay_Exercises.FitMi
{
    public abstract class FitMiExerciseBase_Arm : FitMiExerciseBase
    {
        #region Private data members

        protected bool reset_exercise_flag = false;
        protected double bg_prev_theta = 0;
        protected double bg_delta_theta = 0;
        protected double bg_final_theta = 0;
        protected double bg_baseline_theta = 0;

        #endregion

        #region Constructor

        public FitMiExerciseBase_Arm(Activity a, double gain)
            : base(a, gain)
        {
            //Repetitions Mode stuff
            ConvertSignalToVelocity = false;
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

            double new_x = FitMi_Controller.PuckPack0.GetXAngle();
            double new_y = FitMi_Controller.PuckPack0.GetYAngle();
            double new_z = FitMi_Controller.PuckPack0.GetZAngle();
            double new_theta = 0;
            if (Math.Abs(new_z) < 75)
            {
                new_theta = CartestianToPolar(new_x, new_y);
            }

            if (reset_exercise_flag)
            {
                bg_baseline_theta = new_theta;
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

            //Set the values that external callers will see
            CurrentActualValue = -bg_final_theta;
        }

        #endregion
    }
}