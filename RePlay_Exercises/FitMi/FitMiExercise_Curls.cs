using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_Curls : FitMiExerciseBase_Arm
    {
        #region Constructor

        public FitMiExercise_Curls(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 150;
            Instruction = "Curl your arm";
            SinglePolarity = false;
        }

        #endregion
    }
}