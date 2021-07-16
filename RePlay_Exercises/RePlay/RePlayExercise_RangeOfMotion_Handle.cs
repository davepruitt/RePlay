using Android.App;
using RePlay_DeviceCommunications;

namespace RePlay_Exercises.RePlay
{
    public class RePlayExercise_RangeOfMotion_Handle : RePlayExercise_RangeOfMotion
    {
        #region Constructor

        public RePlayExercise_RangeOfMotion_Handle (ReplayMicrocontroller c, Activity a, double gain)
            : base(c, a, gain)
        {
            //empty
        }

        public RePlayExercise_RangeOfMotion_Handle (Activity a, double gain)
            : base(a, gain)
        {
            //empty
        }

        #endregion

        public override bool DoesDeviceMatch()
        {
            if (ReplayController != null)
            {
                return (ReplayController.CurrentDeviceType == ReplayDeviceType.Handle);
            }
            else
            {
                return false;
            }
        }
    }
}