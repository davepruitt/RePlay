using Android.App;
using RePlay_DeviceCommunications;

namespace RePlay_Exercises.RePlay
{
    public class RePlayExercise_IsometricKnob : RePlayExercise_Isometric
    {
        #region Constructor

        public RePlayExercise_IsometricKnob(ReplayMicrocontroller c, Activity a, double gain)
            : base(c, a, gain)
        {
            //empty
        }

        public RePlayExercise_IsometricKnob(Activity a, double gain)
            : base(a, gain)
        {
            //empty
        }

        #endregion

        public override bool DoesDeviceMatch()
        {
            if (ReplayController != null)
            {
                return (ReplayController.CurrentDeviceType == ReplayDeviceType.Knob_Isometric);
            }
            else
            {
                return false;
            }
        }
    }
}