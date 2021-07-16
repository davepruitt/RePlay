using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace RePlay_Activity_FruitArchery.Main
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class FruitArchery_FruitAttributes : Attribute
    {
        public FruitArchery_FruitSplatColor SplatColor { get; set; } = FruitArchery_FruitSplatColor.Unknown;

        public FruitArchery_FruitAttributes(FruitArchery_FruitSplatColor c)
        {
            SplatColor = c;
        }
    }
}