using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_ReachOut : FitMiExerciseBase_LoadcellAlternating
    {
        #region Constructor

        public FitMiExercise_ReachOut(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 200;
            Instruction = "Reach out and back";
        }

        #endregion
    }
}