using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_WristDeviation : FitMiExerciseBase_Arm
    {
        #region Constructor

        public FitMiExercise_WristDeviation(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 90;
            Instruction = "Flex your wrists up and down";
            NegateTransformedSignal = true;
        }

        #endregion
    }
}