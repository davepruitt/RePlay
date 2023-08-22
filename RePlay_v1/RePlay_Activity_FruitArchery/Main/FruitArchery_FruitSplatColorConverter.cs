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
    public class FruitArchery_FruitSplatColorConverter
    {
        /// <summary>
        /// Converts an fruit splat color to its asset string
        /// </summary>
        public static string ConvertFruitSplatColorToAssetStringDescription(FruitArchery_FruitSplatColor fruit_type)
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
        /// Converts an asset string to the associated fruit splat color
        /// </summary>
        public static FruitArchery_FruitSplatColor ConvertDescriptionToFruitSplatColor(string description)
        {
            var type = typeof(FruitArchery_FruitSplatColor);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (FruitArchery_FruitSplatColor)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (FruitArchery_FruitSplatColor)field.GetValue(null);
                }
            }

            return FruitArchery_FruitSplatColor.Unknown;
        }
    }
}