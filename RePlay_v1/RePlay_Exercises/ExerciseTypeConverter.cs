using RePlay_DeviceCommunications;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Exercises
{
    /// <summary>
    /// Converts numeric values to a Replay device type
    /// </summary>
    public class ExerciseTypeConverter
    {
        public static ReplayDeviceType ConvertExerciseTypeToReCheckModuleType(ExerciseType t)
        {
            switch (t)
            {
                case ExerciseType.RePlay_RangeOfMotion_Handle:
                    return ReplayDeviceType.Handle;
                case ExerciseType.RePlay_RangeOfMotion_Knob:
                    return ReplayDeviceType.Knob;
                case ExerciseType.RePlay_RangeOfMotion_Wrist:
                    return ReplayDeviceType.Wrist;
                case ExerciseType.RePlay_Isometric_Handle:
                    return ReplayDeviceType.Handle_Isometric;
                case ExerciseType.RePlay_Isometric_Knob:
                    return ReplayDeviceType.Knob_Isometric;
                case ExerciseType.RePlay_Isometric_Wrist:
                    return ReplayDeviceType.Wrist_Isometric;
                case ExerciseType.RePlay_Isometric_Pinch:
                case ExerciseType.RePlay_Isometric_Pinch_Flexion:
                case ExerciseType.RePlay_Isometric_Pinch_Extension:
                    return ReplayDeviceType.Pinch;
                case ExerciseType.RePlay_Isometric_PinchLeft:
                case ExerciseType.RePlay_Isometric_Pinch_Left_Flexion:
                case ExerciseType.RePlay_Isometric_Pinch_Left_Extension:
                    return ReplayDeviceType.Pinch_Left;
                default:
                    return ReplayDeviceType.Unknown;
            }
        }

        public static ExerciseType ConvertReplayDeviceTypeToExerciseType(ReplayDeviceType t)
        {
            switch (t)
            {
                case ReplayDeviceType.Handle:
                    return ExerciseType.RePlay_RangeOfMotion_Handle;
                case ReplayDeviceType.Handle_Isometric:
                    return ExerciseType.RePlay_Isometric_Handle;
                case ReplayDeviceType.Knob:
                    return ExerciseType.RePlay_RangeOfMotion_Knob;
                case ReplayDeviceType.Knob_Isometric:
                    return ExerciseType.RePlay_Isometric_Knob;
                case ReplayDeviceType.Pinch:
                    return ExerciseType.RePlay_Isometric_Pinch;
                case ReplayDeviceType.Pinch_Left:
                    return ExerciseType.RePlay_Isometric_PinchLeft;
                case ReplayDeviceType.Wrist:
                    return ExerciseType.RePlay_RangeOfMotion_Wrist;
                case ReplayDeviceType.Wrist_Isometric:
                    return ExerciseType.RePlay_Isometric_Wrist;
            }

            return ExerciseType.Unknown;
        }

        public static ExerciseDeviceType GetExerciseDeviceType(ExerciseType t)
        {
            switch (t)
            {
                //Arm exercises
                case ExerciseType.FitMi_Touches:
                case ExerciseType.FitMi_ReachAcross:
                case ExerciseType.FitMi_Clapping:
                case ExerciseType.FitMi_ReachOut:
                case ExerciseType.FitMi_ReachDiagonal:
                case ExerciseType.FitMi_Supination:
                case ExerciseType.FitMi_Curls:
                case ExerciseType.FitMi_ShoulderExtension:
                case ExerciseType.FitMi_ShoulderAbduction:
                case ExerciseType.FitMi_Flyout:

                //Hand exercises
                case ExerciseType.FitMi_WristFlexion:
                case ExerciseType.FitMi_WristDeviation:
                case ExerciseType.FitMi_Grip:
                case ExerciseType.FitMi_Rotate:
                case ExerciseType.FitMi_KeyPinch:
                case ExerciseType.FitMi_FingerTap:
                case ExerciseType.FitMi_ThumbOpposition:
                case ExerciseType.FitMi_FingerTwists:
                case ExerciseType.FitMi_Rolling:
                case ExerciseType.FitMi_Flipping:
                case ExerciseType.FitMiCustom_Lift:
                case ExerciseType.FitMiCustom_Movement:
                case ExerciseType.FitMiCustom_MovementBidirectional:

                    return ExerciseDeviceType.FitMi;

                //RePlay exercises
                case ExerciseType.RePlay_Isometric_Handle:
                case ExerciseType.RePlay_Isometric_Knob:
                case ExerciseType.RePlay_Isometric_Wrist:
                case ExerciseType.RePlay_Isometric_Pinch:
                case ExerciseType.RePlay_Isometric_PinchLeft:
                case ExerciseType.RePlay_RangeOfMotion_Handle:
                case ExerciseType.RePlay_RangeOfMotion_Knob:
                case ExerciseType.RePlay_RangeOfMotion_Wrist:
                case ExerciseType.RePlay_Isometric_Pinch_Flexion:
                case ExerciseType.RePlay_Isometric_Pinch_Extension:
                case ExerciseType.RePlay_Isometric_Pinch_Left_Flexion:
                case ExerciseType.RePlay_Isometric_Pinch_Left_Extension:

                    return ExerciseDeviceType.ReCheck;

                case ExerciseType.Retrieve_Find:
                    return ExerciseDeviceType.Box;

                case ExerciseType.Touch:
                    return ExerciseDeviceType.Touchscreen;

                case ExerciseType.Keyboard_Typing:
                case ExerciseType.Keyboard_Typing_LeftHanded:
                case ExerciseType.Keyboard_Typing_RightHanded:
                    return ExerciseDeviceType.Keyboard;
            }

            return ExerciseDeviceType.Unknown;
        }

        /// <summary>
        /// Converts a string description to an exercise type
        /// </summary>
        public static ExerciseType ConvertDescriptionToExerciseType(string description)
        {
            var type = typeof(ExerciseType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (ExerciseType)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (ExerciseType)field.GetValue(null);
                }
            }

            return ExerciseType.Unknown;
        }

        /// <summary>
        /// Converts an exercise type to a string description
        /// </summary>
        public static string ConvertExerciseTypeToDescription(ExerciseType device_type)
        {
            FieldInfo fi = device_type.GetType().GetField(device_type.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return device_type.ToString();
        }

        /// <summary>
        /// Converts an enum member string to an exercise type
        /// </summary>
        public static ExerciseType ConvertEnumMemberStringToExerciseType(string enum_member_string)
        {
            var type = typeof(ExerciseType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(EnumMemberAttribute)) as EnumMemberAttribute;
                if (attribute != null)
                {
                    if (attribute.Value == enum_member_string)
                        return (ExerciseType)field.GetValue(null);
                }
                else
                {
                    if (field.Name == enum_member_string)
                        return (ExerciseType)field.GetValue(null);
                }
            }

            return ExerciseType.Unknown;
        }

        /// <summary>
        /// Converts an exercise type to an enum member string
        /// </summary>
        public static string ConvertExerciseTypeToEnumMemberString(ExerciseType device_type)
        {
            FieldInfo fi = device_type.GetType().GetField(device_type.ToString());

            EnumMemberAttribute[] attributes =
                (EnumMemberAttribute[])fi.GetCustomAttributes(
                typeof(EnumMemberAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Value;
            else
                return device_type.ToString();
        }

        public static double ConvertExerciseTypeToDefaultNoiseFloor(ExerciseType exercise_type)
        {
            FieldInfo fi = exercise_type.GetType().GetField(exercise_type.ToString());

            DefaultNoiseFloorAttribute[] attributes =
                (DefaultNoiseFloorAttribute[])fi.GetCustomAttributes(
                typeof(DefaultNoiseFloorAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].NoiseFloor;
            else
                return double.NaN;
        }

        public static bool IsMultiPuck(ExerciseType t)
        {
            switch (t)
            {
                // 2 - puck exercises
                case ExerciseType.FitMi_ReachAcross:
                case ExerciseType.FitMi_ReachOut:
                case ExerciseType.FitMi_ReachDiagonal:
                    return true;

                // 1 - puck exercises
                case ExerciseType.FitMi_Touches:
                case ExerciseType.FitMi_Grip:
                case ExerciseType.FitMi_Clapping:
                case ExerciseType.FitMi_Supination:
                case ExerciseType.FitMi_Curls:
                case ExerciseType.FitMi_ShoulderExtension:
                case ExerciseType.FitMi_ShoulderAbduction:
                case ExerciseType.FitMi_Flyout:
                case ExerciseType.FitMi_WristFlexion:
                case ExerciseType.FitMi_WristDeviation:
                case ExerciseType.FitMi_Rotate:
                case ExerciseType.FitMi_KeyPinch:
                case ExerciseType.FitMi_FingerTap:
                case ExerciseType.FitMi_ThumbOpposition:
                case ExerciseType.FitMi_FingerTwists:
                case ExerciseType.FitMi_Rolling:
                case ExerciseType.FitMi_Flipping:
                    return false;
            }

            return false;
        }
    }
}
