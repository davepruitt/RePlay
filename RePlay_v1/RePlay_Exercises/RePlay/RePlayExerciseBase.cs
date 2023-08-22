using System;
using Android.App;
using RePlay_DeviceCommunications;

namespace RePlay_Exercises.RePlay
{
    public abstract class RePlayExerciseBase : ExerciseBase
    {
        #region Protected members

        protected ReplayMicrocontroller ReplayController = null;

        #endregion

        public RePlayExerciseBase(ReplayMicrocontroller c, Activity activity, double gain)
            : base(gain)
        {
            CurrentActivity = activity;
            ReplayController = c;
            DeviceClass = ExerciseDeviceType.ReCheck;
            SinglePolarity = false;
            ReturnThreshold = 20;
            ForceAlternation = true;
        }

        public RePlayExerciseBase(Activity activity, double gain)
            : base(gain)
        {
            CurrentActivity = activity;
            ReplayController = new ReplayMicrocontroller(activity);
            DeviceClass = ExerciseDeviceType.ReCheck;
            SinglePolarity = false;
            ReturnThreshold = 20;
            ForceAlternation = true;
        }

        #region Methods

        public override bool SetupDevice()
        {
            ReplayController.Open();
            ReplayController.EnableStreaming(true);

            return ReplayController.IsSetupComplete();
        }

        public void EnableStreaming (bool enable)
        {
            ReplayController.EnableStreaming(enable);
        }

        public override void SaveExerciseData()
        {
            Exercise_SaveData.SaveRePlayExerciseData(DataSaver, CurrentActualValue);
        }

        public override void Close()
        {
            ReplayController.Close();
        }

        public override void CloseFile()
        {
            Exercise_SaveData.CloseFile(DataSaver);
        }

        public bool IsReplayDevicePresent ()
        {
            if (ReplayController != null)
            {
                return (ReplayController.CurrentDeviceType != ReplayDeviceType.Unknown);
            }
            else
            {
                return false;
            }
        }

        public ReplayMicrocontroller GetController ()
        {
            return ReplayController;
        }

        public virtual bool DoesDeviceMatch ()
        {
            return false;
        }

        public static RePlayExerciseBase InstantiateCorrectReplayExerciseClass(ReplayMicrocontroller replayController, ExerciseType t, Activity a, double gain)
        {
            switch (t)
            {
                case ExerciseType.RePlay_Isometric_Knob:
                    return new RePlayExercise_IsometricKnob(replayController, a, gain);
                case ExerciseType.RePlay_Isometric_Handle:
                    return new RePlayExercise_IsometricHandle(replayController, a, gain);
                case ExerciseType.RePlay_Isometric_Pinch:
                    return new RePlayExercise_IsometricPinch(replayController, a, gain);
                case ExerciseType.RePlay_Isometric_PinchLeft:
                    return new RePlayExercise_IsometricPinchLeft(replayController, a, gain);
                case ExerciseType.RePlay_Isometric_Wrist:
                    return new RePlayExercise_IsometricWrist(replayController, a, gain);
                case ExerciseType.RePlay_RangeOfMotion_Handle:
                    return new RePlayExercise_RangeOfMotion_Handle(replayController, a, gain);
                case ExerciseType.RePlay_RangeOfMotion_Knob:
                    return new RePlayExercise_RangeOfMotion_Knob(replayController, a, gain);
                case ExerciseType.RePlay_RangeOfMotion_Wrist:
                    return new RePlayExercise_RangeOfMotion_Wrist(replayController, a, gain);
                default:
                    return null;
            }
        }

        #endregion
    }
}