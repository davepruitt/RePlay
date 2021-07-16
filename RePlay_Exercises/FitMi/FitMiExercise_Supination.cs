using Android.App;
using RePlay_Common;
using System.Collections.Generic;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_Supination : FitMiExerciseBase_FlipStyle
    {
        #region Protected data members

        protected List<double> baseline_collection = new List<double>();
        private int baseline_collection_max_elements = 5000;

        protected double initial_value = double.NaN;
        protected bool reset_exercise_flag = false;

        #endregion

        #region Constructor

        public FitMiExercise_Supination(Activity a, double gain)
            : base(a, gain)
        {
            Instruction = "Twist your wrist back and forth";

            //Repetitions Mode
            ConvertSignalToVelocity = true;
            ReturnThreshold = 0.01;
            SinglePolarity = false;
        }

        #endregion
    }
}