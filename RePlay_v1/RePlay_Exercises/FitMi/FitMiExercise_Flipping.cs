using Android.App;
using RePlay_Common;
using System;
using System.Collections.Generic;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_Flipping : FitMiExerciseBase_FlipStyle
    {
        #region Protected data members

        protected List<double> baseline_collection = new List<double>();
        private int baseline_collection_max_elements = 5000;

        protected double initial_value = double.NaN;
        protected bool reset_exercise_flag = false;

        #endregion

        #region Constructor

        public FitMiExercise_Flipping(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 270;
            Instruction = "Flip the puck back and forth";

            //Repetitions Mode
            ConvertSignalToVelocity = true;
            ReturnThreshold = 0.01;
            SinglePolarity = false;
        }

        #endregion
    }
}
