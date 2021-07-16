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
    public class SharkSpriteTypeConverter
    {
        /// <summary>
        /// Converts a shark sprite type to its associated description
        /// </summary>
        public static string ConvertSharkSpriteTypeTotringDescription(SharkSpriteType sprite_type)
        {
            FieldInfo fi = sprite_type.GetType().GetField(sprite_type.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return sprite_type.ToString();
        }

        /// <summary>
        /// Converts a string description to a shark sprite type
        /// </summary>
        public static SharkSpriteType ConvertStringDescriptionToSharkSpriteType(string description)
        {
            var type = typeof(SharkSpriteType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (SharkSpriteType)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (SharkSpriteType)field.GetValue(null);
                }
            }

            return SharkSpriteType.Unknown;
        }
    }
}