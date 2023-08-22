using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_Rotate : FitMiExerciseBase_Twist
    {
        #region Constructor

        public FitMiExercise_Rotate(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 200;
            Instruction = "Rotate the puck back and forth";
        }

        #endregion
    }
}