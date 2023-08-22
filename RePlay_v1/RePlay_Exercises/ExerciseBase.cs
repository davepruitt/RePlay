using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using RePlay_Exercises.FitMi;
using RePlay_Exercises.RePlay;
using RePlay_VNS_Triggering;

namespace RePlay_Exercises
{
    public abstract class ExerciseBase : IRepetitionsModeExercise
    {
        #region Constructor
        
        public ExerciseBase(double gain = 1.0)
        {
            Gain = gain;
        }

        #endregion

        #region IRepetitionsModeExercise Properties

        public virtual TimeSpan MinimumTrialDuration { get; set; } = TimeSpan.FromSeconds(0.1);

        public virtual double ReturnThreshold { get; set; } = 0.1;

        public virtual double HitThreshold { get; set; } = 1.0;

        public virtual bool SinglePolarity { get; set; } = false; // true = 1 signal; false = 2 signals

        public virtual bool ForceAlternation { get; set; } = false;

        public virtual string Instruction { get; set; } = "Start your workout";

        public virtual bool ConvertSignalToVelocity { get; set; } = false;

        #endregion

        #region Properties

        protected object DebuggingPropertiesLock = new object();

        protected Dictionary<string, double> DebuggingProperties { get; set; } = new Dictionary<string, double>();

        public ExerciseDeviceType DeviceClass { get; set; } = ExerciseDeviceType.Unknown;

        public double MaximumNormalizedRange { get; set; } = 1.0;

        public bool NegateTransformedSignal { get; set; } = false;

        /// <summary>
        /// Normalized value = TRANSFORMED value / exercise sensitivity
        /// </summary>
        public double CurrentNormalizedValue
        {
            get
            {
                return CurrentTransformedValue / StandardExerciseSensitivity;
            }
        }

        /// <summary>
        /// Transformed value = The ACTUAL value * GAIN * negation multiplier (1 or -1)
        /// </summary>
        public double CurrentTransformedValue
        {
            get
            {
                double result;
                if (Gain >= 0.0001 && Gain <= 10000)
                {
                    result = CurrentActualValue * Gain;
                }
                else
                {
                    result = CurrentActualValue;
                }

                if (NegateTransformedSignal)
                {
                    result = -result;
                }

                return result;
            }
        }

        /// <summary>
        /// The ACTUAL value calculated for this exercise
        /// </summary>
        public double CurrentActualValue { get; set; } = 0.0;
        
        public Activity CurrentActivity { get; set; }

        public BinaryWriter DataSaver { get; set; }

        public string SaveFileName { get; set; }

        public double Gain { get; set; } = 1.0;

        public static double StandardExerciseSensitivity { get; private set; } = 100.0;
        
        #endregion

        #region Protected Methods

        protected const double RadiansToDegrees = (180.0 / Math.PI);

        protected double CartestianToPolar(double x, double y)
        {
            //Given a cartesian coordinate, this returns an angle from 0 to 360
            double result = Math.Atan(y / x) * RadiansToDegrees;
            if (x < 0)
            {
                result += 180;
            }
            else if (y < 0)
            {
                result += 360;
            }

            return result;
        }

        #endregion

        #region Methods

        public Dictionary<string, double> GetDebuggingProperties ()
        {
            Dictionary<string, double> result = new Dictionary<string, double>();
            lock (DebuggingPropertiesLock)
            {
                foreach (var entry in DebuggingProperties)
                {
                    result.Add(entry.Key, entry.Value);
                }
            }

            return result;
        }

        public virtual List<double> RetrieveBaselineData ()
        {
            return new List<double>();
        }

        public virtual void EnableBaselineDataCollection (bool enable)
        {
            //empty
        }
        
        public virtual void SetupFile(DateTime build_date, 
            string version_name, 
            string version_code, 
            string game, 
            string exercise, 
            string tablet_id,
            string subject_id,
            bool from_prescription,
            VNSAlgorithmParameters vns_algorithm_parameters)
        {
            string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            SaveFileName = subject_id + "_" + game + "_" + current_date_time_stamp + ".txt";
            DataSaver = Exercise_SaveData.OpenFileForSaving(
                CurrentActivity, 
                SaveFileName, 
                build_date, 
                version_name, 
                version_code, 
                tablet_id, 
                subject_id, 
                game, 
                exercise, 
                StandardExerciseSensitivity, 
                Gain,
                StandardExerciseSensitivity,
                from_prescription,
                vns_algorithm_parameters);
        }

        public virtual bool SetupDevice()
        {
            return true;
        }

        public virtual bool ResetExercise(bool long_reset = false)
        {
            //This returns a true or false value indicating whether this function was successful at "resetting" the exercise.
            return true;
        }

        public virtual void Update()
        {
            //empty
        }
        
        public virtual void SaveExerciseData()
        {
            // empty
        }

