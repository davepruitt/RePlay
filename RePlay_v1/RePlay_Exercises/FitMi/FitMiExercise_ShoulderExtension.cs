using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_ShoulderExtension : FitMiExerciseBase_Arm
    {
        #region Constructor

        public FitMiExercise_ShoulderExtension(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 150;
            Instruction = "Reach your arm up and out";
            SinglePolarity = false;
        }

        #endregion
    }
}