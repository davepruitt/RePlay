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

namespace RePlay_Activity_TyperShark.Main
{
    public class EnumerationDescriptionConverter
    {
        /// <summary>
        /// Converts a value from an enumerated type to a string description
        /// </summary>
        public static string ConvertEnumeratedValueToStringDescription(object enum_value)
        {
            FieldInfo fi = enum_value.GetType().GetField(enum_value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return enum_value.ToString();
        }

        /// <summary>
        /// Converts a string description to its associated value within an enumerated type
        /// </summary>
        public static object ConvertStringDescriptionToEnumeratedValue(Type type, string description)
        {
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return field.GetValue(null);
                }
            }

            return null;
        }
    }
}