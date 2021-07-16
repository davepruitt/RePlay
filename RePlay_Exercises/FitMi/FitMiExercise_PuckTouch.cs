using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_PuckTouch : FitMiExerciseBase_Loadcell
    {
        #region Constructor

        public FitMiExercise_PuckTouch(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 200;
            Instruction = "Press down on the puck";
        }

        #endregion
    }
}