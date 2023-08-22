using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_Clapping : FitMiExerciseBase_Loadcell
    {
        #region Constructor

        public FitMiExercise_Clapping(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 30;
            Instruction = "Clap your hands together";
        }

        #endregion
    }
}