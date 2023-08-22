using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_FingerTwists : FitMiExerciseBase_Twist
    {
        #region Constructor

        public FitMiExercise_FingerTwists(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 75;
            Instruction = "Twist the puck back and forth";
        }

        #endregion
    }
}