using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_ShoulderAbduction : FitMiExerciseBase_Arm
    {
        #region Constructor

        public FitMiExercise_ShoulderAbduction(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 150;
            Instruction = "Lift your arm out to the side";
            SinglePolarity = false;
        }

        #endregion
    }
}