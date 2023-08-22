using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_FingerTap : FitMiExerciseBase_Loadcell
    {
        #region Constructor

        public FitMiExercise_FingerTap(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 30;
            Instruction = "Tap the puck";
        }

        #endregion
    }
}