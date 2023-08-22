using Android.App;
using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_Flyout : FitMiExerciseBase_Twist
    {
        #region Constructor

        public FitMiExercise_Flyout(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 120;
            Instruction = "Rotate your arm in and out";
            SinglePolarity = false;
        }

        #endregion
    }
}