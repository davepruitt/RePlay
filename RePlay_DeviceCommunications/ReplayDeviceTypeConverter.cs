using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_DeviceCommunications
{
    /// <summary>
    /// Converts numeric values to a Replay device type
    /// </summary>
    public class ReplayDeviceTypeConverter
    {
        private enum ReplayControllerDeviceTypeIdentifier
        {
            Unknown = 0,
            KnobISO = 1,
            HandleISO = 2,
            WristISO = 3,
            Pinch = 4,
            LeftPinch = 5,
            KnobROM = 6,
            HandleROM = 7,
            WristROM = 8
        }

        public static bool IsIsometricModule (ReplayDeviceType d)
        {
            switch (d)
            {
                case ReplayDeviceType.Handle:
                case ReplayDeviceType.Knob:
                case ReplayDeviceType.Wrist:
                    return false;
                case ReplayDeviceType.Handle_Isometric:
                case ReplayDeviceType.Knob_Isometric:
                case ReplayDeviceType.Wrist_Isometric:
                case ReplayDeviceType.Pinch:
                case ReplayDeviceType.Pinch_Left:
                    return true;
                default:
                    return false;
            }
        }

        public static int ConvertEnumeratedTypeToBoardValue(ReplayDeviceType id)
        {
            switch (id)
            {
                case ReplayDeviceType.Handle:
                    return (int)ReplayControllerDeviceTypeIdentifier.HandleROM;
                case ReplayDeviceType.Handle_Isometric:
                    return (int)ReplayControllerDeviceTypeIdentifier.HandleISO;
                case ReplayDeviceType.Knob:
                    return (int)ReplayControllerDeviceTypeIdentifier.KnobROM;
                case ReplayDeviceType.Knob_Isometric:
                    return (int)ReplayControllerDeviceTypeIdentifier.KnobISO;
                case ReplayDeviceType.Pinch:
                    return (int)ReplayControllerDeviceTypeIdentifier.Pinch;
                case ReplayDeviceType.Pinch_Left:
                    return (int)ReplayControllerDeviceTypeIdentifier.LeftPinch;
                case ReplayDeviceType.Wrist:
                    return (int)ReplayControllerDeviceTypeIdentifier.WristROM;
                case ReplayDeviceType.Wrist_Isometric:
                    return (int)ReplayControllerDeviceTypeIdentifier.WristISO;
            }

            return 0;
        }

        /// <summary>
        /// Converts a numeric value taken from the microcontroller board to a device type
        /// </summary>
        public static ReplayDeviceType ConvertBoardValueToEnumeratedType(int board_value)
        {
            ReplayControllerDeviceTypeIdentifier id = (ReplayControllerDeviceTypeIdentifier)board_value;
            switch (id)
            {
                case ReplayControllerDeviceTypeIdentifier.HandleISO:
                    return ReplayDeviceType.Handle_Isometric;
                case ReplayControllerDeviceTypeIdentifier.HandleROM:
                    return ReplayDeviceType.Handle;
                case ReplayControllerDeviceTypeIdentifier.KnobISO:
                    return ReplayDeviceType.Knob_Isometric;
                case ReplayControllerDeviceTypeIdentifier.KnobROM:
                    return ReplayDeviceType.Knob;
                case ReplayControllerDeviceTypeIdentifier.Pinch:
                    return ReplayDeviceType.Pinch;
                case ReplayControllerDeviceTypeIdentifier.LeftPinch:
                    return ReplayDeviceType.Pinch_Left;
                case ReplayControllerDeviceTypeIdentifier.WristISO:
                    return ReplayDeviceType.Wrist_Isometric;
                case ReplayControllerDeviceTypeIdentifier.WristROM:
                    return ReplayDeviceType.Wrist;
                default:
                    return ReplayDeviceType.Unknown;
            }
        }

        /// <summary>
        /// Converts a string description of a device type to its corresponding enumerated type value
        /// </summary>
        public static ReplayDeviceType ConvertDescriptionToDeviceType(string description)
        {
            var type = typeof(ReplayDeviceType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (ReplayDeviceType)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (ReplayDeviceType)field.GetValue(null);
                }
            }

            return ReplayDeviceType.Unknown;
        }

        /// <summary>
        /// Converts a device type to a string description
        /// </summary>
        public static string ConvertDeviceTypeToDescription(ReplayDeviceType device_type)
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
    }
}
