using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_ThumbOpposition : FitMiExerciseBase_Loadcell
    {
        #region Constructor

        public FitMiExercise_ThumbOpposition(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 50;
            Instruction = "Extend your thumb and press the puck";
        }

        #endregion
    }
}