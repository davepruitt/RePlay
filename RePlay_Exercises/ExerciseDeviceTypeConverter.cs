using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace RePlay_Exercises
{
    public class ExerciseDeviceTypeConverter
    {
        /// <summary>
        /// Converts a string description to an exercise device type
        /// </summary>
        public static ExerciseDeviceType ConvertDescriptionToExerciseDeviceType(string description)
        {
            var type = typeof(ExerciseDeviceType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (ExerciseDeviceType)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (ExerciseDeviceType)field.GetValue(null);
                }
            }

            return ExerciseDeviceType.Unknown;
        }

        /// <summary>
        /// Converts an exercise device type to a string description
        /// </summary>
        public static string ConvertExerciseDeviceTypeToDescription(ExerciseDeviceType device_type)
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