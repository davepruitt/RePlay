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

namespace RePlay_Activity_FruitArchery.Main
{
    public class FruitArchery_FruitTypeConverter
    {
        /// <summary>
        /// Converts an fruit type to its respective asset string
        /// </summary>
        public static string ConvertFruitTypeToAssetStringDescription(FruitArchery_FruitType fruit_type)
        {
            FieldInfo fi = fruit_type.GetType().GetField(fruit_type.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return fruit_type.ToString();
        }

        /// <summary>
        /// Converts a string description to a fruit type
        /// </summary>
        public static FruitArchery_FruitType ConvertDescriptionToFruitType(string description)
        {
            var type = typeof(FruitArchery_FruitType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (FruitArchery_FruitType)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (FruitArchery_FruitType)field.GetValue(null);
                }
            }

            return FruitArchery_FruitType.Apple;
        }

        /// <summary>
        /// Converts a fruit type to its associated splat color
        /// </summary>
        public static FruitArchery_FruitSplatColor ConvertFruitTypeToFruitSplatColor (FruitArchery_FruitType fruit_type)
        {
            FieldInfo fi = fruit_type.GetType().GetField(fruit_type.ToString());

            FruitArchery_FruitAttributes[] attributes =
                (FruitArchery_FruitAttributes[])fi.GetCustomAttributes(
                typeof(FruitArchery_FruitAttributes),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].SplatColor;
            else
                return FruitArchery_FruitSplatColor.Unknown;
        }
    }
}