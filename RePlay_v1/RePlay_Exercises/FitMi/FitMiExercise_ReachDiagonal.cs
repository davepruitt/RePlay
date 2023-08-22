using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_ReachDiagonal : FitMiExerciseBase_LoadcellAlternating
    {
        #region Constructor

        public FitMiExercise_ReachDiagonal(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 200;
            Instruction = "Reach out and back";
        }

        #endregion
    }
}