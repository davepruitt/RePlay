using System;
using Android.App;
using FitMiAndroid;

namespace RePlay_Exercises.FitMi
{
    /// <summary>
    /// Base class for FitMi exercises
    /// </summary>
    public abstract class FitMiExerciseBase : ExerciseBase
    {
        #region IRepetitionsModeExercise Overrides

        public override bool ConvertSignalToVelocity
        {
            get => base.ConvertSignalToVelocity;
            set
            {
                base.ConvertSignalToVelocity = value;

                if (base.ConvertSignalToVelocity)
                {
                    ReturnThreshold = 0.02;
                    HitThreshold = 0.03;
                }
                else
                {
                    ReturnThreshold = 0.05;
                    HitThreshold = 0.05;
                }
            }
        }

        #endregion

        #region Protected members

        protected HIDPuckDongle FitMi_Controller = null;

        #endregion

        #region Constructor

        public FitMiExerciseBase(Activity a, double gain)
            : base(gain)
        {
            CurrentActivity = a;
            DeviceClass = ExerciseDeviceType.FitMi;

            //Repetitions Mode stuff
            MinimumTrialDuration = TimeSpan.FromSeconds(0.2);
        }

        #endregion

        #region Properties

        public HIDPuckDongle PuckDongle
        {
            get
            {
                return FitMi_Controller;
            }
        }

        #endregion

        #region Methods

        public override bool SetupDevice()
        {
            try
            {
                FitMi_Controller = new HIDPuckDongle(CurrentActivity);
                FitMi_Controller.Open();
            }
            catch (Exception e)
            {
                //empty
            }
            
            return FitMi_Controller.IsOpened();
        }

        public override bool ResetExercise (bool long_reset = false)
        {
            return true;
        }

        public override void Update ()
        {
            if (FitMi_Controller != null)
            {
                if (FitMi_Controller.IsOpen)
                {
                    FitMi_Controller.CheckForNewPuckData();
                }
                else
                {
                    throw new Exception("Error while attempting to retrieve data from the FitMi pucks!!!");
                }
            }
        }

        public override void SaveExerciseData()
        {
            Exercise_SaveData.SaveCurrentPuckData(DataSaver, FitMi_Controller);
        }

        public override void Close()
        {
            FitMi_Controller.Stop();
        }

        public override void CloseFile()
        {
            Exercise_SaveData.CloseFile(DataSaver);
        }

        #endregion
    }
}