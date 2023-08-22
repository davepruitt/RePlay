using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_ReachAcross : FitMiExerciseBase_LoadcellAlternating
    {
        #region Constructor

        public FitMiExercise_ReachAcross(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 200;
            Instruction = "Reach back and forth";
        }

        #endregion
    }
}