        public virtual void Close()
        {
            // empty
        }

        public virtual void CloseFile()
        {
            // empty
        }

        public static ExerciseBase InstantiateCorrectExerciseClass(ExerciseType t, Activity a, double gain)
        {
            switch (t)
            {
                case ExerciseType.FitMi_Rolling:
                    return new FitMiExercise_PuckRoll(a, gain);
                case ExerciseType.FitMi_Touches:
                    return new FitMiExercise_PuckTouch(a, gain);
                case ExerciseType.FitMi_Clapping:
                    return new FitMiExercise_Clapping(a, gain);
                case ExerciseType.FitMi_Curls:
                    return new FitMiExercise_Curls(a, gain);
                case ExerciseType.FitMi_FingerTap:
                    return new FitMiExercise_FingerTap(a, gain);
                case ExerciseType.FitMi_FingerTwists:
                    return new FitMiExercise_FingerTwists(a, gain);
                case ExerciseType.FitMi_Flipping:
                    return new FitMiExercise_Flipping(a, gain);
                case ExerciseType.FitMi_Flyout:
                    return new FitMiExercise_Flyout(a, gain);
                case ExerciseType.FitMi_Grip:
                    return new FitMiExercise_Grip(a, gain);
                case ExerciseType.FitMi_KeyPinch:
                    return new FitMiExercise_KeyPinch(a, gain);
                case ExerciseType.FitMi_ReachAcross:
                    return new FitMiExercise_ReachAcross(a, gain);
                case ExerciseType.FitMi_ReachDiagonal:
                    return new FitMiExercise_ReachDiagonal(a, gain);
                case ExerciseType.FitMi_ReachOut:
                    return new FitMiExercise_ReachOut(a, gain);
                case ExerciseType.FitMi_Rotate:
                    return new FitMiExercise_Rotate(a, gain);
                case ExerciseType.FitMi_ShoulderAbduction:
                    return new FitMiExercise_ShoulderAbduction(a, gain);
                case ExerciseType.FitMi_ShoulderExtension:
                    return new FitMiExercise_ShoulderExtension(a, gain);
                case ExerciseType.FitMi_Supination:
                    return new FitMiExercise_Supination(a, gain);
                case ExerciseType.FitMi_ThumbOpposition:
                    return new FitMiExercise_ThumbOpposition(a, gain);
                case ExerciseType.FitMi_WristDeviation:
                    return new FitMiExercise_WristDeviation(a, gain);
                case ExerciseType.FitMi_WristFlexion:
                    return new FitMiExercise_WristFlexion(a, gain);
                case ExerciseType.FitMiCustom_Lift:
                    return new FitMiExercise_Lift(a, gain);
                case ExerciseType.FitMiCustom_Movement:
                    return new FitMiExerciseBase_Movement(a, gain);
                case ExerciseType.FitMiCustom_MovementBidirectional:
                    return new FitMiExercise_MovementBidirectional(a, gain);


                case ExerciseType.RePlay_RangeOfMotion_Handle:
                    return new RePlayExercise_RangeOfMotion_Handle(a, gain);
                case ExerciseType.RePlay_RangeOfMotion_Knob:
                    return new RePlayExercise_RangeOfMotion_Knob(a, gain);
                case ExerciseType.RePlay_RangeOfMotion_Wrist:
                    return new RePlayExercise_RangeOfMotion_Wrist(a, gain);

                case ExerciseType.RePlay_Isometric_Handle:
                    return new RePlayExercise_IsometricHandle(a, gain);
                case ExerciseType.RePlay_Isometric_Knob:
                    return new RePlayExercise_IsometricKnob(a, gain);
                case ExerciseType.RePlay_Isometric_Wrist:
                    return new RePlayExercise_IsometricWrist(a, gain);

                case ExerciseType.RePlay_Isometric_Pinch:
                    return new RePlayExercise_IsometricPinch(a, gain);
                case ExerciseType.RePlay_Isometric_Pinch_Flexion:
                    return new RePlayExercise_IsometricPinch_Flexion(a, gain);
                case ExerciseType.RePlay_Isometric_Pinch_Extension:
                    return new RePlayExercise_IsometricPinch_Extension(a, gain);

                case ExerciseType.RePlay_Isometric_PinchLeft:
                    return new RePlayExercise_IsometricPinchLeft(a, gain);
                case ExerciseType.RePlay_Isometric_Pinch_Left_Flexion:
                    return new RePlayExercise_IsometricPinchLeft_Flexion(a, gain);
                case ExerciseType.RePlay_Isometric_Pinch_Left_Extension:
                    return new RePlayExercise_IsometricPinchLeft_Extension(a, gain);

                default:
                    return new FitMiExercise_Unknown(a, gain);
            }
        }
        
        #endregion

    }
}