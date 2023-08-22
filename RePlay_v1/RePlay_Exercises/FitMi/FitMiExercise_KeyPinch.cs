using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_KeyPinch : FitMiExerciseBase_Loadcell
    {
        #region Constructor

        public FitMiExercise_KeyPinch(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 50;
            Instruction = "Grip and release";
        }

        #endregion
    }
}