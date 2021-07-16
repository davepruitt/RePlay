using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_Grip : FitMiExerciseBase_Loadcell
    {
        #region Constructor

        public FitMiExercise_Grip(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 200;
            Instruction = "Grip and release";
        }

        #endregion
    }
}