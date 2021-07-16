using Android.App;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_PuckRoll : FitMiExerciseBase_Arm
    {
        #region Constructor

        public FitMiExercise_PuckRoll (Activity a, double gain)
            : base(a, gain)
        {
            Instruction = "Roll the puck back and forth";
        }

        #endregion
    }
}

