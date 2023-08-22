using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_WristFlexion : FitMiExerciseBase_Twist
    {
        #region Constructor

        public FitMiExercise_WristFlexion(Activity a, double gain)
            : base(a, gain)
        {
            MaximumNormalizedRange = 75;
            Instruction = "Flex your wrists back and forth";
        }

        #endregion
    }
}